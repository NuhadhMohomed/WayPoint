using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WayPoint.API.Controllers;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

/// <summary>
/// Security Fix 03: Seat Hold Concurrency and Double Booking Prevention Tests.
/// Validates that:
/// 1. Simultaneous hold requests for the same seat allow exactly one winner and return 409 Conflict for losers.
/// 2. Concurrent holds for different physical seats succeed in parallel without contention.
/// 3. Multi-seat reservations remain strictly atomic and consistent.
/// 4. Expired holds dynamically release their allocations and become available for new passengers.
/// 5. Manually released holds become immediately available.
/// 6. Confirmed bookings maintain permanent allocations preventing conflicting holds.
/// 7. Simultaneous checkout requests are idempotent and do not create duplicate bookings.
/// 8. Expired hold checkouts are rejected with 410 Gone.
/// 9. Payment failures do not create corrupt or orphaned seat allocations.
/// 10. Passenger ownership security (Fix 01) remains strictly enforced.
/// 11. Payment transaction verification (Fix 02) remains strictly enforced.
/// 12. Repeated high-concurrency races produce zero intermittent double bookings.
/// </summary>
public class SeatConcurrencySecurityTests
{
    private readonly IConfiguration _configuration;

    public SeatConcurrencySecurityTests()
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
    // TEST 1 & 2: Two passengers simultaneously attempting to hold the same seat
    // Exactly one competing request succeeds (200 OK) and one fails (409 Conflict)
    // =========================================================================

    [Fact]
    public async Task SimultaneousHoldRequests_SameSeat_TwoPassengers_ExactlyOneSucceeds_AndOneReturns409Conflict()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_SameSeat_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var serviceB = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserB.Id, "Passenger"));

        var controllerA = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");
        var controllerB = CreateSeatHoldController(serviceB, context, testData.UserB.Id, "Passenger");

        var requestA = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        };

        var requestB = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerB.Id
        };

        // Act: Execute truly simultaneous requests via Task.WhenAll
        var taskA = controllerA.CreateSeatHold(requestA, CancellationToken.None);
        var taskB = controllerB.CreateSeatHold(requestB, CancellationToken.None);

        var results = await Task.WhenAll(taskA, taskB);

        // Assert: Exactly one 200 OK and exactly one 409 Conflict
        var okCount = results.Count(r => r.Result is OkObjectResult);
        var conflictCount = results.Count(r => r.Result is ConflictObjectResult || (r.Result is ObjectResult obj && obj.StatusCode == StatusCodes.Status409Conflict));

        Assert.Equal(1, okCount);
        Assert.Equal(1, conflictCount);

        // Verify database state: Exactly one active hold and one allocation for Seat 1A
        var activeHolds = await context.SeatHolds
            .Where(sh => sh.ServiceId == testData.Service.Id && sh.Seat.SeatNumber == "1A" && sh.Status == SeatHoldStatus.Held)
            .ToListAsync();
        Assert.Single(activeHolds);

        var activeAllocations = await context.SeatAllocations
            .Where(sa => sa.ServiceId == testData.Service.Id && sa.Seat.SeatNumber == "1A")
            .ToListAsync();
        Assert.Single(activeAllocations);
        Assert.Equal(activeHolds[0].PassengerId, activeAllocations[0].PassengerId);
    }

    // =========================================================================
    // TEST 3: Different passengers holding different seats concurrently
    // =========================================================================

    [Fact]
    public async Task ConcurrentHoldRequests_DifferentSeats_BothPassengersSucceed()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_DifferentSeats_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var serviceB = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserB.Id, "Passenger"));

        var controllerA = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");
        var controllerB = CreateSeatHoldController(serviceB, context, testData.UserB.Id, "Passenger");

        var requestA = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        };

        var requestB = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2B" },
            PassengerId = testData.PassengerB.Id
        };

        // Act: Execute concurrently
        var taskA = controllerA.CreateSeatHold(requestA, CancellationToken.None);
        var taskB = controllerB.CreateSeatHold(requestB, CancellationToken.None);

        var results = await Task.WhenAll(taskA, taskB);

        // Assert: Both succeed with 200 OK
        Assert.IsType<OkObjectResult>(results[0].Result);
        Assert.IsType<OkObjectResult>(results[1].Result);

        var allocations = await context.SeatAllocations
            .Where(sa => sa.ServiceId == testData.Service.Id)
            .ToListAsync();
        Assert.Equal(2, allocations.Count);
    }

    // =========================================================================
    // TEST 4: Multi-seat reservations remain atomic and consistent
    // =========================================================================

    [Fact]
    public async Task MultiSeatReservations_ConcurrentHold_AtomicityMaintained()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_MultiSeat_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var serviceB = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserB.Id, "Passenger"));

        var controllerA = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");
        var controllerB = CreateSeatHoldController(serviceB, context, testData.UserB.Id, "Passenger");

        // Passenger A wants ["1A", "2B"], Passenger B wants ["2B", "3C"] -> Contention on "2B"
        var requestA = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "2B" },
            PassengerId = testData.PassengerA.Id
        };

        var requestB = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2B", "3C" },
            PassengerId = testData.PassengerB.Id
        };

        // Act
        var taskA = controllerA.CreateSeatHold(requestA, CancellationToken.None);
        var taskB = controllerB.CreateSeatHold(requestB, CancellationToken.None);

        var results = await Task.WhenAll(taskA, taskB);

        // Assert: Exactly one succeeds, one gets 409 Conflict
        var okCount = results.Count(r => r.Result is OkObjectResult);
        var conflictCount = results.Count(r => r.Result is ConflictObjectResult || (r.Result is ObjectResult obj && obj.StatusCode == StatusCodes.Status409Conflict));

        Assert.Equal(1, okCount);
        Assert.Equal(1, conflictCount);

        // Assert atomicity: The successful passenger got both seats, loser got 0 seats
        var winnerResult = results.First(r => r.Result is OkObjectResult);
        var okResult = (OkObjectResult)winnerResult.Result!;
        var holdDto = Assert.IsType<SeatHoldResponseDto>(okResult.Value);

        var winnerPassengerId = holdDto.SeatNumbers.Contains("1A") ? testData.PassengerA.Id : testData.PassengerB.Id;

        var passengerAllocations = await context.SeatAllocations
            .Where(sa => sa.ServiceId == testData.Service.Id && sa.PassengerId == winnerPassengerId)
            .ToListAsync();
        Assert.Equal(2, passengerAllocations.Count);

        var loserPassengerId = winnerPassengerId == testData.PassengerA.Id ? testData.PassengerB.Id : testData.PassengerA.Id;
        var loserAllocations = await context.SeatAllocations
            .Where(sa => sa.ServiceId == testData.Service.Id && sa.PassengerId == loserPassengerId)
            .ToListAsync();
        Assert.Empty(loserAllocations);
    }

    // =========================================================================
    // TEST 5: Expired holds becoming available
    // =========================================================================

    [Fact]
    public async Task ExpiredSeatHolds_BecomeAvailable_ForNewPassenger()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_ExpiredHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        // Seed an expired hold and allocation for Passenger A on Seat 1A (held 15 minutes ago, expired 5 minutes ago)
        var expiredTime = DateTime.UtcNow.AddMinutes(-5);
        var holdA = new SeatHold
        {
            Id = Guid.NewGuid(),
            ServiceId = testData.Service.Id,
            SeatId = testData.SeatA.Id,
            PassengerId = testData.PassengerA.Id,
            HeldAt = DateTime.UtcNow.AddMinutes(-15),
            HeldUntil = expiredTime,
            Status = SeatHoldStatus.Held // Not yet updated to Expired
        };
        var allocA = new SeatAllocation
        {
            Id = Guid.NewGuid(),
            ServiceId = testData.Service.Id,
            SeatId = testData.SeatA.Id,
            HoldId = holdA.Id,
            PassengerId = testData.PassengerA.Id,
            AllocatedAt = DateTime.UtcNow.AddMinutes(-15),
            ExpiresAt = expiredTime,
            AllocationType = SeatAllocationType.Hold
        };
        await context.SeatHolds.AddAsync(holdA);
        await context.SeatAllocations.AddAsync(allocA);
        await context.SaveChangesAsync();

        var serviceB = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserB.Id, "Passenger"));
        var controllerB = CreateSeatHoldController(serviceB, context, testData.UserB.Id, "Passenger");

        var requestB = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerB.Id
        };

        // Act: Passenger B requests the expired seat
        var result = await controllerB.CreateSeatHold(requestB, CancellationToken.None);

        // Assert: Succeeds with 200 OK
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<SeatHoldResponseDto>(okResult.Value);
        Assert.Contains("1A", responseDto.SeatNumbers);

        // In database, old hold marked Expired, new allocation belongs to Passenger B
        var updatedHoldA = await context.SeatHolds.FindAsync(holdA.Id);
        Assert.NotNull(updatedHoldA);
        Assert.Equal(SeatHoldStatus.Expired, updatedHoldA.Status);

        var activeAllocations = await context.SeatAllocations
            .Where(sa => sa.ServiceId == testData.Service.Id && sa.SeatId == testData.SeatA.Id)
            .ToListAsync();
        Assert.Single(activeAllocations);
        Assert.Equal(testData.PassengerB.Id, activeAllocations[0].PassengerId);
    }

    // =========================================================================
    // TEST 6: Released holds becoming available
    // =========================================================================

    [Fact]
    public async Task ReleasedSeatHolds_BecomeImmediatelyAvailable_ForNewPassenger()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_ReleasedHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var controllerA = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");

        var holdRequestA = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        };

        var holdResult = await controllerA.CreateSeatHold(holdRequestA, CancellationToken.None);
        var okHold = Assert.IsType<OkObjectResult>(holdResult.Result);
        var holdDto = Assert.IsType<SeatHoldResponseDto>(okHold.Value);

        // Passenger A explicitly releases the hold
        var releaseResult = await controllerA.ReleaseSeatHold(holdDto.HoldId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(releaseResult);

        // Passenger B now attempts to hold the same seat
        var serviceB = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserB.Id, "Passenger"));
        var controllerB = CreateSeatHoldController(serviceB, context, testData.UserB.Id, "Passenger");

        var holdRequestB = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerB.Id
        };

        var resultB = await controllerB.CreateSeatHold(holdRequestB, CancellationToken.None);

        // Assert: Passenger B succeeds immediately
        Assert.IsType<OkObjectResult>(resultB.Result);

        var activeAllocations = await context.SeatAllocations
            .Where(sa => sa.ServiceId == testData.Service.Id && sa.SeatId == testData.SeatA.Id)
            .ToListAsync();
        Assert.Single(activeAllocations);
        Assert.Equal(testData.PassengerB.Id, activeAllocations[0].PassengerId);
    }

    // =========================================================================
    // TEST 7: Confirmed bookings preventing conflicting holds
    // =========================================================================

    [Fact]
    public async Task ConfirmedBookings_PreventConflictingSeatHolds()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_ConfirmedBooking_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        // Seed confirmed booking for seat 1A
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = "WP-BOOK01",
            PassengerId = testData.PassengerA.Id,
            ServiceId = testData.Service.Id,
            SeatNumbers = "1A",
            TotalFareAmount = 2850.0m,
            Status = BookingStatus.Confirmed
        };
        var bookingAlloc = new SeatAllocation
        {
            Id = Guid.NewGuid(),
            ServiceId = testData.Service.Id,
            SeatId = testData.SeatA.Id,
            BookingId = booking.Id,
            PassengerId = testData.PassengerA.Id,
            AllocatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.MaxValue,
            AllocationType = SeatAllocationType.Booking
        };
        await context.Bookings.AddAsync(booking);
        await context.SeatAllocations.AddAsync(bookingAlloc);
        await context.SaveChangesAsync();

        var serviceB = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserB.Id, "Passenger"));
        var controllerB = CreateSeatHoldController(serviceB, context, testData.UserB.Id, "Passenger");

        var holdRequest = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerB.Id
        };

        // Act
        var result = await controllerB.CreateSeatHold(holdRequest, CancellationToken.None);

        // Assert: 409 Conflict returned
        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    // =========================================================================
    // TEST 8: Simultaneous checkout requests not generating duplicate bookings
    // =========================================================================

    [Fact]
    public async Task SimultaneousCheckoutRequests_SameHold_IdempotentDuplicateHandling()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_SimultaneousCheckout_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var holdController = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");

        // 1. Create a hold
        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);

        var holdDto = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // 2. Process sandbox payment
        var paymentResult = await serviceA.ProcessSandboxPaymentAsync(new PaymentChargeRequestDto
        {
            HoldId = holdDto.HoldId,
            CardNumber = "4532 1111 2222 0001",
            Amount = testData.Service.BaseFare
        });
        Assert.True(paymentResult.IsSuccess);

        // 3. Simultaneously fire two checkout requests for the same hold and payment txn
        var checkoutController1 = CreateBookingController(serviceA, context, testData.UserA.Id, "Passenger");
        var checkoutController2 = CreateBookingController(serviceA, context, testData.UserA.Id, "Passenger");

        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = holdDto.HoldId,
            PaymentTransactionId = paymentResult.TransactionId,
            PassengerName = "Passenger Alpha"
        };

        var task1 = checkoutController1.Checkout(checkoutRequest, CancellationToken.None);
        var task2 = checkoutController2.Checkout(checkoutRequest, CancellationToken.None);

        var checkoutResults = await Task.WhenAll(task1, task2);

        // Assert: Both return 200 OK with the exact same booking reference (idempotent replay)
        Assert.IsType<OkObjectResult>(checkoutResults[0].Result);
        Assert.IsType<OkObjectResult>(checkoutResults[1].Result);

        var confirm1 = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)checkoutResults[0].Result!).Value);
        var confirm2 = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)checkoutResults[1].Result!).Value);
        Assert.Equal(confirm1.BookingReference, confirm2.BookingReference);

        // In database: Exactly 1 booking entity created
        var bookings = await context.Bookings.Where(b => b.ServiceId == testData.Service.Id).ToListAsync();
        Assert.Single(bookings);

        // In database: Allocation transitioned to permanent Booking
        var alloc = await context.SeatAllocations.SingleAsync(sa => sa.ServiceId == testData.Service.Id && sa.SeatId == testData.SeatA.Id);
        Assert.Equal(SeatAllocationType.Booking, alloc.AllocationType);
        Assert.Equal(DateTime.MaxValue, alloc.ExpiresAt);
    }

    // =========================================================================
    // TEST 9: Payment failures not creating inconsistent seat allocations
    // =========================================================================

    [Fact]
    public async Task PaymentFailures_DoNotCreateInconsistentSeatAllocations()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_PaymentFailure_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var holdController = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");

        // 1. Create a hold
        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);

        var holdDto = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // 2. Process sandbox payment that declines (card ending in 0002)
        var paymentResult = await serviceA.ProcessSandboxPaymentAsync(new PaymentChargeRequestDto
        {
            HoldId = holdDto.HoldId,
            CardNumber = "4532 1111 2222 0002", // Declined
            Amount = testData.Service.BaseFare
        });
        Assert.False(paymentResult.IsSuccess);

        // 3. Attempt checkout with the declined transaction
        var bookingController = CreateBookingController(serviceA, context, testData.UserA.Id, "Passenger");
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = holdDto.HoldId,
            PaymentTransactionId = paymentResult.TransactionId,
            PassengerName = "Passenger Alpha"
        }, CancellationToken.None);

        // Assert: Checkout fails
        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        // No confirmed bookings exist
        var bookings = await context.Bookings.Where(b => b.ServiceId == testData.Service.Id).ToListAsync();
        Assert.Empty(bookings);

        // Allocation remains type Hold, not converted to Booking
        var alloc = await context.SeatAllocations.SingleAsync(sa => sa.ServiceId == testData.Service.Id && sa.SeatId == testData.SeatA.Id);
        Assert.Equal(SeatAllocationType.Hold, alloc.AllocationType);
    }

    // =========================================================================
    // TEST 10: Existing ownership security remains enforced (Security Fix 01)
    // =========================================================================

    [Fact]
    public async Task OwnershipSecurity_CannotHoldOrReleaseForOtherPassenger()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_Ownership_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        // Caller is Passenger A
        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var controllerA = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");

        // Attempt to create a hold specifying Passenger B's ID (spoofing)
        var spoofRequest = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerB.Id // Spoofed ID
        };

        var result = await controllerA.CreateSeatHold(spoofRequest, CancellationToken.None);
        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    // =========================================================================
    // TEST 11: Existing payment transaction verification remains enforced (Security Fix 02)
    // =========================================================================

    [Fact]
    public async Task PaymentVerification_UnverifiedPaymentTransaction_Rejected()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_PaymentVerify_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedConcurrencyTestDataAsync(context);

        var serviceA = new BookingService(context, _configuration, CreateMockCurrentUserService(testData.UserA.Id, "Passenger"));
        var holdController = CreateSeatHoldController(serviceA, context, testData.UserA.Id, "Passenger");

        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);

        var holdDto = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // Attempt checkout with forged transaction ID not in database
        var bookingController = CreateBookingController(serviceA, context, testData.UserA.Id, "Passenger");
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = holdDto.HoldId,
            PaymentTransactionId = "TXN-FORGED-9999",
            PassengerName = "Passenger Alpha"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    // =========================================================================
    // TEST 12: Repeated concurrent runs to expose intermittent race conditions
    // =========================================================================

    [Fact]
    public async Task RepeatedConcurrentRuns_20Rounds_ZeroDoubleAllocations()
    {
        // Run 20 rounds of high-concurrency races with 4 competing passengers each
        for (int round = 0; round < 20; round++)
        {
            var dbName = $"ConcurrencyTest_LoopRound_{round}_{Guid.NewGuid()}";
            using var context = CreateDbContext(dbName);
            var testData = await SeedConcurrencyTestDataAsync(context);

            // Create 4 passengers
            var users = new List<User>();
            var passengers = new List<PassengerProfile>();
            var controllers = new List<SeatHoldController>();

            var role = await context.Roles.FirstAsync(r => r.RoleName == "Passenger");
            for (int i = 0; i < 4; i++)
            {
                var user = new User { Id = Guid.NewGuid(), Email = $"p{i}@race.lk", FullName = $"Race User {i}", RoleId = role.Id };
                var passenger = new PassengerProfile { Id = Guid.NewGuid(), UserId = user.Id, User = user };
                users.Add(user);
                passengers.Add(passenger);

                await context.Users.AddAsync(user);
                await context.PassengerProfiles.AddAsync(passenger);
            }
            await context.SaveChangesAsync();

            for (int i = 0; i < 4; i++)
            {
                var svc = new BookingService(context, _configuration, CreateMockCurrentUserService(users[i].Id, "Passenger"));
                controllers.Add(CreateSeatHoldController(svc, context, users[i].Id, "Passenger"));
            }

            // All 4 passengers race for Seat "1A"
            var tasks = controllers.Select((c, i) => c.CreateSeatHold(new SeatHoldRequestDto
            {
                ServiceId = testData.Service.Id,
                SeatNumbers = new List<string> { "1A" },
                PassengerId = passengers[i].Id
            }, CancellationToken.None)).ToList();

            var results = await Task.WhenAll(tasks);

            var okCount = results.Count(r => r.Result is OkObjectResult);
            var conflictCount = results.Count(r => r.Result is ConflictObjectResult || (r.Result is ObjectResult obj && obj.StatusCode == StatusCodes.Status409Conflict));

            Assert.True(okCount == 1, $"Round {round}: Expected exactly 1 winner but got {okCount}");
            Assert.True(conflictCount == 3, $"Round {round}: Expected exactly 3 conflicts but got {conflictCount}");

            var seatAllocations = await context.SeatAllocations
                .Where(sa => sa.ServiceId == testData.Service.Id && sa.SeatId == testData.SeatA.Id)
                .ToListAsync();
            Assert.Single(seatAllocations);
        }
    }

    // =========================================================================
    // TEST INFRASTRUCTURE HELPERS
    // =========================================================================

    private static WayPointDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WayPointDbContext(options);
    }

    private static ICurrentUserService CreateMockCurrentUserService(Guid? userId, string? role)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(m => m.UserId).Returns(userId);
        mock.Setup(m => m.Role).Returns(role);
        mock.Setup(m => m.IsAuthenticated).Returns(userId.HasValue);
        mock.Setup(m => m.IsStaff).Returns(role is "Admin" or "TransportManager" or "Operator" or "Conductor");
        mock.Setup(m => m.IsAdmin).Returns(role == "Admin");
        mock.Setup(m => m.IsPassenger).Returns(role == "Passenger");
        mock.Setup(m => m.IsInRole(It.IsAny<string>())).Returns((string r) => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        return mock.Object;
    }

    private static SeatHoldController CreateSeatHoldController(
        IBookingService bookingService, WayPointDbContext context, Guid userId, string role)
    {
        var logger = new Mock<ILogger<SeatHoldController>>();
        var currentUserService = CreateMockCurrentUserService(userId, role);
        var controller = new SeatHoldController(bookingService, logger.Object, context, currentUserService);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new("role", role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private static BookingController CreateBookingController(
        IBookingService bookingService, WayPointDbContext context, Guid userId, string role)
    {
        var logger = new Mock<ILogger<BookingController>>();
        var controller = new BookingController(bookingService, context, logger.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new("role", role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private class ConcurrencyTestData
    {
        public required User UserA { get; set; }
        public required PassengerProfile PassengerA { get; set; }
        public required User UserB { get; set; }
        public required PassengerProfile PassengerB { get; set; }
        public required Service Service { get; set; }
        public required Seat SeatA { get; set; }
        public required Seat SeatB { get; set; }
        public required Seat SeatC { get; set; }
    }

    private static async Task<ConcurrencyTestData> SeedConcurrencyTestDataAsync(WayPointDbContext context)
    {
        var passengerRole = new Role { Id = Guid.NewGuid(), RoleName = "Passenger" };
        var adminRole = new Role { Id = Guid.NewGuid(), RoleName = "Admin" };
        await context.Roles.AddRangeAsync(passengerRole, adminRole);

        var userA = new User { Id = Guid.NewGuid(), Email = "passenger.a@concurrency.lk", FullName = "Passenger Alpha", RoleId = passengerRole.Id };
        var passengerA = new PassengerProfile { Id = Guid.NewGuid(), UserId = userA.Id, User = userA };

        var userB = new User { Id = Guid.NewGuid(), Email = "passenger.b@concurrency.lk", FullName = "Passenger Beta", RoleId = passengerRole.Id };
        var passengerB = new PassengerProfile { Id = Guid.NewGuid(), UserId = userB.Id, User = userB };

        await context.Users.AddRangeAsync(userA, userB);
        await context.PassengerProfiles.AddRangeAsync(passengerA, passengerB);

        var route = new Route
        {
            Id = Guid.NewGuid(),
            RouteCode = "RT-CONCUR-01",
            Name = "Colombo - Kandy High-Speed",
            OriginCity = "Colombo",
            DestinationCity = "Kandy",
            TotalDistanceKm = 115.0m,
            IsActive = true
        };

        var layout = new SeatLayout
        {
            Id = Guid.NewGuid(),
            Name = "Luxury 2+2 Layout",
            TotalRows = 10,
            TotalColumns = 4
        };

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = "NB-CONCUR-999",
            BusClass = BusClass.Luxury,
            TotalSeatCapacity = 40,
            SeatLayoutId = layout.Id
        };

        var seatA = new Seat { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "1A", RowIndex = 1, ColumnIndex = 1 };
        var seatB = new Seat { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "2B", RowIndex = 1, ColumnIndex = 2 };
        var seatC = new Seat { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "3C", RowIndex = 2, ColumnIndex = 1 };

        var service = new Service
        {
            Id = Guid.NewGuid(),
            ServiceCode = "SRV-CONCUR-100",
            RouteId = route.Id,
            Route = route,
            BusId = bus.Id,
            Bus = bus,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(3),
            BaseFare = 2850.0m,
            Status = ServiceStatus.Scheduled
        };

        await context.Routes.AddAsync(route);
        await context.SeatLayouts.AddAsync(layout);
        await context.Buses.AddAsync(bus);
        await context.Seats.AddRangeAsync(seatA, seatB, seatC);
        await context.Services.AddAsync(service);
        await context.SaveChangesAsync();

        return new ConcurrencyTestData
        {
            UserA = userA,
            PassengerA = passengerA,
            UserB = userB,
            PassengerB = passengerB,
            Service = service,
            SeatA = seatA,
            SeatB = seatB,
            SeatC = seatC
        };
    }
}
