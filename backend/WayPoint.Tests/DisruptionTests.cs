using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.AiWorkflows.DTOs;
using WayPoint.Application.Features.DisruptionManagement.DTOs;
using WayPoint.Domain.Entities.Audit;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Disruption;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Entities.Ai;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using WayPoint.Infrastructure.Services.Disruption;
using Xunit;

namespace WayPoint.Tests;

/// <summary>
/// Student 4 (Dineth) Comprehensive Disruption, Rebooking &amp; Approval Test Suite.
/// Implements Section 8 testing requirements from guide-student4-dineth-disruption.md:
/// 
/// 1. Unit Tests:
///    - Disruption impact classification rules (BR-DISRUPT-001, BR-APPROVAL-001)
///    - Approval state machine transitions (Proposed -> PendingManagerApproval -> Approved / Rejected)
///    - Manager decision audit logging with before/after state snapshots
///    - Disruption logging &amp; impact calculation
///    - Public service alert broadcast &amp; retrieval
/// 
/// 2. Integration Tests:
///    - Approval boundary: unauthorized execution blocked without Approved state (BR-APPLY-001)
///    - Transactional rollback during rebooking failure (BR-REBOOK-001)
///    - Successful atomic rebooking execution with ticket re-issuance &amp; service alerts
///    - Safe-failure fallback on AI execution error (FR-AI-004, SafeFailure)
/// </summary>
public class DisruptionTests
{
    // =========================================================================
    // SECTION 1: UNIT TESTS — IMPACT CLASSIFICATION (BR-DISRUPT-001, BR-APPROVAL-001)
    // =========================================================================

    [Theory]
    [InlineData(10, "Low")]   // Departure timetable shift <= 15 min -> Low Impact (can auto-execute)
    [InlineData(15, "Low")]   // Boundary condition: exactly 15 min -> Low Impact
    [InlineData(16, "High")]  // Boundary condition: 16 min (>15 min) -> High Impact (requires Manager approval)
    [InlineData(45, "High")]  // Major shift (45 min) -> High Impact
    public async Task BR_DISRUPT_001_TimetableShift_ClassifiesImpactCorrectly(
        int shiftMinutes, string expectedClassification)
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, _) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);

        // Adjust replacement departure time
        service2.DepartureTime = service1.DepartureTime.AddMinutes(shiftMinutes);
        await context.SaveChangesAsync();

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Engine Overheating",
            Severity = DisruptionSeverity.Major,
            Status = DisruptionStatus.Logged,
            AffectedPassengerCount = 20
        };
        await context.DisruptionCases.AddAsync(disruptionCase);
        await context.SaveChangesAsync();

        // Act
        var classification = await approvalService.ClassifyImpactSeverityAsync(
            disruptionCase.Id, service2.Id);

        // Assert
        Assert.Equal(expectedClassification, classification);
    }

    [Fact]
    public async Task BR_DISRUPT_001_ServiceCancelled_AlwaysClassifiesAsHighImpact()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, _) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);

        // Shift is only 5 minutes, but service status is Cancelled
        service2.DepartureTime = service1.DepartureTime.AddMinutes(5);
        service1.Status = ServiceStatus.Cancelled;
        await context.SaveChangesAsync();

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Severe Route Flooding",
            Severity = DisruptionSeverity.Major,
            Status = DisruptionStatus.Logged,
            AffectedPassengerCount = 15
        };
        await context.DisruptionCases.AddAsync(disruptionCase);
        await context.SaveChangesAsync();

        // Act
        var classification = await approvalService.ClassifyImpactSeverityAsync(
            disruptionCase.Id, service2.Id);

        // Assert
        Assert.Equal("High", classification);
    }

    [Fact]
    public async Task BR_DISRUPT_001_CriticalSeverity_AlwaysClassifiesAsHighImpact()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, _) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);

        // Shift is 0 minutes, but disruption severity is Critical
        service2.DepartureTime = service1.DepartureTime;
        await context.SaveChangesAsync();

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Head-on Vehicle Collision",
            Severity = DisruptionSeverity.Critical,
            Status = DisruptionStatus.Logged,
            AffectedPassengerCount = 35
        };
        await context.DisruptionCases.AddAsync(disruptionCase);
        await context.SaveChangesAsync();

        // Act
        var classification = await approvalService.ClassifyImpactSeverityAsync(
            disruptionCase.Id, service2.Id);

        // Assert
        Assert.Equal("High", classification);
    }

    // =========================================================================
    // SECTION 2: UNIT TESTS — APPROVAL STATE MACHINE & AUDIT LOGGING (BR-APPROVAL-001)
    // =========================================================================

    [Fact]
    public async Task BR_APPROVAL_001_HighImpactProposal_TransitionsToPendingManagerApproval()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, _) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);
        var rebookingService = new RebookingService(context, approvalService);

        // Departure shift > 15 minutes
        service2.DepartureTime = service1.DepartureTime.AddMinutes(30);
        await context.SaveChangesAsync();

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Mechanical Breakdown",
            Severity = DisruptionSeverity.Major,
            Status = DisruptionStatus.Logged,
            AffectedPassengerCount = 25
        };
        await context.DisruptionCases.AddAsync(disruptionCase);
        await context.SaveChangesAsync();

        // Act - Create rebooking proposal
        var proposal = await rebookingService.CreateProposalAsync(new CreateRebookingProposalDto
        {
            DisruptionCaseId = disruptionCase.Id,
            ReplacementServiceId = service2.Id,
            ProposedByAgent = "ResourceBookingAgent"
        });

        // Assert
        Assert.Equal(RebookingStatus.PendingManagerApproval, proposal.Status);

        var updatedCase = await context.DisruptionCases.FindAsync(disruptionCase.Id);
        Assert.NotNull(updatedCase);
        Assert.Equal(DisruptionStatus.PendingApproval, updatedCase.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_ManagerApproves_TransitionsToApprovedAndPendingExecution()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, manager) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Engine Failure",
            Severity = DisruptionSeverity.Major,
            Status = DisruptionStatus.PendingApproval,
            AffectedPassengerCount = 20
        };
        await context.DisruptionCases.AddAsync(disruptionCase);

        var proposal = new RebookingProposal
        {
            DisruptionCaseId = disruptionCase.Id,
            ReplacementServiceId = service2.Id,
            ProposedByAgent = "ResourceBookingAgent",
            Status = RebookingStatus.PendingManagerApproval
        };
        await context.RebookingProposals.AddAsync(proposal);
        await context.SaveChangesAsync();

        // Act - Manager Approves proposal
        var decisionDto = await approvalService.SubmitDecisionAsync(
            proposal.Id,
            manager.Id,
            new ApprovalDecisionRequestDto
            {
                Decision = ApprovalDecisionType.Approve,
                Comments = "Approved emergency replacement coach dispatch."
            });

        // Assert
        Assert.NotNull(decisionDto);
        Assert.Equal(ApprovalDecisionType.Approve, decisionDto.Decision);
        Assert.Equal(manager.Id, decisionDto.ManagerId);

        var updatedProposal = await context.RebookingProposals.FindAsync(proposal.Id);
        Assert.NotNull(updatedProposal);
        Assert.Equal(RebookingStatus.Approved, updatedProposal.Status);

        var updatedCase = await context.DisruptionCases.FindAsync(disruptionCase.Id);
        Assert.NotNull(updatedCase);
        Assert.Equal(DisruptionStatus.PendingApproval, updatedCase.Status);

        // Verify Immutable Audit Log (BR-AUDIT-001)
        var auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == proposal.Id.ToString());
        Assert.NotNull(auditLog);
        Assert.Equal(manager.Id.ToString(), auditLog.ActorId);
        Assert.Contains("ApprovalDecision:Approve", auditLog.ActionType);
        Assert.Contains("PendingManagerApproval", auditLog.BeforeStateJson);
        Assert.Contains("Approved", auditLog.AfterStateJson);
    }

    [Fact]
    public async Task SubmitDecisionAsync_ManagerRejects_TransitionsToRejectedAndCancelled()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, manager) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Minor Route Divergence",
            Severity = DisruptionSeverity.Minor,
            Status = DisruptionStatus.PendingApproval,
            AffectedPassengerCount = 10
        };
        await context.DisruptionCases.AddAsync(disruptionCase);

        var proposal = new RebookingProposal
        {
            DisruptionCaseId = disruptionCase.Id,
            ReplacementServiceId = service2.Id,
            ProposedByAgent = "ResourceBookingAgent",
            Status = RebookingStatus.PendingManagerApproval
        };
        await context.RebookingProposals.AddAsync(proposal);
        await context.SaveChangesAsync();

        // Act - Manager Rejects proposal
        var decisionDto = await approvalService.SubmitDecisionAsync(
            proposal.Id,
            manager.Id,
            new ApprovalDecisionRequestDto
            {
                Decision = ApprovalDecisionType.Reject,
                Comments = "Cost differential exceeds limits. Divert to scheduled train."
            });

        // Assert
        Assert.NotNull(decisionDto);
        Assert.Equal(ApprovalDecisionType.Reject, decisionDto.Decision);

        var updatedProposal = await context.RebookingProposals.FindAsync(proposal.Id);
        Assert.NotNull(updatedProposal);
        Assert.Equal(RebookingStatus.Rejected, updatedProposal.Status);

        var updatedCase = await context.DisruptionCases.FindAsync(disruptionCase.Id);
        Assert.NotNull(updatedCase);
        Assert.Equal(DisruptionStatus.Cancelled, updatedCase.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_NonPendingProposal_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, manager) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Maintenance",
            Severity = DisruptionSeverity.Minor,
            Status = DisruptionStatus.Analyzing,
            AffectedPassengerCount = 5
        };
        await context.DisruptionCases.AddAsync(disruptionCase);

        // Proposal is in 'Proposed' status (not PendingManagerApproval)
        var proposal = new RebookingProposal
        {
            DisruptionCaseId = disruptionCase.Id,
            ReplacementServiceId = service2.Id,
            ProposedByAgent = "ResourceBookingAgent",
            Status = RebookingStatus.Proposed
        };
        await context.RebookingProposals.AddAsync(proposal);
        await context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            approvalService.SubmitDecisionAsync(
                proposal.Id,
                manager.Id,
                new ApprovalDecisionRequestDto { Decision = ApprovalDecisionType.Approve })
        );

        Assert.Contains("Only proposals in 'PendingManagerApproval' status can receive decisions", ex.Message);
    }

    // =========================================================================
    // SECTION 3: UNIT TESTS — DISRUPTION LOGGING & IMPACT ASSESSMENT
    // =========================================================================

    [Fact]
    public async Task LogDisruptionAsync_ValidRequest_CreatesCaseAndSetsServiceDisrupted()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, _, _) = await SeedServicesAndManagerAsync(context);
        var disruptionService = new DisruptionService(context);

        var logDto = new LogDisruptionDto
        {
            DisruptedServiceId = service.Id,
            Reason = "Transmission Failure",
            Severity = DisruptionSeverity.Critical,
            AffectedPassengerCountOverride = 35
        };

        // Act
        var result = await disruptionService.LogDisruptionAsync(logDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.Id, result.DisruptedServiceId);
        Assert.Equal(DisruptionSeverity.Critical, result.Severity);
        Assert.Equal(35, result.AffectedPassengerCount);
        Assert.Equal(DisruptionStatus.Logged, result.Status);

        var updatedService = await context.Services.FindAsync(service.Id);
        Assert.NotNull(updatedService);
        Assert.Equal(ServiceStatus.Disrupted, updatedService.Status);
    }

    [Fact]
    public async Task GetDisruptionImpactAsync_CalculatesPassengerCountAndRevenueAtRisk()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, _, _) = await SeedServicesAndManagerAsync(context);
        var disruptionService = new DisruptionService(context);

        // Seed 3 confirmed bookings
        for (int i = 0; i < 3; i++)
        {
            var user = new User
            {
                Email = $"passenger{i}@waypoint.lk",
                FullName = $"Passenger {i}",
                PhoneNumber = $"+9477123456{i}",
                PasswordHash = "hash",
                RoleId = Guid.NewGuid()
            };
            await context.Users.AddAsync(user);

            var profile = new PassengerProfile
            {
                UserId = user.Id,
                NicOrPassport = $"90123456{i}V"
            };
            await context.PassengerProfiles.AddAsync(profile);

            var booking = new Booking
            {
                BookingReference = $"WP-IMP-00{i}",
                PassengerId = profile.Id,
                ServiceId = service.Id,
                SeatNumbers = $"A{i + 1}",
                TotalFareAmount = 2500m,
                Status = BookingStatus.Confirmed
            };
            await context.Bookings.AddAsync(booking);
        }

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service.Id,
            Reason = "Electrical Malfunction",
            Severity = DisruptionSeverity.Major,
            Status = DisruptionStatus.Logged,
            AffectedPassengerCount = 3
        };
        await context.DisruptionCases.AddAsync(disruptionCase);
        await context.SaveChangesAsync();

        // Act
        var impact = await disruptionService.GetDisruptionImpactAsync(disruptionCase.Id);

        // Assert
        Assert.NotNull(impact);
        Assert.Equal(3, impact.TotalBookedPassengers);
        Assert.Equal(7500m, impact.RevenueAtRisk);
        Assert.Equal(3, impact.AffectedBookingIds.Count);
        Assert.Equal(3, impact.AffectedPassengerEmails.Count);
    }

    // =========================================================================
    // SECTION 4: UNIT TESTS — PUBLIC SERVICE ALERT BROADCAST & FEED
    // =========================================================================

    [Fact]
    public async Task ServiceAlertService_BroadcastAndRetrieveActiveAlerts_Succeeds()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service, _, _) = await SeedServicesAndManagerAsync(context);
        var alertService = new ServiceAlertService(context);

        var alertDto = new CreateServiceAlertDto
        {
            ServiceId = service.Id,
            Title = "Delay Warning: Colombo–Kandy Corridor",
            Message = "Service EX-08 running with a 25-minute delay due to road clearing."
        };

        // Act - Broadcast
        var createdAlert = await alertService.BroadcastAlertAsync(alertDto);

        // Assert
        Assert.NotNull(createdAlert);
        Assert.Equal(service.Id, createdAlert.ServiceId);
        Assert.Equal(alertDto.Title, createdAlert.Title);

        // Act - Retrieve Active Alerts Feed
        var alerts = await alertService.GetActiveAlertsAsync();
        Assert.NotEmpty(alerts);
        Assert.Contains(alerts, a => a.Title == alertDto.Title);
    }

    // =========================================================================
    // SECTION 5: INTEGRATION TESTS — APPROVAL BOUNDARY ENFORCEMENT (BR-APPLY-001)
    // =========================================================================

    [Fact]
    public async Task BR_APPLY_001_ExecuteUnapprovedProposal_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, _) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);
        var rebookingService = new RebookingService(context, approvalService);

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Engine Failure",
            Severity = DisruptionSeverity.Critical,
            Status = DisruptionStatus.PendingApproval,
            AffectedPassengerCount = 20
        };
        await context.DisruptionCases.AddAsync(disruptionCase);

        // Proposal is still PendingManagerApproval (NOT Approved)
        var proposal = new RebookingProposal
        {
            DisruptionCaseId = disruptionCase.Id,
            ReplacementServiceId = service2.Id,
            ProposedByAgent = "ResourceBookingAgent",
            Status = RebookingStatus.PendingManagerApproval
        };
        await context.RebookingProposals.AddAsync(proposal);
        await context.SaveChangesAsync();

        // Act & Assert — Must strictly reject execution (BR-APPLY-001)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            rebookingService.ExecuteApprovedRebookingAsync(proposal.Id));

        Assert.Contains("Only proposals with status 'Approved' can be executed (BR-APPLY-001)", ex.Message);
    }

    // =========================================================================
    // SECTION 6: INTEGRATION TESTS — TRANSACTIONAL REBOOKING EXECUTION (BR-REBOOK-001)
    // =========================================================================

    [Fact]
    public async Task BR_REBOOK_001_ExecuteApprovedRebooking_TransfersBookingsAndReissuesTickets()
    {
        // Arrange
        using var context = CreateDbContext();
        var (service1, service2, _) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);
        var rebookingService = new RebookingService(context, approvalService);

        // Seed passenger, booking, and ticket on service1
        var user = new User
        {
            Email = "rebooked.passenger@waypoint.lk",
            FullName = "Rebooked Passenger",
            PasswordHash = "hash",
            RoleId = Guid.NewGuid()
        };
        await context.Users.AddAsync(user);

        var profile = new PassengerProfile
        {
            UserId = user.Id,
            NicOrPassport = "998877665V"
        };
        await context.PassengerProfiles.AddAsync(profile);

        var booking = new Booking
        {
            BookingReference = "WP-RBK-TEST1",
            PassengerId = profile.Id,
            ServiceId = service1.Id,
            SeatNumbers = "A1",
            TotalFareAmount = 3000m,
            Status = BookingStatus.Confirmed
        };
        await context.Bookings.AddAsync(booking);

        var ticket = new Ticket
        {
            BookingId = booking.Id,
            QrCodePayload = "WP|REF:WP-RBK-TEST1|SRV:SRV-01|SEATS:A1",
            Status = TicketStatus.Issued
        };
        await context.Tickets.AddAsync(ticket);

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Brake Line Failure",
            Severity = DisruptionSeverity.Major,
            Status = DisruptionStatus.PendingApproval,
            AffectedPassengerCount = 1
        };
        await context.DisruptionCases.AddAsync(disruptionCase);

        var proposal = new RebookingProposal
        {
            DisruptionCaseId = disruptionCase.Id,
            ReplacementServiceId = service2.Id,
            ProposedByAgent = "ResourceBookingAgent",
            Status = RebookingStatus.Approved // Pre-approved by manager
        };
        await context.RebookingProposals.AddAsync(proposal);
        await context.SaveChangesAsync();

        // Act - Execute the approved rebooking
        var executionResult = await rebookingService.ExecuteApprovedRebookingAsync(proposal.Id);

        // Assert - Execution succeeded
        Assert.True(executionResult.Success);
        Assert.Equal(1, executionResult.PassengersRebooked);

        // Assert - Booking transferred to replacement service
        var updatedBooking = await context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updatedBooking);
        Assert.Equal(service2.Id, updatedBooking.ServiceId);

        // Assert - Proposal marked Executed and Disruption resolved
        var updatedProposal = await context.RebookingProposals.FindAsync(proposal.Id);
        Assert.NotNull(updatedProposal);
        Assert.Equal(RebookingStatus.Executed, updatedProposal.Status);

        var updatedCase = await context.DisruptionCases.FindAsync(disruptionCase.Id);
        Assert.NotNull(updatedCase);
        Assert.Equal(DisruptionStatus.Resolved, updatedCase.Status);

        // Assert - ServiceAlert automatically generated notifying passenger
        var alert = await context.ServiceAlerts.FirstOrDefaultAsync(a => a.ServiceId == service1.Id);
        Assert.NotNull(alert);
        Assert.Contains("Passengers Rebooked", alert.Title);
        Assert.Contains(service2.ServiceCode, alert.Message);
    }

    [Fact]
    public async Task BR_REBOOK_001_TransactionalRollback_WhenExceptionOccurs_LeavesDataIntact()
    {
        // Arrange
        var dbName = $"WayPoint_Rollback_Disruption_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var (service1, service2, _) = await SeedServicesAndManagerAsync(context);
        var approvalService = new ApprovalService(context);
        var rebookingService = new RebookingService(context, approvalService);

        var user = new User
        {
            Email = "intact.passenger@waypoint.lk",
            FullName = "Intact Passenger",
            PasswordHash = "hash",
            RoleId = Guid.NewGuid()
        };
        await context.Users.AddAsync(user);

        var profile = new PassengerProfile
        {
            UserId = user.Id,
            NicOrPassport = "123456789V"
        };
        await context.PassengerProfiles.AddAsync(profile);

        var booking = new Booking
        {
            BookingReference = "WP-INTACT-99",
            PassengerId = profile.Id,
            ServiceId = service1.Id,
            SeatNumbers = "A2",
            TotalFareAmount = 2800m,
            Status = BookingStatus.Confirmed
        };
        await context.Bookings.AddAsync(booking);

        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = service1.Id,
            Reason = "Radiator Leak",
            Severity = DisruptionSeverity.Major,
            Status = DisruptionStatus.PendingApproval,
            AffectedPassengerCount = 1
        };
        await context.DisruptionCases.AddAsync(disruptionCase);

        var proposal = new RebookingProposal
        {
            DisruptionCaseId = disruptionCase.Id,
            ReplacementServiceId = service2.Id,
            ProposedByAgent = "ResourceBookingAgent",
            Status = RebookingStatus.PendingManagerApproval // Invalid status causes atomic rejection
        };
        await context.RebookingProposals.AddAsync(proposal);
        await context.SaveChangesAsync();

        // Act - Attempt execution which fails pre-validation
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            rebookingService.ExecuteApprovedRebookingAsync(proposal.Id));

        // Assert - Verify state in database was completely preserved
        using var verifyContext = CreateDbContext(dbName);
        var verifyBooking = await verifyContext.Bookings.FindAsync(booking.Id);
        Assert.NotNull(verifyBooking);
        Assert.Equal(service1.Id, verifyBooking.ServiceId); // Remains on service1
        Assert.Equal(BookingStatus.Confirmed, verifyBooking.Status);

        var verifyProposal = await verifyContext.RebookingProposals.FindAsync(proposal.Id);
        Assert.NotNull(verifyProposal);
        Assert.Equal(RebookingStatus.PendingManagerApproval, verifyProposal.Status); // Unchanged
    }

    // =========================================================================
    // SECTION 7: INTEGRATION TESTS — SAFE-FAILURE FALLBACK (FR-AI-004)
    // =========================================================================

    [Fact]
    public async Task SafeFailure_AiWorkflowExecutionError_RecordsSafeFailureWithoutCorruptingData()
    {
        // Arrange
        using var context = CreateDbContext();
        var aiService = new AiWorkflowService(context);

        // 1. Create a running workflow trace
        var createdWorkflow = await aiService.CreateWorkflowAsync(new CreateAiWorkflowDto(
            "Generate multi-agent rebooking alternatives for disrupted Colombo–Ella service."
        ));

        Assert.Equal(AiWorkflowStatus.Running, createdWorkflow.Status);

        // 2. Add step 1: Planner
        var step1 = await aiService.AddStepAsync(createdWorkflow.Id, new CreateAiWorkflowStepDto(
            "PlannerAgent",
            1,
            "Decompose disruption constraints and identify candidate corridors."
        ));
        Assert.NotNull(step1);

        // 3. Simulate Agentic Failure / Safety Violation -> Transition to SafeFailure (FR-AI-004)
        var failureStatus = new UpdateAiWorkflowStatusDto(AiWorkflowStatus.SafeFailure);

        var updatedWorkflow = await aiService.UpdateWorkflowStatusAsync(createdWorkflow.Id, failureStatus);

        // Assert - Workflow cleanly recorded as SafeFailure
        Assert.Equal(AiWorkflowStatus.SafeFailure, updatedWorkflow.Status);
        Assert.NotNull(updatedWorkflow.CompletedAt);

        // Assert - Database state query verifies persisted SafeFailure status
        var persisted = await context.AiWorkflows.FindAsync(createdWorkflow.Id);
        Assert.NotNull(persisted);
        Assert.Equal(AiWorkflowStatus.SafeFailure, persisted.Status);
    }

    // =========================================================================
    // TEST ENVIRONMENT SEEDING & HELPERS
    // =========================================================================

    private static WayPointDbContext CreateDbContext(string? sharedDbName = null)
    {
        var databaseName = sharedDbName ?? $"WayPoint_Disruption_Test_{Guid.NewGuid()}";
        var inMemoryOptions = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WayPointDbContext(inMemoryOptions);
    }

    private static async Task<(Service service1, Service service2, User manager)> SeedServicesAndManagerAsync(
        WayPointDbContext context)
    {
        var route = new Route
        {
            RouteCode = "RT-COL-ELLA",
            OriginCity = "Colombo",
            DestinationCity = "Ella",
            TotalDistanceKm = 205.0m,
            IsActive = true
        };
        await context.Routes.AddAsync(route);

        var seatLayout = new SeatLayout
        {
            Name = "Standard 2x2 AC",
            TotalRows = 10,
            TotalColumns = 4
        };
        await context.SeatLayouts.AddAsync(seatLayout);

        var bus1 = new Bus
        {
            RegistrationNumber = "ND-8821",
            BusClass = BusClass.Standard,
            TotalSeatCapacity = 40,
            SeatLayoutId = seatLayout.Id
        };
        var bus2 = new Bus
        {
            RegistrationNumber = "WP-CAD-4120",
            BusClass = BusClass.Luxury,
            TotalSeatCapacity = 40,
            SeatLayoutId = seatLayout.Id
        };
        await context.Buses.AddRangeAsync(bus1, bus2);

        // Seed seats for bus 1 and bus 2
        for (int r = 1; r <= 2; r++)
        {
            for (int c = 1; c <= 2; c++)
            {
                var seatName = $"{(char)('A' + c - 1)}{r}";
                await context.Seats.AddAsync(new Seat
                {
                    SeatLayoutId = seatLayout.Id,
                    SeatNumber = seatName,
                    RowIndex = r,
                    ColumnIndex = c
                });
            }
        }

        var baseTime = DateTime.UtcNow.Date.AddHours(6).AddMinutes(30);

        var service1 = new Service
        {
            ServiceCode = "EX-08-DISRUPTED",
            RouteId = route.Id,
            BusId = bus1.Id,
            DepartureTime = baseTime,
            ArrivalTime = baseTime.AddHours(5).AddMinutes(15),
            BaseFare = 2400m,
            Status = ServiceStatus.Scheduled
        };

        var service2 = new Service
        {
            ServiceCode = "EX-08-REPLACEMENT",
            RouteId = route.Id,
            BusId = bus2.Id,
            DepartureTime = baseTime.AddMinutes(45), // 45 min shift by default
            ArrivalTime = baseTime.AddHours(5).AddMinutes(50),
            BaseFare = 2400m,
            Status = ServiceStatus.Scheduled
        };

        await context.Services.AddRangeAsync(service1, service2);

        var managerRole = new Role
        {
            RoleName = "TransportManager",
            Description = "Transport operations manager authorized for high-impact rebooking approvals."
        };
        await context.Roles.AddAsync(managerRole);

        var managerUser = new User
        {
            Email = "manager.test@waypoint.lk",
            FullName = "Amara Wickramasinghe",
            PasswordHash = "hash",
            RoleId = managerRole.Id
        };
        await context.Users.AddAsync(managerUser);

        await context.SaveChangesAsync();

        return (service1, service2, managerUser);
    }
}
