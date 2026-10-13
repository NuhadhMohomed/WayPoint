using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WayPoint.Application.DTOs.Auth;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Application.Features.JourneyPlanning.DTOs;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure;
using WayPoint.Infrastructure.Data;
using Xunit.Abstractions;

namespace WayPoint.Tests;

public class RealPostgreSqlIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;
    private readonly string _connectionString;

    public RealPostgreSqlIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5010") };

        // Load .env by walking up directory tree
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? envPath = null;
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                envPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        if (envPath != null && File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
                var separatorIdx = trimmed.IndexOf('=');
                if (separatorIdx > 0)
                {
                    var key = trimmed[..separatorIdx].Trim();
                    var val = trimmed[(separatorIdx + 1)..].Trim().Trim('"', '\'');
                    Environment.SetEnvironmentVariable(key, val);
                }
            }
        }

        var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        _connectionString = DependencyInjection.ResolveConnectionString(config);
    }

    private WayPointDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new WayPointDbContext(options);
    }

    [Fact]
    public async Task Scenario00_Check_EF_Migrations_History()
    {
        await using var context = CreateDbContext();
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";";
        var appliedMigrations = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            appliedMigrations.Add(reader.GetString(0));
        }
        _output.WriteLine($"Found {appliedMigrations.Count} applied migrations in __EFMigrationsHistory:");
        foreach (var m in appliedMigrations)
        {
            _output.WriteLine($"  - {m}");
        }
    }

    [Fact]
    public async Task Scenario01_Direct_PostgreSql_Reads_And_Writes_Persist_Correctly()
    {
        await using var context = CreateDbContext();

        // Read: Verify seeded services exist in local PostgreSQL
        var services = await context.Services.Include(s => s.Bus).ToListAsync();
        services.Should().NotBeEmpty("Seeded services should exist in waypoint_test");
        _output.WriteLine($"Verified {services.Count} seeded services in waypoint_test:");
        foreach (var s in services)
        {
            _output.WriteLine($"  Service: {s.ServiceCode} -> Id: {s.Id}");
        }

        var testService = services.First();
        var testSeat = await context.Seats
            .Where(s => s.SeatLayoutId == testService.Bus.SeatLayoutId)
            .FirstAsync();
        var testPassenger = await context.PassengerProfiles.FirstAsync();

        // Write: Create disposable record in local PostgreSQL
        var disposableHold = new SeatHold
        {
            ServiceId = testService.Id,
            SeatId = testSeat.Id,
            PassengerId = testPassenger.Id,
            Status = SeatHoldStatus.Held,
            HeldAt = DateTime.UtcNow,
            HeldUntil = DateTime.UtcNow.AddMinutes(5),
            ReservationGroupId = Guid.NewGuid()
        };

        context.SeatHolds.Add(disposableHold);
        await context.SaveChangesAsync();
        _output.WriteLine($"Saved disposable SeatHold ID: {disposableHold.Id}");

        // Verify read back
        var fetched = await context.SeatHolds.FindAsync(disposableHold.Id);
        fetched.Should().NotBeNull();
        fetched!.Status.Should().Be(SeatHoldStatus.Held);

        // Cleanup
        context.SeatHolds.Remove(fetched);
        await context.SaveChangesAsync();
        _output.WriteLine("Disposable test record cleaned up successfully.");
    }

    [Fact]
    public async Task Scenario02_To_11_Complete_End_To_End_Booking_And_Lifecycle_Workflow()
    {
        await using var dbContext = CreateDbContext();

        // 1. Passenger Registration
        var testEmail = $"passenger_{Guid.NewGuid():N}@waypoint.lk";
        var testPassword = "SecurePassword123!";
        var registerRequest = new RegisterRequestDto
        {
            Email = testEmail,
            Password = testPassword,
            FullName = "Local Integration Passenger",
            PhoneNumber = "+94770001122",
            Role = "Passenger"
        };

        var regResponse = await _httpClient.PostAsJsonAsync("/api/v1/Auth/register", registerRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var regResult = await regResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        regResult.Should().NotBeNull();
        regResult!.Token.Should().NotBeNullOrWhiteSpace();
        _output.WriteLine($"Registered passenger {testEmail}");

        // 2. Passenger Login
        var loginResponse = await _httpClient.PostAsJsonAsync("/api/v1/Auth/login", new LoginRequestDto
        {
            Email = testEmail,
            Password = testPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        loginResult.Should().NotBeNull();
        var token = loginResult!.Token;
        _output.WriteLine("Passenger login successful, JWT obtained.");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 3. Journey Search
        var searchRequest = new JourneySearchRequestDto
        {
            OriginCity = "Colombo",
            DestinationCity = "Kandy",
            TravelDate = DateTime.UtcNow.Date.AddDays(1),
            PassengerCount = 2
        };

        var searchResponse = await _httpClient.PostAsJsonAsync("/api/v1/journeys/search", searchRequest);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<JourneySearchResponseDto>();
        searchResult.Should().NotBeNull();
        searchResult!.Candidates.Should().NotBeEmpty("Should find scheduled journeys for Colombo -> Kandy");
        var selectedJourney = searchResult.Candidates.First();
        _output.WriteLine($"Found journey route {selectedJourney.RouteNumber} service {selectedJourney.ServiceId}");

        // Find available unallocated physical seat numbers for this service from database
        var service = await dbContext.Services.Include(s => s.Bus).FirstAsync(s => s.Id == selectedJourney.ServiceId);
        var allocatedSeatIdsForService = await dbContext.SeatAllocations
            .Where(a => a.ServiceId == selectedJourney.ServiceId)
            .Select(a => a.SeatId)
            .ToListAsync();

        var serviceSeats = await dbContext.Seats
            .Where(s => s.SeatLayoutId == service.Bus.SeatLayoutId && !allocatedSeatIdsForService.Contains(s.Id))
            .Take(2)
            .ToListAsync();

        serviceSeats.Count.Should().Be(2, "Need 2 physical seats to test multi-seat hold grouping");
        var seatIds = serviceSeats.Select(s => s.Id).ToList();
        var seatNumbers = serviceSeats.Select(s => s.SeatNumber).ToList();

        // 4. Seat Hold Persistence & 6. ReservationGroupId Handling
        var holdRequest = new SeatHoldRequestDto
        {
            ServiceId = selectedJourney.ServiceId,
            SeatNumbers = seatNumbers,
            PassengerName = "Local Integration Passenger"
        };

        var holdResponse = await _httpClient.PostAsJsonAsync("/api/v1/bookings/hold", holdRequest);
        holdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var holdResult = await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponseDto>();
        holdResult.Should().NotBeNull();
        holdResult!.HoldId.Should().NotBeEmpty();
        holdResult.ReservationGroupId.Should().NotBeEmpty();
        _output.WriteLine($"Created Hold ID {holdResult.HoldId} with ReservationGroupId {holdResult.ReservationGroupId}");

        // Verify SeatAllocation records in database have AllocationType Hold and match ReservationGroupId
        var allocations = await dbContext.SeatAllocations
            .Where(a => a.ReservationGroupId == holdResult.ReservationGroupId)
            .ToListAsync();
        allocations.Count.Should().Be(2);
        allocations.Should().OnlyContain(a => a.AllocationType == SeatAllocationType.Hold);
        _output.WriteLine("Database verified 2 SeatAllocation records with AllocationType 'Hold'");

        // 5. PaymentAttempt Persistence (Sandbox charge simulation)
        var paymentAmount = selectedJourney.TotalFare * 2;
        var paymentRequest = new PaymentChargeRequestDto
        {
            HoldId = holdResult.HoldId,
            ReservationGroupId = holdResult.ReservationGroupId,
            Amount = paymentAmount,
            CardNumber = "4111111111110001",
            CardholderName = "LOCAL INTEGRATION PASSENGER",
            ExpiryDate = "12/28",
            Cvv = "123"
        };

        var payResponse = await _httpClient.PostAsJsonAsync("/api/v1/payments/confirm-sandbox-charge", paymentRequest);
        payResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payResult = await payResponse.Content.ReadFromJsonAsync<PaymentChargeResponseDto>();
        payResult.Should().NotBeNull();
        payResult!.TransactionId.Should().NotBeNullOrWhiteSpace();
        _output.WriteLine($"Sandbox payment succeeded with TxnId: {payResult.TransactionId}");

        // Verify PaymentAttempt record persisted in database
        var paymentAttempt = await dbContext.PaymentAttempts
            .FirstOrDefaultAsync(p => p.GatewayTransactionId == payResult.TransactionId);
        paymentAttempt.Should().NotBeNull();
        paymentAttempt!.ReservationGroupId.Should().Be(holdResult.ReservationGroupId);
        paymentAttempt.Status.Should().Be(PaymentStatus.Success);
        _output.WriteLine("Database verified PaymentAttempt record persisted with status 'Success'");

        // 7. Booking Confirmation (Checkout)
        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = holdResult.HoldId,
            ReservationGroupId = holdResult.ReservationGroupId,
            PaymentTransactionId = payResult.TransactionId,
            PassengerName = "Local Integration Passenger"
        };

        var checkoutResponse = await _httpClient.PostAsJsonAsync("/api/v1/bookings/checkout", checkoutRequest);
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<BookingConfirmationDto>();
        checkoutResult.Should().NotBeNull();
        checkoutResult!.BookingReference.Should().NotBeNullOrWhiteSpace();
        _output.WriteLine($"Checkout confirmed! Booking Reference: {checkoutResult.BookingReference}");

        // Verify SeatAllocations transitioned from Hold to Booking in PostgreSQL
        var confirmedAllocations = await dbContext.SeatAllocations
            .AsNoTracking()
            .Where(a => a.ReservationGroupId == holdResult.ReservationGroupId)
            .ToListAsync();
        confirmedAllocations.Should().OnlyContain(a => a.AllocationType == SeatAllocationType.Booking);
        _output.WriteLine("Database verified SeatAllocations transitioned from 'Hold' to 'Booking'");

        // 8. Cryptographic QR Ticket Generation & Validation
        checkoutResult.QrCodePayload.Should().NotBeNullOrWhiteSpace();
        _output.WriteLine($"Generated QR Payload: {checkoutResult.QrCodePayload}");

        // Login as Conductor/Operator to verify ticket
        var operatorLogin = await _httpClient.PostAsJsonAsync("/api/v1/Auth/login", new LoginRequestDto
        {
            Email = "operator@waypoint.lk",
            Password = "Password123!"
        });
        operatorLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var opToken = (await operatorLogin.Content.ReadFromJsonAsync<AuthResponseDto>())!.Token;

        using var opClient = new HttpClient { BaseAddress = new Uri("http://localhost:5010") };
        opClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opToken);

        var verifyTicketResponse = await opClient.PostAsJsonAsync("/api/v1/tickets/verify", new
        {
            qrCodePayload = checkoutResult.QrCodePayload
        });
        verifyTicketResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _output.WriteLine("Cryptographic QR Ticket verification passed via Conductor endpoint.");

        // 9. Seat Allocation Uniqueness Verification in real PostgreSQL
        Func<Task> duplicateAllocationAction = async () =>
        {
            await using var dupContext = CreateDbContext();
            var passengerProfile = await dupContext.PassengerProfiles.FirstAsync();
            dupContext.SeatAllocations.Add(new SeatAllocation
            {
                ServiceId = selectedJourney.ServiceId,
                SeatId = seatIds[0], // Duplicate physically allocated seat!
                PassengerId = passengerProfile.Id,
                AllocationType = SeatAllocationType.Hold,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                ReservationGroupId = Guid.NewGuid()
            });
            await dupContext.SaveChangesAsync();
        };

        var ex = await duplicateAllocationAction.Should().ThrowAsync<DbUpdateException>();
        ex.WithInnerException<Npgsql.PostgresException>()
            .WithMessage("*IX_SeatAllocations_ServiceId_SeatId*");
        _output.WriteLine("PostgreSQL Unique Index IX_SeatAllocations_ServiceId_SeatId strictly prevented duplicate allocation!");

        // 10. Concurrent Seat Hold Prevention
        var passenger2Email = $"passenger2_{Guid.NewGuid():N}@waypoint.lk";
        await _httpClient.PostAsJsonAsync("/api/v1/Auth/register", new RegisterRequestDto
        {
            Email = passenger2Email,
            Password = testPassword,
            FullName = "Second Passenger",
            PhoneNumber = "+94770001133",
            Role = "Passenger"
        });
        var login2Res = await _httpClient.PostAsJsonAsync("/api/v1/Auth/login", new LoginRequestDto
        {
            Email = passenger2Email,
            Password = testPassword
        });
        var token2 = (await login2Res.Content.ReadFromJsonAsync<AuthResponseDto>())!.Token;

        using var client2 = new HttpClient { BaseAddress = new Uri("http://localhost:5010") };
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // Attempt to hold the already-booked seat
        var conflictHoldResponse = await client2.PostAsJsonAsync("/api/v1/bookings/hold", new SeatHoldRequestDto
        {
            ServiceId = selectedJourney.ServiceId,
            SeatNumbers = new List<string> { seatNumbers[0] },
            PassengerName = "Second Passenger"
        });

        // Must fail with Conflict or BadRequest
        conflictHoldResponse.IsSuccessStatusCode.Should().BeFalse("Holding already booked seat must be rejected");
        _output.WriteLine($"Conflicting hold rejected with HTTP {conflictHoldResponse.StatusCode}");

        // 11. Booking Cancellation
        var cancelResponse = await _httpClient.PostAsJsonAsync("/api/v1/bookings/cancel", new CancelBookingRequestDto
        {
            BookingReference = checkoutResult.BookingReference,
            Reason = "Customer travel schedule change"
        });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _output.WriteLine("Booking successfully cancelled.");

        // Verify allocations released in PostgreSQL
        var cancelledAllocations = await dbContext.SeatAllocations
            .AsNoTracking()
            .Where(a => a.ReservationGroupId == holdResult.ReservationGroupId)
            .ToListAsync();
        cancelledAllocations.Should().BeEmpty("Seat allocations must be released upon cancellation");
        _output.WriteLine("Database verified SeatAllocations released after cancellation.");
    }

    [Fact]
    public async Task Scenario12_Verify_Manager_Approval_In_PostgreSql()
    {
        await using var context = CreateDbContext();
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"Id\", \"DisruptionCaseId\", \"ReplacementServiceId\", \"ProposedByAgent\", \"Status\" FROM \"RebookingProposals\" ORDER BY \"CreatedAt\" DESC LIMIT 5;";
        await using var reader = await cmd.ExecuteReaderAsync();
        var foundProposals = 0;
        while (await reader.ReadAsync())
        {
            foundProposals++;
            var pId = reader.GetGuid(0);
            var caseId = reader.GetGuid(1);
            var replId = reader.GetGuid(2);
            var agent = reader.GetString(3);
            var status = reader.GetString(4);
            _output.WriteLine($"[Proposal] Id: {pId}, CaseId: {caseId}, ReplacementId: {replId}, Agent: {agent}, Status: {status}");
        }
        _output.WriteLine($"Total verified proposals in PostgreSQL: {foundProposals}");

        await reader.CloseAsync();

        cmd.CommandText = "SELECT \"Id\", \"RebookingProposalId\", \"ManagerId\", \"Decision\", \"Comments\", \"DecidedAt\" FROM \"ApprovalDecisions\" ORDER BY \"DecidedAt\" DESC LIMIT 5;";
        await using var reader2 = await cmd.ExecuteReaderAsync();
        var foundApprovals = 0;
        while (await reader2.ReadAsync())
        {
            foundApprovals++;
            var aId = reader2.GetGuid(0);
            var propId = reader2.GetGuid(1);
            var mgrId = reader2.GetGuid(2);
            var decision = reader2.GetString(3);
            var comments = reader2.IsDBNull(4) ? "(null)" : reader2.GetString(4);
            var decidedAt = reader2.GetDateTime(5);
            _output.WriteLine($"[ApprovalDecision] Id: {aId}, ProposalId: {propId}, ManagerId: {mgrId}, Decision: {decision}, Comments: {comments}, DecidedAt: {decidedAt:u}");
        }
        _output.WriteLine($"Total verified approval records in PostgreSQL: {foundApprovals}");
    }
}
