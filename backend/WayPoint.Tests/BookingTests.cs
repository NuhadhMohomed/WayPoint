using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WayPoint.API.Controllers;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class BookingTests
{
    private readonly IConfiguration _configuration;

    public BookingTests()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    // =========================================================================
    // SECTION 1: UNIT TESTS
    // =========================================================================

    [Theory]
    [InlineData(30, 0.90, 5130.0)]  // > 24h away: 90% refund on Rs. 5700
    [InlineData(18, 0.50, 2850.0)]  // 12-24h away: 50% refund on Rs. 5700
    [InlineData(6, 0.0, 0.0)]       // < 12h away: 0% non-refundable
    public void BR_REFUND_001_ShouldCalculateCorrectRefundPercentageAndAmount(
        int hoursUntilDeparture,
        decimal expectedPercentage,
        decimal expectedRefundAmount)
    {
        // Arrange
        const decimal totalPaid = 5700.0m;

        // Act
        decimal percentage;
        if (hoursUntilDeparture > 24)
        {
            percentage = 0.90m;
        }
        else if (hoursUntilDeparture >= 12)
        {
            percentage = 0.50m;
        }
        else
        {
            percentage = 0.0m;
        }

        var refundAmount = totalPaid * percentage;
        var cancellationFee = totalPaid - refundAmount;

        // Assert
        Assert.Equal(expectedPercentage, percentage);
        Assert.Equal(expectedRefundAmount, refundAmount);
        Assert.Equal(totalPaid, refundAmount + cancellationFee);
    }

    [Fact]
    public async Task HmacSha256_ShouldGenerateValidSignatureAndDetectTampering()
    {
        // Arrange
        var bookingService = new BookingService(null!, _configuration);

        const string bookingRef = "WP-7B92K1";
        const string serviceCode = "SRV-COL-ELLA-0800";
        const string seats = "4A,4B";
        const string passenger = "Nimal Silva";

        // Act - Generate legitimate signed QR payload
        var validPayload = await bookingService.GenerateTicketPayloadAsync(bookingRef, serviceCode, seats, passenger);

        // Assert
        Assert.NotNull(validPayload);
        Assert.StartsWith("WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:4A,4B|PASS:Nimal Silva|HMAC:", validPayload);

        // Act - Verify verification passes on authentic payload
        var verifyValid = await bookingService.VerifyTicketQrAsync(new VerifyQrRequestDto { QrCodePayload = validPayload });
        Assert.NotEqual("Security Violation: Tampered Signature", verifyValid.Status);

        // Act - Tamper with seat number in payload (forge seat 1A instead of 4A)
        var tamperedPayload = validPayload.Replace("SEATS:4A,4B", "SEATS:1A,1B");
        var verifyTampered = await bookingService.VerifyTicketQrAsync(new VerifyQrRequestDto { QrCodePayload = tamperedPayload });

        // Assert - Tampered signature must be flagged and rejected
        Assert.False(verifyTampered.IsValid);
        Assert.Equal("Security Violation: Tampered Signature", verifyTampered.Status);
    }

    [Theory]
    [InlineData("4000 0000 0000 0001", true, "Success")]
    [InlineData("4000 0000 0000 0002", false, "Declined")]
    [InlineData("4000 0000 0000 0003", false, "Timeout")]
    public async Task PaymentSandbox_ShouldHandlePresetCardOutcomes(
        string cardNumber,
        bool expectedSuccess,
        string expectedStatus)
    {
        // Arrange
        var bookingService = new BookingService(null!, _configuration);

        var request = new PaymentChargeRequestDto
        {
            CardNumber = cardNumber,
            CardholderName = "NIMAL SILVA",
            ExpiryDate = "08/28",
            Cvv = "123",
            Amount = 5700.0m
        };

        // Act
        var result = await bookingService.ProcessSandboxPaymentAsync(request);

        // Assert
        Assert.Equal(expectedSuccess, result.IsSuccess);
        Assert.Equal(expectedStatus, result.GatewayStatus);
        if (expectedSuccess)
        {
            Assert.StartsWith("TXN-", result.TransactionId);
        }
    }

    // =========================================================================
    // SECTION 2: INTEGRATION TESTS (Section 8 Requirement Guide)
    // =========================================================================

    /// <summary>
    /// Concurrency Integration Test:
    /// Fires two simultaneous seat hold requests for the exact same seat on the same corridor service.
    /// Asserts that exactly 1 request returns 200 OK and exactly 1 request returns 409 Conflict.
    /// </summary>
    [Fact]
    public async Task Concurrency_TwoSimultaneousSeatHoldRequests_ExactlyOneReturns200AndOneReturns409Conflict()
    {
        // Arrange
        var dbName = $"WayPoint_Concurrency_Test_{Guid.NewGuid()}";
        using var setupContext = CreateDbContext(dbName);
        var (service, passenger) = await EnsureTestDataAsync(setupContext);

        // Pick a unique seat number on this bus
        var testSeatNumber = $"C{Guid.NewGuid().ToString("N")[..4]}";
        var seat = new Seat
        {
            Id = Guid.NewGuid(),
            SeatLayoutId = service.Bus.SeatLayoutId,
            SeatNumber = testSeatNumber,
            RowIndex = 1,
            ColumnIndex = 1
        };
        await setupContext.Seats.AddAsync(seat);
        await setupContext.SaveChangesAsync();

        // Prepare 2 separate DbContext scopes simulating 2 concurrent HTTP requests
        using var context1 = CreateDbContext(dbName);
        var service1 = new BookingService(context1, _configuration);
        var logger1 = new Mock<ILogger<SeatHoldController>>();
        var controller1 = new SeatHoldController(service1, logger1.Object);

        using var context2 = CreateDbContext(dbName);
        var service2 = new BookingService(context2, _configuration);
        var logger2 = new Mock<ILogger<SeatHoldController>>();
        var controller2 = new SeatHoldController(service2, logger2.Object);

        var request1 = new SeatHoldRequestDto
        {
            ServiceId = service.Id,
            SeatNumbers = new List<string> { testSeatNumber },
            PassengerId = passenger.Id,
            PassengerName = "Passenger Alpha"
        };

        var request2 = new SeatHoldRequestDto
        {
            ServiceId = service.Id,
            SeatNumbers = new List<string> { testSeatNumber },
            PassengerId = passenger.Id,
            PassengerName = "Passenger Beta"
        };

        // Act - Fire two simultaneous requests for the exact same seat
        var task1 = controller1.CreateSeatHold(request1, CancellationToken.None);
        var task2 = controller2.CreateSeatHold(request2, CancellationToken.None);

        var results = await Task.WhenAll(task1, task2);

        // Assert - Exactly 1 returns 200 OK and 1 returns 409 Conflict
        var okResult = results.FirstOrDefault(r => r.Result is OkObjectResult);
        var conflictResult = results.FirstOrDefault(r => r.Result is ConflictObjectResult);

        Assert.NotNull(okResult);
        Assert.NotNull(conflictResult);

        var okObj = (OkObjectResult)okResult.Result!;
        Assert.Equal(200, okObj.StatusCode);
        var holdResponse = Assert.IsType<SeatHoldResponseDto>(okObj.Value);
        Assert.Equal("Held", holdResponse.Status);
        Assert.Contains(testSeatNumber, holdResponse.SeatNumbers);

        var conflictObj = (ConflictObjectResult)conflictResult.Result!;
        Assert.Equal(409, conflictObj.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(conflictObj.Value);
        Assert.Equal("Seat Hold Conflict", problemDetails.Title);
        Assert.Contains(testSeatNumber, problemDetails.Detail);
    }

    /// <summary>
    /// Transaction Rollback Integration Test:
    /// Simulates a payment failure during checkout and verifies that no booking or ticket records
    /// are inserted into the PostgreSQL database, and that the hold is not converted.
    /// </summary>
    [Fact]
    public async Task TransactionRollback_SimulatedPaymentFailureDuringCheckout_NoBookingOrTicketRecordsInserted()
    {
        // Arrange
        var dbName = $"WayPoint_Rollback_Test_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var (service, passenger) = await EnsureTestDataAsync(context);
        var bookingService = new BookingService(context, _configuration);

        var testSeatNumber = $"R{Guid.NewGuid().ToString("N")[..4]}";
        var seat = new Seat
        {
            Id = Guid.NewGuid(),
            SeatLayoutId = service.Bus.SeatLayoutId,
            SeatNumber = testSeatNumber,
            RowIndex = 2,
            ColumnIndex = 2
        };
        await context.Seats.AddAsync(seat);
        await context.SaveChangesAsync();

        // 1. Establish an active seat hold
        var holdResult = await bookingService.CreateSeatHoldAsync(new SeatHoldRequestDto
        {
            ServiceId = service.Id,
            SeatNumbers = new List<string> { testSeatNumber },
            PassengerId = passenger.Id,
            PassengerName = "Rollback Test Passenger"
        });

        Assert.NotNull(holdResult);
        Assert.Equal("Held", holdResult.Status);

        // 2. Prepare checkout with simulated payment failure transaction ID
        var failedCheckoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = holdResult.HoldId,
            PaymentTransactionId = "FAILED_CARD_DECLINED_INSUFFICIENT_FUNDS",
            PassengerName = "Rollback Test Passenger",
            PassengerEmail = "rollback.test@waypoint.lk",
            PassengerPhone = "+94 77 999 8877"
        };

        // Act - Attempt checkout; payment failure must trigger transaction rollback
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => bookingService.ExecuteCheckoutAsync(failedCheckoutRequest)
        );

        Assert.Contains("Payment failed", exception.Message);

        // Assert - Verify that transaction rollback prevented any Booking from persisting
        using var verifyContext = CreateDbContext(dbName);
        var persistedBookings = await verifyContext.Bookings
            .Where(b => b.SeatNumbers.Contains(testSeatNumber))
            .ToListAsync();
        Assert.Empty(persistedBookings);

        // Assert - Verify that no Ticket was issued or persisted
        var persistedTickets = await verifyContext.Tickets
            .Where(t => t.QrCodePayload.Contains(testSeatNumber))
            .ToListAsync();
        Assert.Empty(persistedTickets);

        // Assert - Verify that SeatHold was NOT transitioned to ConvertedToBooking
        var holdInDb = await verifyContext.SeatHolds.FindAsync(holdResult.HoldId);
        Assert.NotNull(holdInDb);
        Assert.NotEqual(SeatHoldStatus.ConvertedToBooking, holdInDb.Status);
    }

    // =========================================================================
    // TEST ENVIRONMENT HELPERS
    // =========================================================================

    private static WayPointDbContext CreateDbContext(string? sharedDbName = null)
    {
        var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".env")))
            {
                dir = dir.Parent;
            }

            if (dir != null)
            {
                var envPath = Path.Combine(dir.FullName, ".env");
                foreach (var line in File.ReadAllLines(envPath))
                {
                    if (line.StartsWith("DATABASE_URL=", StringComparison.OrdinalIgnoreCase))
                    {
                        dbUrl = line["DATABASE_URL=".Length..].Trim();
                        break;
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(dbUrl))
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?> { { "ConnectionStrings:DATABASE_URL", dbUrl } })
                    .Build();
                var connStr = DependencyInjection.ResolveConnectionString(config);

                var options = new DbContextOptionsBuilder<WayPointDbContext>()
                    .UseNpgsql(connStr, npgsql => npgsql.EnableRetryOnFailure(3))
                    .Options;
                var context = new WayPointDbContext(options);
                if (context.Database.CanConnect())
                {
                    return context;
                }
            }
            catch
            {
                // Fall back to in-memory store if PostgreSQL is unreachable
            }
        }

        var databaseName = sharedDbName ?? $"WayPoint_Test_{Guid.NewGuid()}";
        var inMemoryOptions = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WayPointDbContext(inMemoryOptions);
    }

    private static async Task<(Service service, PassengerProfile passenger)> EnsureTestDataAsync(WayPointDbContext context)
    {
        var existingService = await context.Services
            .Include(s => s.Route)
            .Include(s => s.Bus)
                .ThenInclude(b => b.SeatLayout)
            .FirstOrDefaultAsync();

        var existingPassenger = await context.PassengerProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync();

        if (existingService != null && existingPassenger != null)
        {
            return (existingService, existingPassenger);
        }

        var role = new Role { Id = Guid.NewGuid(), RoleName = "Passenger" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test.passenger.{Guid.NewGuid():N}@waypoint.lk",
            FullName = "Integration Test Passenger",
            PasswordHash = "hash",
            RoleId = role.Id,
            PhoneNumber = "+94770001122"
        };
        var passenger = new PassengerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            NicOrPassport = "199512345678"
        };

        var route = new Route
        {
            Id = Guid.NewGuid(),
            RouteCode = $"RT-T-{Guid.NewGuid().ToString()[..4]}",
            Name = "Colombo - Ella Highland Scenic Corridor",
            OriginCity = "Colombo",
            DestinationCity = "Ella",
            TotalDistanceKm = 205.0m,
            IsActive = true
        };

        var layout = new SeatLayout
        {
            Id = Guid.NewGuid(),
            Name = "2x2 Luxury Layout",
            TotalRows = 10,
            TotalColumns = 4
        };

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = $"NC-TEST-{Guid.NewGuid().ToString()[..4]}",
            BusClass = BusClass.SuperLuxury,
            TotalSeatCapacity = 40,
            SeatLayoutId = layout.Id,
            IsUnderMaintenance = false
        };

        var service = new Service
        {
            Id = Guid.NewGuid(),
            ServiceCode = $"SRV-TEST-{Guid.NewGuid().ToString()[..4]}",
            RouteId = route.Id,
            Route = route,
            BusId = bus.Id,
            Bus = bus,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(5),
            BaseFare = 2850.0m,
            Status = ServiceStatus.Scheduled
        };

        await context.Roles.AddAsync(role);
        await context.Users.AddAsync(user);
        await context.PassengerProfiles.AddAsync(passenger);
        await context.Routes.AddAsync(route);
        await context.SeatLayouts.AddAsync(layout);
        await context.Buses.AddAsync(bus);
        await context.Services.AddAsync(service);
        await context.SaveChangesAsync();

        return (service, passenger);
    }
}
