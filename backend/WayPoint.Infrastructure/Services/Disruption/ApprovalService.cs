using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;
using WayPoint.Domain.Entities.Audit;
using WayPoint.Domain.Entities.Disruption;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Disruption;

/// <summary>
/// Manages the Transport Manager approval boundary (BR-APPROVAL-001, BR-APPROVAL-002).
/// Classifies impact severity, manages the pending approval queue, and creates
/// immutable ApprovalDecision records with audit logging.
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly IWayPointDbContext _context;

    /// <summary>
    /// Timetable shift threshold in minutes for high-impact classification (BR-APPROVAL-001).
    /// Shifts ≤ this value are Low Impact and can auto-execute.
    /// Shifts > this value require PendingManagerApproval.
    /// </summary>
    private const int HighImpactTimetableShiftMinutes = 15;

    public ApprovalService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<List<PendingApprovalDto>> GetPendingApprovalsAsync()
    {
        var pendingProposals = await _context.RebookingProposals
            .Include(rp => rp.DisruptionCase)
                .ThenInclude(dc => dc.DisruptedService)
            .Include(rp => rp.ReplacementService)
            .Where(rp => rp.Status == RebookingStatus.PendingManagerApproval)
            .OrderByDescending(rp => rp.CreatedAt)
            .ToListAsync();

        var result = new List<PendingApprovalDto>();

        foreach (var rp in pendingProposals)
        {
            // Calculate revenue at risk from affected bookings
            var revenueAtRisk = await _context.Bookings
                .Where(b => b.ServiceId == rp.DisruptionCase.DisruptedServiceId
                         && b.Status == BookingStatus.Confirmed)
                .SumAsync(b => b.TotalFareAmount);

            result.Add(new PendingApprovalDto
            {
                RebookingProposalId = rp.Id,
                DisruptionCaseId = rp.DisruptionCaseId,
                OriginalServiceCode = rp.DisruptionCase.DisruptedService.ServiceCode,
                ReplacementServiceCode = rp.ReplacementService.ServiceCode,
                AffectedPassengersCount = rp.DisruptionCase.AffectedPassengerCount,
                RevenueAtRisk = revenueAtRisk,
                DisruptionReason = rp.DisruptionCase.Reason,
                Severity = rp.DisruptionCase.Severity,
                ProposedByAgent = rp.ProposedByAgent,
                ProposedAt = rp.CreatedAt
            });
        }

        return result;
    }

    public async Task<ApprovalDecisionDto> SubmitDecisionAsync(
        Guid proposalId, Guid managerId, ApprovalDecisionRequestDto dto)
    {
        // Find the proposal
        var proposal = await _context.RebookingProposals
            .Include(rp => rp.DisruptionCase)
            .FirstOrDefaultAsync(rp => rp.Id == proposalId);

        if (proposal == null)
            throw new KeyNotFoundException($"Rebooking proposal with ID '{proposalId}' was not found.");

        if (proposal.Status != RebookingStatus.PendingManagerApproval)
            throw new InvalidOperationException(
                $"Proposal '{proposalId}' is in status '{proposal.Status}' and cannot be acted upon. " +
                $"Only proposals in 'PendingManagerApproval' status can receive decisions.");

        // Validate manager exists
        var manager = await _context.Users.FirstOrDefaultAsync(u => u.Id == managerId);
        if (manager == null)
            throw new KeyNotFoundException($"Manager with ID '{managerId}' was not found.");

        // Record the before-state for audit
        var beforeState = new { ProposalStatus = proposal.Status.ToString(), DisruptionStatus = proposal.DisruptionCase.Status.ToString() };

        // Create immutable ApprovalDecision record (BR-APPROVAL-002)
        var decision = new ApprovalDecision
        {
            RebookingProposalId = proposalId,
            ManagerId = managerId,
            Decision = dto.Decision,
            Comments = dto.Comments,
            DecidedAt = DateTime.UtcNow
        };

        await _context.ApprovalDecisions.AddAsync(decision);

        // Transition proposal status based on decision
        switch (dto.Decision)
        {
            case ApprovalDecisionType.Approve:
                proposal.Status = RebookingStatus.Approved;
                proposal.DisruptionCase.Status = DisruptionStatus.PendingApproval; // Ready for execution
                break;

            case ApprovalDecisionType.Reject:
                proposal.Status = RebookingStatus.Rejected;
                proposal.DisruptionCase.Status = DisruptionStatus.Cancelled;
                break;

            case ApprovalDecisionType.RequestRevision:
                proposal.Status = RebookingStatus.Proposed; // Return to proposed for revision
                proposal.DisruptionCase.Status = DisruptionStatus.Analyzing;
                break;
        }

        proposal.UpdatedAt = DateTime.UtcNow;
        proposal.DisruptionCase.UpdatedAt = DateTime.UtcNow;

        // Record after-state for audit
        var afterState = new { ProposalStatus = proposal.Status.ToString(), DisruptionStatus = proposal.DisruptionCase.Status.ToString() };

        // Create immutable audit log entry (BR-AUDIT-001)
        var auditLog = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActorId = managerId.ToString(),
            ActionType = $"ApprovalDecision:{dto.Decision}",
            EntityName = "RebookingProposal",
            EntityId = proposalId.ToString(),
            BeforeStateJson = JsonSerializer.Serialize(beforeState),
            AfterStateJson = JsonSerializer.Serialize(afterState)
        };

        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();

        return new ApprovalDecisionDto
        {
            Id = decision.Id,
            RebookingProposalId = decision.RebookingProposalId,
            ManagerId = decision.ManagerId,
            ManagerName = manager.FullName,
            Decision = decision.Decision,
            Comments = decision.Comments,
            DecidedAt = decision.DecidedAt
        };
    }

    public async Task<string> ClassifyImpactSeverityAsync(Guid disruptionCaseId, Guid replacementServiceId)
    {
        var disruptionCase = await _context.DisruptionCases
            .Include(dc => dc.DisruptedService)
            .FirstOrDefaultAsync(dc => dc.Id == disruptionCaseId);

        if (disruptionCase == null)
            throw new KeyNotFoundException($"Disruption case with ID '{disruptionCaseId}' was not found.");

        // Rule 1: Service cancellation is always High Impact
        if (disruptionCase.DisruptedService.Status == ServiceStatus.Cancelled)
            return "High";

        // Rule 2: Critical severity is always High Impact
        if (disruptionCase.Severity == DisruptionSeverity.Critical)
            return "High";

        // Rule 3: Check timetable shift (BR-APPROVAL-001)
        var replacementService = await _context.Services.FirstOrDefaultAsync(s => s.Id == replacementServiceId);
        if (replacementService != null)
        {
            var timetableShiftMinutes = Math.Abs(
                (replacementService.DepartureTime - disruptionCase.DisruptedService.DepartureTime).TotalMinutes);

            if (timetableShiftMinutes > HighImpactTimetableShiftMinutes)
                return "High";
        }

        return "Low";
    }
}
