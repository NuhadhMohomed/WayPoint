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
using WayPoint.Infrastructure;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

/// <summary>
/// Security Fix 04: Multiple Seat Hold Grouping and Reservation Isolation Test Suite.
/// Verifies that multiple seat holds selected in one reservation attempt share a unique server-generated
/// ReservationGroupId, separate reservation attempts by the same passenger remain strictly isolated,
/// checkout processes only seats belonging to the target group, fare calculation and payment binding
/// operate strictly on the selected group, and previous security protections (Fix 01, 02, 03) remain operational.
/// </summary>
public class ReservationGroupingSecurityTests
{
    private readonly IConfiguration _configuration;

    public ReservationGroupingSecurityTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "SUPER_SECURE_TEST_KEY_THAT_HAS_32_BYTES_MIN!" },
            { "Jwt:Issuer", "WayPoint.TestIssuer" },
            { "Jwt:Audience", "WayPoint.TestAudience" },
            { "TicketSignature:HmacSecret", "TEST_TICKET_SIGNATURE_KEY_HMAC256_32BYTES_OK" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    // =========================================================================
    // TEST 1: Multiple seats selected in one reservation attempt share the same reservation group
    // =========================================================================
    [Fact]
    public async Task MultipleSeats_SelectedInOneAttempt_ShareSameReservationGroupId()
    {
        var dbName = $"SecTest_Group_Multi_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var holdLogger = new Mock<ILogger<SeatHoldController>>();
        var holdController = new SeatHoldController(bookingService, holdLogger.Object, context, currentUserService);
        SetControllerUser(holdController, testData.UserA.Id, "Passenger");

        var request = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        };

        var result = await holdController.CreateSeatHold(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SeatHoldResponseDto>(okResult.Value);

        Assert.NotEqual(Guid.Empty, response.ReservationGroupId);
        Assert.Equal(2, response.SeatNumbers.Count);

        // Verify database holds share the identical ReservationGroupId
        var holdsInDb = await context.SeatHolds
            .Where(sh => sh.ReservationGroupId == response.ReservationGroupId)
            .ToListAsync();

        Assert.Equal(2, holdsInDb.Count);
        Assert.All(holdsInDb, h => Assert.Equal(response.ReservationGroupId, h.ReservationGroupId));
        Assert.All(holdsInDb, h => Assert.Equal(SeatHoldStatus.Held, h.Status));

        // Verify allocations also record the ReservationGroupId
        var allocationsInDb = await context.SeatAllocations
            .Where(sa => sa.ReservationGroupId == response.ReservationGroupId)
            .ToListAsync();

        Assert.Equal(2, allocationsInDb.Count);
        Assert.All(allocationsInDb, a => Assert.Equal(response.ReservationGroupId, a.ReservationGroupId));
    }

    // =========================================================================
    // TEST 2: Two separate reservation attempts by the same passenger receive different group identifiers
    // =========================================================================
    [Fact]
    public async Task TwoSeparateAttempts_BySamePassenger_ReceiveDifferentGroupIdentifiers()
    {
        var dbName = $"SecTest_Group_TwoAttempts_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var holdLogger = new Mock<ILogger<SeatHoldController>>();
        var holdController = new SeatHoldController(bookingService, holdLogger.Object, context, currentUserService);
        SetControllerUser(holdController, testData.UserA.Id, "Passenger");

        // Attempt 1: Hold 1A
        var result1 = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var response1 = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)result1.Result!).Value);

        // Attempt 2: Hold 2A (same passenger, same scheduled service)
        var result2 = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var response2 = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)result2.Result!).Value);

        Assert.NotEqual(response1.ReservationGroupId, response2.ReservationGroupId);

        var hold1 = await context.SeatHolds.SingleAsync(sh => sh.Seat.SeatNumber == "1A");
        var hold2 = await context.SeatHolds.SingleAsync(sh => sh.Seat.SeatNumber == "2A");

        Assert.Equal(response1.ReservationGroupId, hold1.ReservationGroupId);
        Assert.Equal(response2.ReservationGroupId, hold2.ReservationGroupId);
        Assert.NotEqual(hold1.ReservationGroupId, hold2.ReservationGroupId);
    }

    // =========================================================================
    // TEST 3: Checkout for Group A includes only Group A's seats
    // =========================================================================
    [Fact]
    public async Task CheckoutForGroupA_IncludesOnlyGroupASeats_DoesNotCombineUnrelatedHolds()
    {
        var dbName = $"SecTest_Group_CheckoutA_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Group A: seats 1A and 1B (2 seats)
        var holdResultA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultA.Result!).Value);

        // Group B: seat 2A (1 seat, separate attempt by same passenger on same service)
        var holdResultB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultB.Result!).Value);

        // Pay strictly for Group A (2 seats * 2500 = 5000)
        var paymentResultA = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 5000.0m
        }, CancellationToken.None);
        var paymentA = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)paymentResultA.Result!).Value);

        // Checkout Group A
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = paymentA.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var confirmation = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)checkoutResult.Result!).Value);

        // Assert: Booking has strictly 1A and 1B, fare is 5000
        Assert.Equal(2, confirmation.SeatNumbers.Count);
        Assert.Contains("1A", confirmation.SeatNumbers);
        Assert.Contains("1B", confirmation.SeatNumbers);
        Assert.DoesNotContain("2A", confirmation.SeatNumbers);
        Assert.Equal(5000.0m, confirmation.TotalFareAmount);
    }

    // =========================================================================
    // TEST 4: Checkout for Group A does not modify Group B
    // =========================================================================
    [Fact]
    public async Task CheckoutForGroupA_DoesNotModifyGroupB_GroupBRemainsHeld()
    {
        var dbName = $"SecTest_Group_Isolation_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Group A: 1A
        var holdResultA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultA.Result!).Value);

        // Group B: 2A
        var holdResultB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultB.Result!).Value);

        // Pay and checkout Group A
        var payResult = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 2500.0m
        }, CancellationToken.None);
        var paymentA = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payResult.Result!).Value);

        await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = paymentA.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        // Verify in database: Group A is ConvertedToBooking, but Group B is STILL Held!
        using var verifyContext = CreateDbContext(dbName);
        var holdAInDb = await verifyContext.SeatHolds.SingleAsync(h => h.Seat.SeatNumber == "1A");
        var holdBInDb = await verifyContext.SeatHolds.SingleAsync(h => h.Seat.SeatNumber == "2A");

        Assert.Equal(SeatHoldStatus.ConvertedToBooking, holdAInDb.Status);
        Assert.Equal(SeatHoldStatus.Held, holdBInDb.Status);

        var allocB = await verifyContext.SeatAllocations.SingleAsync(sa => sa.ReservationGroupId == groupB.ReservationGroupId);
        Assert.Equal(SeatAllocationType.Hold, allocB.AllocationType);
    }

    // =========================================================================
    // TEST 5: Payment for Group A cannot confirm Group B
    // =========================================================================
    [Fact]
    public async Task PaymentForGroupA_CannotConfirmGroupB_ThrowsException()
    {
        var dbName = $"SecTest_Group_CrossPayment_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Group A: 1A
        var holdResultA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultA.Result!).Value);

        // Group B: 2A
        var holdResultB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultB.Result!).Value);

        // Pay for Group A
        var payResult = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 2500.0m
        }, CancellationToken.None);
        var paymentA = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payResult.Result!).Value);

        // Attempt to checkout Group B using Payment A
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupB.HoldId,
            ReservationGroupId = groupB.ReservationGroupId,
            PaymentTransactionId = paymentA.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("different reservation group", problem.Detail);
    }

    // =========================================================================
    // TEST 6: Fare calculation uses only seats belonging to the selected group
    // =========================================================================
    [Fact]
    public async Task FareCalculation_UsesOnlySeatsInSelectedGroup_RejectsAmountMismatch()
    {
        var dbName = $"SecTest_Group_Fare_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Group A has 2 seats (required fare: 5000.00)
        var holdResultA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultA.Result!).Value);

        // Group B has 1 seat (2500.00)
        await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);

        // If client pays 7500.00 (trying to bundle Group A and Group B into one payment)
        var payResultMismatch = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 7500.0m // Incorrect: Group A only requires 5000.00
        }, CancellationToken.None);
        var paymentMismatch = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payResultMismatch.Result!).Value);

        // Checkout Group A should fail due to fare mismatch
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = paymentMismatch.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("Payment amount mismatch", problem.Detail);
    }

    // =========================================================================
    // TEST 7: A passenger cannot access another passenger's reservation group
    // =========================================================================
    [Fact]
    public async Task PassengerCannotAccess_AnotherPassengersReservationGroup_ReturnsForbidden()
    {
        var dbName = $"SecTest_Group_CrossPassenger_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        // Passenger A creates hold
        var currentUserServiceA = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingServiceA = new BookingService(context, _configuration, currentUserServiceA);
        var (holdControllerA, _, _) = CreateControllers(bookingServiceA, context, testData.UserA.Id);

        var holdResultA = await holdControllerA.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultA.Result!).Value);

        // Passenger B attempts to inspect status, release, and checkout Group A
        var currentUserServiceB = CreateMockCurrentUserService(testData.UserB.Id, "Passenger");
        var bookingServiceB = new BookingService(context, _configuration, currentUserServiceB);
        var (holdControllerB, _, bookingControllerB) = CreateControllers(bookingServiceB, context, testData.UserB.Id);

        // Inspect status
        var statusResult = await holdControllerB.GetSeatHoldStatus(groupA.ReservationGroupId, CancellationToken.None);
        var statusForbidden = Assert.IsType<ObjectResult>(statusResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusForbidden.StatusCode);

        // Release hold
        var releaseResult = await holdControllerB.ReleaseSeatHold(groupA.ReservationGroupId, CancellationToken.None);
        var releaseForbidden = Assert.IsType<ObjectResult>(releaseResult);
        Assert.Equal(StatusCodes.Status403Forbidden, releaseForbidden.StatusCode);

        // Checkout
        var checkoutResult = await bookingControllerB.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = "TXN-ANY-ID",
            PassengerName = testData.UserB.FullName
        }, CancellationToken.None);
        var checkoutForbidden = Assert.IsType<ObjectResult>(checkoutResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, checkoutForbidden.StatusCode);
    }

    // =========================================================================
    // TEST 8: Expired holds within a group are rejected safely
    // =========================================================================
    [Fact]
    public async Task ExpiredHoldsWithinGroup_AreRejectedSafely_RollsBackCleanly()
    {
        var dbName = $"SecTest_Group_Expired_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var group = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // Generate payment
        var payResult = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = group.HoldId,
            ReservationGroupId = group.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 5000.0m
        }, CancellationToken.None);
        var payment = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payResult.Result!).Value);

        // Manually expire holds in DB
        var holds = await context.SeatHolds.Where(h => h.ReservationGroupId == group.ReservationGroupId).ToListAsync();
        foreach (var h in holds)
        {
            h.HeldUntil = DateTime.UtcNow.AddMinutes(-5);
        }
        await context.SaveChangesAsync();

        // Attempt checkout
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = group.HoldId,
            ReservationGroupId = group.ReservationGroupId,
            PaymentTransactionId = payment.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var goneResult = Assert.IsType<ObjectResult>(checkoutResult.Result);
        Assert.Equal(StatusCodes.Status410Gone, goneResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(goneResult.Value);
        Assert.Contains("expired", problem.Detail, StringComparison.OrdinalIgnoreCase);

        // Verify holds transitioned to Expired and allocations removed
        using var verifyContext = CreateDbContext(dbName);
        var expiredHolds = await verifyContext.SeatHolds.Where(h => h.ReservationGroupId == group.ReservationGroupId).ToListAsync();
        Assert.All(expiredHolds, h => Assert.Equal(SeatHoldStatus.Expired, h.Status));

        var remainingAllocations = await verifyContext.SeatAllocations.Where(sa => sa.ReservationGroupId == group.ReservationGroupId).ToListAsync();
        Assert.Empty(remainingAllocations);
    }

    // =========================================================================
    // TEST 9: Group checkout remains atomic
    // =========================================================================
    [Fact]
    public async Task GroupCheckout_RemainsAtomic_OnFailureNoBookingsPersist()
    {
        var dbName = $"SecTest_Group_Atomic_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var group = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // Checkout with nonexistent transaction ID
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = group.HoldId,
            ReservationGroupId = group.ReservationGroupId,
            PaymentTransactionId = "TXN-NONEXISTENT",
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);

        // Database assertions: No booking was created, holds remain Held
        using var verifyContext = CreateDbContext(dbName);
        var bookings = await verifyContext.Bookings.ToListAsync();
        Assert.Empty(bookings);

        var holds = await verifyContext.SeatHolds.Where(h => h.ReservationGroupId == group.ReservationGroupId).ToListAsync();
        Assert.All(holds, h => Assert.Equal(SeatHoldStatus.Held, h.Status));
    }

    // =========================================================================
    // TEST 10: Duplicate checkout requests do not create duplicate bookings or tickets
    // =========================================================================
    [Fact]
    public async Task DuplicateCheckoutRequests_DoNotCreateDuplicateBookingsOrTickets()
    {
        var dbName = $"SecTest_Group_Idempotency_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var group = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        var payResult = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = group.HoldId,
            ReservationGroupId = group.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 5000.0m
        }, CancellationToken.None);
        var payment = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payResult.Result!).Value);

        var checkoutDto = new BookingCheckoutRequestDto
        {
            HoldId = group.HoldId,
            ReservationGroupId = group.ReservationGroupId,
            PaymentTransactionId = payment.TransactionId,
            PassengerName = testData.UserA.FullName
        };

        // First checkout request
        var result1 = await bookingController.Checkout(checkoutDto, CancellationToken.None);
        var conf1 = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)result1.Result!).Value);

        // Second checkout request (duplicate / network replay)
        var result2 = await bookingController.Checkout(checkoutDto, CancellationToken.None);
        var conf2 = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)result2.Result!).Value);

        Assert.Equal(conf1.BookingId, conf2.BookingId);
        Assert.Equal(conf1.BookingReference, conf2.BookingReference);
        Assert.Equal(conf1.TicketId, conf2.TicketId);

        // Verify total bookings in DB is exactly 1
        using var verifyContext = CreateDbContext(dbName);
        var bookings = await verifyContext.Bookings.ToListAsync();
        Assert.Single(bookings);
        var tickets = await verifyContext.Tickets.ToListAsync();
        Assert.Single(tickets);
    }

    // =========================================================================
    // TEST 11: Different reservation groups can be processed independently
    // =========================================================================
    [Fact]
    public async Task DifferentReservationGroups_CanBeProcessedIndependently()
    {
        var dbName = $"SecTest_Group_Independent_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Group A: 1A
        var resA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)resA.Result!).Value);

        // Group B: 2A
        var resB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)resB.Result!).Value);

        // Pay & Checkout Group A
        var payA = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 2500.0m
        }, CancellationToken.None);
        var txA = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payA.Result!).Value);

        var confA = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = txA.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);
        var bookingA = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)confA.Result!).Value);

        // Pay & Checkout Group B
        var payB = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = groupB.HoldId,
            ReservationGroupId = groupB.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 2500.0m
        }, CancellationToken.None);
        var txB = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payB.Result!).Value);

        var confB = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupB.HoldId,
            ReservationGroupId = groupB.ReservationGroupId,
            PaymentTransactionId = txB.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);
        var bookingB = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)confB.Result!).Value);

        Assert.NotEqual(bookingA.BookingId, bookingB.BookingId);
        Assert.NotEqual(bookingA.BookingReference, bookingB.BookingReference);
        Assert.NotEqual(bookingA.TicketId, bookingB.TicketId);

        using var verifyContext = CreateDbContext(dbName);
        var allBookings = await verifyContext.Bookings.ToListAsync();
        Assert.Equal(2, allBookings.Count);
    }

    // =========================================================================
    // TEST 12: Existing single-seat booking works normally
    // =========================================================================
    [Fact]
    public async Task ExistingSingleSeatBooking_WorksNormally()
    {
        var dbName = $"SecTest_Group_SingleSeat_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdRes = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var hold = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdRes.Result!).Value);

        var payRes = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = hold.HoldId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 2500.0m
        }, CancellationToken.None);
        var payment = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payRes.Result!).Value);

        var checkoutRes = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = hold.HoldId,
            PaymentTransactionId = payment.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var confirmation = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)checkoutRes.Result!).Value);
        Assert.Single(confirmation.SeatNumbers);
        Assert.Equal("1A", confirmation.SeatNumbers[0]);
        Assert.Equal(2500.0m, confirmation.TotalFareAmount);
        Assert.Equal("Confirmed", confirmation.Status);
    }

    // =========================================================================
    // TEST 13: Existing multi-seat booking works normally
    // =========================================================================
    [Fact]
    public async Task ExistingMultiSeatBooking_WorksNormally_WithValidQrTicket()
    {
        var dbName = $"SecTest_Group_MultiSeat_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdRes = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B", "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var hold = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdRes.Result!).Value);

        var payRes = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = hold.HoldId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 7500.0m
        }, CancellationToken.None);
        var payment = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payRes.Result!).Value);

        var checkoutRes = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = hold.HoldId,
            PaymentTransactionId = payment.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var confirmation = Assert.IsType<BookingConfirmationDto>(((OkObjectResult)checkoutRes.Result!).Value);
        Assert.Equal(3, confirmation.SeatNumbers.Count);
        Assert.Equal(7500.0m, confirmation.TotalFareAmount);
        Assert.NotEmpty(confirmation.QrCodePayload);

        // Verify ticket can be retrieved
        var ticket = await bookingService.GetTicketByIdAsync(confirmation.TicketId);
        Assert.NotNull(ticket);
        Assert.Equal(confirmation.BookingReference, ticket.BookingReference);
        Assert.Equal(3, ticket.SeatNumbers.Count);
    }

    // =========================================================================
    // TEST 14: Release of Group A does not release Group B
    // =========================================================================
    [Fact]
    public async Task ReleaseGroupA_DoesNotReleaseGroupB_SeatsRemainAvailableIndependently()
    {
        var dbName = $"SecTest_Group_Release_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, _) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Group A: 1A, 1B
        var resA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)resA.Result!).Value);

        // Group B: 2A
        var resB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)resB.Result!).Value);

        // Release Group A using ReservationGroupId
        var releaseResult = await holdController.ReleaseSeatHold(groupA.ReservationGroupId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(releaseResult);

        // Verify: Group A seats are expired and allocations deleted
        using var verifyContext = CreateDbContext(dbName);
        var holdsA = await verifyContext.SeatHolds.Where(h => h.ReservationGroupId == groupA.ReservationGroupId).ToListAsync();
        Assert.All(holdsA, h => Assert.Equal(SeatHoldStatus.Expired, h.Status));

        var allocsA = await verifyContext.SeatAllocations.Where(sa => sa.ReservationGroupId == groupA.ReservationGroupId).ToListAsync();
        Assert.Empty(allocsA);

        // Verify: Group B is STILL active!
        var holdB = await verifyContext.SeatHolds.SingleAsync(h => h.ReservationGroupId == groupB.ReservationGroupId);
        Assert.Equal(SeatHoldStatus.Held, holdB.Status);

        var allocB = await verifyContext.SeatAllocations.SingleAsync(sa => sa.ReservationGroupId == groupB.ReservationGroupId);
        Assert.Equal(SeatAllocationType.Hold, allocB.AllocationType);
    }

    // =========================================================================
    // TEST 15: Simultaneous checkout and cross-group requests do not bypass group isolation
    // =========================================================================
    [Fact]
    public async Task SimultaneousCheckoutAndCrossGroupRequests_DoNotBypassGroupIsolation()
    {
        var dbName = $"SecTest_Group_Concurrency_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, paymentController, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Group A: 1A
        var resA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)resA.Result!).Value);

        // Group B: 2A
        var resB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)resB.Result!).Value);

        // Pay for Group A
        var payA = await paymentController.ProcessSandboxCharge(new PaymentChargeRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 2500.0m
        }, CancellationToken.None);
        var txA = Assert.IsType<PaymentChargeResponseDto>(((OkObjectResult)payA.Result!).Value);

        // Concurrently run valid checkout for Group A and fraudulent checkout for Group B using txA
        var taskValidA = bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = txA.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var taskFraudB = bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupB.HoldId,
            ReservationGroupId = groupB.ReservationGroupId,
            PaymentTransactionId = txA.TransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var results = await Task.WhenAll(taskValidA, taskFraudB);
        var resValid = results[0];
        var resFraud = results[1];

        // Valid checkout succeeds
        Assert.IsType<OkObjectResult>(resValid.Result);

        // Fraudulent checkout using Group A's transaction on Group B is strictly rejected
        var badRequest = Assert.IsType<BadRequestObjectResult>(resFraud.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.True(
            problem.Detail!.Contains("different reservation group") ||
            problem.Detail.Contains("already been used"),
            $"Expected group isolation or replay error but was: {problem.Detail}"
        );
    }

    // =========================================================================
    // TEST 16: Payment with null ReservationGroupId and null HoldId cannot confirm booking (Rejected)
    // =========================================================================
    [Fact]
    public async Task PaymentWithNullReservationGroupId_AndNullHoldId_CannotConfirmBooking_ReturnsBadRequest()
    {
        var dbName = $"SecTest_Group_NullGroup_NullHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Hold a seat for Group A
        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // Directly seed an unlinked payment attempt (null ReservationGroupId, null HoldId)
        var unlinkedPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-UNLINKED-{Guid.NewGuid().ToString()[..6]}",
            Amount = 2500.0m,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = null,
            ReservationGroupId = null,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(unlinkedPayment);
        await context.SaveChangesAsync();

        // Attempt checkout using the unlinked payment
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = unlinkedPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("not linked to this reservation group", problem.Detail);

        // Verify hold remains Held and no booking was created
        var holdInDb = await context.SeatHolds.FindAsync(groupA.HoldId);
        Assert.NotNull(holdInDb);
        Assert.Equal(SeatHoldStatus.Held, holdInDb.Status);
        Assert.Empty(await context.Bookings.ToListAsync());
    }

    // =========================================================================
    // TEST 17: Payment with Guid.Empty ReservationGroupId and null HoldId cannot confirm booking (Rejected)
    // =========================================================================
    [Fact]
    public async Task PaymentWithGuidEmptyReservationGroupId_AndNullHoldId_CannotConfirmBooking_ReturnsBadRequest()
    {
        var dbName = $"SecTest_Group_EmptyGroup_NullHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        var emptyGroupPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-EMPTYGRP-{Guid.NewGuid().ToString()[..6]}",
            Amount = 2500.0m,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = null,
            ReservationGroupId = Guid.Empty,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(emptyGroupPayment);
        await context.SaveChangesAsync();

        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = emptyGroupPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("not linked to this reservation group", problem.Detail);
    }

    // =========================================================================
    // TEST 18: Payment with Guid.Empty ReservationGroupId and Guid.Empty HoldId cannot confirm booking (Rejected)
    // =========================================================================
    [Fact]
    public async Task PaymentWithGuidEmptyReservationGroupId_AndGuidEmptyHoldId_CannotConfirmBooking_ReturnsBadRequest()
    {
        var dbName = $"SecTest_Group_EmptyGroup_EmptyHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        var emptyPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-EMPTYBOTH-{Guid.NewGuid().ToString()[..6]}",
            Amount = 2500.0m,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = Guid.Empty,
            ReservationGroupId = Guid.Empty,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(emptyPayment);
        await context.SaveChangesAsync();

        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = emptyPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("not linked to this reservation group", problem.Detail);
    }

    // =========================================================================
    // TEST 19: Payment with null ReservationGroupId and different HoldId cannot confirm booking (Rejected)
    // =========================================================================
    [Fact]
    public async Task PaymentWithNullReservationGroupId_AndDifferentHoldId_CannotConfirmBooking_ReturnsBadRequest()
    {
        var dbName = $"SecTest_Group_NullGroup_DiffHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResultA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultA.Result!).Value);

        var holdResultB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultB.Result!).Value);

        // Payment is linked to Group A's HoldId, but ReservationGroupId is null
        var legacyPaymentA = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-LEGACY-A-{Guid.NewGuid().ToString()[..6]}",
            Amount = 2500.0m,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = groupA.HoldId,
            ReservationGroupId = null,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(legacyPaymentA);
        await context.SaveChangesAsync();

        // Attempt to checkout Group B with this payment
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupB.HoldId,
            ReservationGroupId = groupB.ReservationGroupId,
            PaymentTransactionId = legacyPaymentA.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("different seat hold", problem.Detail);
    }

    // =========================================================================
    // TEST 20: Payment with Guid.Empty ReservationGroupId and different HoldId cannot confirm booking (Rejected)
    // =========================================================================
    [Fact]
    public async Task PaymentWithGuidEmptyReservationGroupId_AndDifferentHoldId_CannotConfirmBooking_ReturnsBadRequest()
    {
        var dbName = $"SecTest_Group_EmptyGroup_DiffHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResultA = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultA.Result!).Value);

        var holdResultB = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "2A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupB = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResultB.Result!).Value);

        var emptyGroupDiffHoldPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-EMPTYGRP-DIFFHOLD-{Guid.NewGuid().ToString()[..6]}",
            Amount = 2500.0m,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = groupA.HoldId,
            ReservationGroupId = Guid.Empty,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(emptyGroupDiffHoldPayment);
        await context.SaveChangesAsync();

        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupB.HoldId,
            ReservationGroupId = groupB.ReservationGroupId,
            PaymentTransactionId = emptyGroupDiffHoldPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("different seat hold", problem.Detail);
    }

    // =========================================================================
    // TEST 21: Legacy payment with null ReservationGroupId and matching HoldId confirms booking and binds ReservationGroupId
    // =========================================================================
    [Fact]
    public async Task LegacyPaymentWithNullReservationGroupId_MatchingHoldId_ConfirmsBooking_AndBindsReservationGroupId()
    {
        var dbName = $"SecTest_Group_LegacyMatchingHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // Legacy payment created before ReservationGroupId existed
        var legacyPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-LEGACY-MATCH-{Guid.NewGuid().ToString()[..6]}",
            Amount = 2500.0m,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = groupA.HoldId,
            ReservationGroupId = null,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(legacyPayment);
        await context.SaveChangesAsync();

        // Act: Checkout using the legacy payment
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = legacyPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        // Assert: Checkout succeeds
        var okResult = Assert.IsType<OkObjectResult>(checkoutResult.Result);
        var confirmation = Assert.IsType<BookingConfirmationDto>(okResult.Value);
        Assert.Equal("Confirmed", confirmation.Status);

        // Assert: In database, payment is now explicitly bound to the ReservationGroupId
        var paymentInDb = await context.PaymentAttempts.FindAsync(legacyPayment.Id);
        Assert.NotNull(paymentInDb);
        Assert.Equal(groupA.ReservationGroupId, paymentInDb.ReservationGroupId);
        Assert.Equal(confirmation.BookingId, paymentInDb.BookingId);
    }

    // =========================================================================
    // TEST 22: Legacy payment with null ReservationGroupId and sibling HoldId in multi-seat group confirms booking and binds ReservationGroupId
    // =========================================================================
    [Fact]
    public async Task LegacyPaymentWithNullReservationGroupId_SiblingHoldId_ConfirmsBooking_AndBindsReservationGroupId()
    {
        var dbName = $"SecTest_Group_LegacySiblingHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedGroupingTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var (holdController, _, bookingController) = CreateControllers(bookingService, context, testData.UserA.Id);

        // Multi-seat reservation attempt: 1A and 1B
        var holdResult = await holdController.CreateSeatHold(new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "1A", "1B" },
            PassengerId = testData.PassengerA.Id
        }, CancellationToken.None);
        var groupA = Assert.IsType<SeatHoldResponseDto>(((OkObjectResult)holdResult.Result!).Value);

        // Find the second seat hold (sibling hold) in the group
        var groupHolds = await context.SeatHolds
            .Where(sh => sh.ReservationGroupId == groupA.ReservationGroupId)
            .ToListAsync();
        Assert.Equal(2, groupHolds.Count);
        var siblingHold = groupHolds.First(h => h.Id != groupA.HoldId);

        // Legacy payment recorded with siblingHold.Id
        var legacyPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-LEGACY-SIBLING-{Guid.NewGuid().ToString()[..6]}",
            Amount = 5000.0m, // 2 seats * 2500
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = siblingHold.Id,
            ReservationGroupId = null,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(legacyPayment);
        await context.SaveChangesAsync();

        // Act: Checkout passing primary HoldId
        var checkoutResult = await bookingController.Checkout(new BookingCheckoutRequestDto
        {
            HoldId = groupA.HoldId,
            ReservationGroupId = groupA.ReservationGroupId,
            PaymentTransactionId = legacyPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(checkoutResult.Result);
        var confirmation = Assert.IsType<BookingConfirmationDto>(okResult.Value);
        Assert.Equal("Confirmed", confirmation.Status);
        Assert.Equal(2, confirmation.SeatNumbers.Count);

        // Assert: In database, payment is now bound to the ReservationGroupId
        var paymentInDb = await context.PaymentAttempts.FindAsync(legacyPayment.Id);
        Assert.NotNull(paymentInDb);
        Assert.Equal(groupA.ReservationGroupId, paymentInDb.ReservationGroupId);
    }

    // =========================================================================
    // HELPERS & TEST SEEDING
    // =========================================================================

    private static (SeatHoldController, PaymentController, BookingController) CreateControllers(
        IBookingService bookingService,
        WayPointDbContext context,
        Guid userId)
    {
        var currentUserService = CreateMockCurrentUserService(userId, "Passenger");

        var holdLogger = new Mock<ILogger<SeatHoldController>>();
        var holdController = new SeatHoldController(bookingService, holdLogger.Object, context, currentUserService);
        SetControllerUser(holdController, userId, "Passenger");

        var payLogger = new Mock<ILogger<PaymentController>>();
        var paymentController = new PaymentController(bookingService, payLogger.Object, context, currentUserService);
        SetControllerUser(paymentController, userId, "Passenger");

        var bookLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookLogger.Object);
        SetControllerUser(bookingController, userId, "Passenger");

        return (holdController, paymentController, bookingController);
    }

    private static ICurrentUserService CreateMockCurrentUserService(Guid userId, string role = "Passenger")
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(m => m.UserId).Returns(userId);
        mock.Setup(m => m.IsAuthenticated).Returns(true);
        mock.Setup(m => m.IsStaff).Returns(role is "Admin" or "TransportManager" or "Operator");
        mock.Setup(m => m.Role).Returns(role);
        return mock.Object;
    }

    private static void SetControllerUser(ControllerBase controller, Guid userId, string role = "Passenger")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static WayPointDbContext CreateDbContext(string sharedDbName)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("USE_POSTGRES_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrWhiteSpace(dbUrl))
            {
                try
                {
                    var config = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?> { { "ConnectionStrings:DATABASE_URL", dbUrl } })
                        .Build();
                    var connStr = DependencyInjection.ResolveConnectionString(config);

                    var npgsqlOptions = new DbContextOptionsBuilder<WayPointDbContext>()
                        .UseNpgsql(connStr, npgsql => npgsql.EnableRetryOnFailure(3))
                        .Options;
                    var npgsqlContext = new WayPointDbContext(npgsqlOptions);
                    if (npgsqlContext.Database.CanConnect())
                    {
                        return npgsqlContext;
                    }
                }
                catch
                {
                    // Fall back to in-memory store if PostgreSQL is unreachable
                }
            }
        }

        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(sharedDbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WayPointDbContext(options);
    }

    private static async Task<GroupingTestData> SeedGroupingTestDataAsync(WayPointDbContext context)
    {
        var passengerRole = new Role { Id = Guid.NewGuid(), RoleName = "Passenger", Description = "Passenger Role" };
        await context.Roles.AddAsync(passengerRole);

        var userA = new User { Id = Guid.NewGuid(), Email = $"passengerA_{Guid.NewGuid()}@waypoint.lk", FullName = "Passenger A", RoleId = passengerRole.Id, PasswordHash = "hashed", Role = passengerRole };
        var passengerA = new PassengerProfile { Id = Guid.NewGuid(), UserId = userA.Id, User = userA };

        var userB = new User { Id = Guid.NewGuid(), Email = $"passengerB_{Guid.NewGuid()}@waypoint.lk", FullName = "Passenger B", RoleId = passengerRole.Id, PasswordHash = "hashed", Role = passengerRole };
        var passengerB = new PassengerProfile { Id = Guid.NewGuid(), UserId = userB.Id, User = userB };

        await context.Users.AddRangeAsync(userA, userB);
        await context.PassengerProfiles.AddRangeAsync(passengerA, passengerB);

        var route = new Route { Id = Guid.NewGuid(), RouteCode = "EX-GRP-01", Name = "Colombo - Kandy Express", OriginCity = "Makumbura", DestinationCity = "Kandy", TotalDistanceKm = 115.5m };
        var layout = new SeatLayout { Id = Guid.NewGuid(), Name = "Standard 2x2 Layout", TotalRows = 10, TotalColumns = 4 };
        var bus = new Bus { Id = Guid.NewGuid(), RegistrationNumber = "WP-GRP-99", BusClass = BusClass.Luxury, TotalSeatCapacity = 40, SeatLayoutId = layout.Id };

        var seats = new List<Seat>
        {
            new() { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "1A", RowIndex = 1, ColumnIndex = 1 },
            new() { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "1B", RowIndex = 1, ColumnIndex = 2 },
            new() { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "2A", RowIndex = 2, ColumnIndex = 1 },
            new() { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "2B", RowIndex = 2, ColumnIndex = 2 },
            new() { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "3A", RowIndex = 3, ColumnIndex = 1 },
            new() { Id = Guid.NewGuid(), SeatLayoutId = layout.Id, SeatNumber = "3B", RowIndex = 3, ColumnIndex = 2 }
        };

        var service = new Service
        {
            Id = Guid.NewGuid(),
            ServiceCode = "SRV-GRP-100",
            RouteId = route.Id,
            Route = route,
            BusId = bus.Id,
            Bus = bus,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(3),
            BaseFare = 2500.0m,
            Status = ServiceStatus.Scheduled
        };

        await context.Routes.AddAsync(route);
        await context.SeatLayouts.AddAsync(layout);
        await context.Buses.AddAsync(bus);
        await context.Seats.AddRangeAsync(seats);
        await context.Services.AddAsync(service);

        await context.SaveChangesAsync();

        return new GroupingTestData(userA, passengerA, userB, passengerB, service, seats);
    }

    private record GroupingTestData(
        User UserA,
        PassengerProfile PassengerA,
        User UserB,
        PassengerProfile PassengerB,
        Service Service,
        List<Seat> Seats
    );
}
