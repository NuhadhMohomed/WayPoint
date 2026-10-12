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
/// Security Fix 02: Payment Transaction Verification Test Suite.
/// Verifies prevention of fabricated, failed, pending, reused, mismatched, or replayed payment transactions,
/// validates idempotency for duplicate checkout requests, and confirms Flutter/Web workflow compatibility.
/// </summary>
public class PaymentTransactionVerificationSecurityTests
{
    private readonly IConfiguration _configuration;

    public PaymentTransactionVerificationSecurityTests()
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
    // TEST 1: Legitimate successful payment confirms the correct booking
    // =========================================================================

    [Fact]
    public async Task LegitimateSuccessfulPayment_ConfirmsCorrectBooking_WithIssuedTicket()
    {
        // Arrange
        var dbName = $"SecTest_Pay_Legit_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var paymentLogger = new Mock<ILogger<PaymentController>>();
        var paymentController = new PaymentController(bookingService, paymentLogger.Object, context, currentUserService);
        SetControllerUser(paymentController, testData.UserA.Id, "Passenger");

        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        // 1. Process payment via confirm-sandbox-charge (Preset 1: Success)
        var chargeRequest = new PaymentChargeRequestDto
        {
            HoldId = testData.HoldA.Id,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = testData.Service.BaseFare
        };

        var chargeResult = await paymentController.ProcessSandboxCharge(chargeRequest, CancellationToken.None);
        var chargeOk = Assert.IsType<OkObjectResult>(chargeResult.Result);
        var chargeResponse = Assert.IsType<PaymentChargeResponseDto>(chargeOk.Value);
        Assert.True(chargeResponse.IsSuccess);
        Assert.StartsWith("TXN-", chargeResponse.TransactionId);

        // 2. Checkout with the returned legitimate transaction ID
        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = chargeResponse.TransactionId,
            PassengerName = testData.UserA.FullName
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);
        var checkoutOk = Assert.IsType<OkObjectResult>(checkoutResult.Result);
        var confirmation = Assert.IsType<BookingConfirmationDto>(checkoutOk.Value);

        // Assert: Booking created and ticket issued
        Assert.NotNull(confirmation.BookingReference);
        Assert.StartsWith("WP-", confirmation.BookingReference);
        Assert.Equal("Confirmed", confirmation.Status);
        Assert.NotEqual(Guid.Empty, confirmation.TicketId);

        // Assert: In database, hold transitioned to ConvertedToBooking
        var holdInDb = await context.SeatHolds.FindAsync(testData.HoldA.Id);
        Assert.NotNull(holdInDb);
        Assert.Equal(SeatHoldStatus.ConvertedToBooking, holdInDb.Status);

        // Assert: In database, PaymentAttempt is linked to BookingId
        var paymentInDb = await context.PaymentAttempts
            .FirstOrDefaultAsync(p => p.GatewayTransactionId == chargeResponse.TransactionId);
        Assert.NotNull(paymentInDb);
        Assert.Equal(confirmation.BookingId, paymentInDb.BookingId);
        Assert.Equal(PaymentStatus.Success, paymentInDb.Status);
    }

    // =========================================================================
    // TEST 2: Fabricated transaction ID is rejected
    // =========================================================================

    [Fact]
    public async Task FabricatedTransactionId_IsRejected_ReturnsBadRequest_AndNoBookingCreated()
    {
        // Arrange
        var dbName = $"SecTest_Pay_Fabricated_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        // Act: Attempt checkout with fabricated, non-existent transaction ID
        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = "TXN-FABRICATED-FAKE1234",
            PassengerName = "Attacker"
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 400 Bad Request
        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("does not exist", problem.Detail);

        // Assert: No new booking persisted in database
        var bookings = await context.Bookings.Where(b => b.Id != testData.BookingA.Id && b.PassengerId == testData.PassengerA.Id).ToListAsync();
        Assert.Empty(bookings);

        // Assert: Hold remains Held
        var holdInDb = await context.SeatHolds.FindAsync(testData.HoldA.Id);
        Assert.NotNull(holdInDb);
        Assert.Equal(SeatHoldStatus.Held, holdInDb.Status);
    }

    // =========================================================================
    // TEST 3: Failed / declined payment cannot confirm a booking
    // =========================================================================

    [Fact]
    public async Task FailedPayment_CannotConfirmBooking_ReturnsBadRequest()
    {
        // Arrange
        var dbName = $"SecTest_Pay_Failed_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var paymentLogger = new Mock<ILogger<PaymentController>>();
        var paymentController = new PaymentController(bookingService, paymentLogger.Object, context, currentUserService);
        SetControllerUser(paymentController, testData.UserA.Id, "Passenger");

        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        // 1. Charge card ending in 0002 (simulated decline)
        var chargeRequest = new PaymentChargeRequestDto
        {
            HoldId = testData.HoldA.Id,
            CardNumber = "4000 0000 0000 0002",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = testData.Service.BaseFare
        };

        var chargeResult = await paymentController.ProcessSandboxCharge(chargeRequest, CancellationToken.None);
        var declinedResult = Assert.IsType<ObjectResult>(chargeResult.Result);
        Assert.Equal(StatusCodes.Status402PaymentRequired, declinedResult.StatusCode);
        var chargeResponse = Assert.IsType<PaymentChargeResponseDto>(declinedResult.Value);
        Assert.False(chargeResponse.IsSuccess);
        Assert.StartsWith("TXN-FAIL-", chargeResponse.TransactionId);

        // 2. Attacker attempts to checkout using the failed transaction ID
        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = chargeResponse.TransactionId,
            PassengerName = testData.UserA.FullName
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 400 Bad Request
        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("Failed", problem.Detail);

        // Assert: Hold remains Held, no booking created
        var holdInDb = await context.SeatHolds.FindAsync(testData.HoldA.Id);
        Assert.NotNull(holdInDb);
        Assert.Equal(SeatHoldStatus.Held, holdInDb.Status);
    }

    // =========================================================================
    // TEST 4: Pending / timeout payment cannot confirm a booking
    // =========================================================================

    [Fact]
    public async Task PendingPayment_CannotConfirmBooking_ReturnsBadRequest()
    {
        // Arrange
        var dbName = $"SecTest_Pay_Pending_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var paymentLogger = new Mock<ILogger<PaymentController>>();
        var paymentController = new PaymentController(bookingService, paymentLogger.Object, context, currentUserService);
        SetControllerUser(paymentController, testData.UserA.Id, "Passenger");

        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        // 1. Charge card ending in 0003 (simulated timeout)
        var chargeRequest = new PaymentChargeRequestDto
        {
            HoldId = testData.HoldA.Id,
            CardNumber = "4000 0000 0000 0003",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = testData.Service.BaseFare
        };

        var chargeResult = await paymentController.ProcessSandboxCharge(chargeRequest, CancellationToken.None);
        var timeoutResult = Assert.IsType<ObjectResult>(chargeResult.Result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, timeoutResult.StatusCode);
        var chargeResponse = Assert.IsType<PaymentChargeResponseDto>(timeoutResult.Value);
        Assert.False(chargeResponse.IsSuccess);
        Assert.StartsWith("TXN-PEND-", chargeResponse.TransactionId);

        // 2. Attacker attempts to checkout using the pending transaction ID
        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = chargeResponse.TransactionId,
            PassengerName = testData.UserA.FullName
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 400 Bad Request
        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("Pending", problem.Detail);
    }

    // =========================================================================
    // TEST 5: Payment belonging to another passenger is rejected
    // =========================================================================

    [Fact]
    public async Task PaymentBelongingToAnotherPassenger_IsRejected_ReturnsForbidden()
    {
        // Arrange
        var dbName = $"SecTest_Pay_CrossPass_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        // Create a legitimate payment under Passenger B
        var paymentB = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-PAID-BY-B-{Guid.NewGuid().ToString()[..6]}",
            Amount = testData.Service.BaseFare,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = testData.HoldB.Id,
            PassengerId = testData.PassengerB.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(paymentB);
        await context.SaveChangesAsync();

        // Passenger A attempts to checkout Passenger A's hold using Passenger B's transaction
        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = paymentB.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 403 Forbidden
        var forbidden = Assert.IsType<ObjectResult>(checkoutResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);

        // Assert: No new booking created for Passenger A
        var bookings = await context.Bookings.Where(b => b.Id != testData.BookingA.Id && b.PassengerId == testData.PassengerA.Id).ToListAsync();
        Assert.Empty(bookings);
    }

    // =========================================================================
    // TEST 6: Payment linked to a different seat hold is rejected
    // =========================================================================

    [Fact]
    public async Task PaymentLinkedToDifferentSeatHold_IsRejected_ReturnsBadRequest()
    {
        // Arrange
        var dbName = $"SecTest_Pay_DiffHold_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        // Passenger A has HoldA and creates a second hold (HoldA2)
        var seatA2 = new Seat
        {
            Id = Guid.NewGuid(),
            SeatLayoutId = testData.SeatA.SeatLayoutId,
            SeatNumber = "3C",
            RowIndex = 3,
            ColumnIndex = 3
        };
        var holdA2 = new SeatHold
        {
            Id = Guid.NewGuid(),
            ServiceId = testData.Service.Id,
            SeatId = seatA2.Id,
            PassengerId = testData.PassengerA.Id,
            Passenger = testData.PassengerA,
            Service = testData.Service,
            Seat = seatA2,
            HeldAt = DateTime.UtcNow,
            HeldUntil = DateTime.UtcNow.AddMinutes(10),
            Status = SeatHoldStatus.Held
        };
        await context.Seats.AddAsync(seatA2);
        await context.SeatHolds.AddAsync(holdA2);

        // Legitimate payment was processed specifically for HoldA
        var paymentHoldA = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-FOR-HOLDA-{Guid.NewGuid().ToString()[..6]}",
            Amount = testData.Service.BaseFare,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = testData.HoldA.Id,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(paymentHoldA);
        await context.SaveChangesAsync();

        // Passenger A attempts to checkout HoldA2 using the transaction linked to HoldA
        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = holdA2.Id,
            PaymentTransactionId = paymentHoldA.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 400 Bad Request
        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("different seat hold", problem.Detail);
    }

    // =========================================================================
    // TEST 7: Incorrect payment amount is rejected
    // =========================================================================

    [Fact]
    public async Task IncorrectPaymentAmount_IsRejected_ReturnsBadRequest()
    {
        // Arrange
        var dbName = $"SecTest_Pay_AmountMismatch_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        // Payment was recorded with underpayment (e.g., Rs. 500 instead of Rs. 2850)
        var underpaidPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = $"TXN-UNDERPAID-{Guid.NewGuid().ToString()[..6]}",
            Amount = 500.0m, // Fare is 2850.0m
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = testData.HoldA.Id,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(underpaidPayment);
        await context.SaveChangesAsync();

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = underpaidPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 400 Bad Request
        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("amount mismatch", problem.Detail);
    }

    // =========================================================================
    // TEST 8: Reused transaction cannot confirm an unrelated booking
    // =========================================================================

    [Fact]
    public async Task ReusedTransaction_CannotConfirmUnrelatedBooking_ReturnsBadRequest()
    {
        // Arrange
        var dbName = $"SecTest_Pay_Replay_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        // Payment was ALREADY used to confirm BookingA
        var usedPayment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            BookingId = testData.BookingA.Id,
            GatewayTransactionId = $"TXN-ALREADY-USED-{Guid.NewGuid().ToString()[..6]}",
            Amount = testData.Service.BaseFare,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = testData.HoldA.Id,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow.AddHours(-1)
        };
        await context.PaymentAttempts.AddAsync(usedPayment);

        // Passenger A creates a fresh new seat hold (HoldA3)
        var seatA3 = new Seat
        {
            Id = Guid.NewGuid(),
            SeatLayoutId = testData.SeatA.SeatLayoutId,
            SeatNumber = "4D",
            RowIndex = 4,
            ColumnIndex = 4
        };
        var holdA3 = new SeatHold
        {
            Id = Guid.NewGuid(),
            ServiceId = testData.Service.Id,
            SeatId = seatA3.Id,
            PassengerId = testData.PassengerA.Id,
            Passenger = testData.PassengerA,
            Service = testData.Service,
            Seat = seatA3,
            HeldAt = DateTime.UtcNow,
            HeldUntil = DateTime.UtcNow.AddMinutes(10),
            Status = SeatHoldStatus.Held
        };
        await context.Seats.AddAsync(seatA3);
        await context.SeatHolds.AddAsync(holdA3);
        await context.SaveChangesAsync();

        // Passenger A attempts to confirm HoldA3 using the already used transaction
        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = holdA3.Id,
            PaymentTransactionId = usedPayment.GatewayTransactionId,
            PassengerName = testData.UserA.FullName
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 400 Bad Request
        var badRequest = Assert.IsType<BadRequestObjectResult>(checkoutResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Payment Verification Failed", problem.Title);
        Assert.Contains("already been used", problem.Detail);

        // Assert: HoldA3 remains Held
        var holdInDb = await context.SeatHolds.FindAsync(holdA3.Id);
        Assert.NotNull(holdInDb);
        Assert.Equal(SeatHoldStatus.Held, holdInDb.Status);
    }

    // =========================================================================
    // TEST 9: Duplicate checkout requests do not create duplicate bookings (Idempotency)
    // =========================================================================

    [Fact]
    public async Task DuplicateCheckoutRequests_HandledIdempotently_DoNotCreateDuplicateBookings()
    {
        // Arrange
        var dbName = $"SecTest_Pay_Idempotent_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        // Create a successful unconsumed payment record for HoldA
        var txnId = $"TXN-IDEMPOTENT-{Guid.NewGuid().ToString()[..6]}";
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = txnId,
            Amount = testData.Service.BaseFare,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = testData.HoldA.Id,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(payment);
        await context.SaveChangesAsync();

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = txnId,
            PassengerName = testData.UserA.FullName
        };

        // Act 1: Initial checkout request
        var result1 = await bookingController.Checkout(checkoutRequest, CancellationToken.None);
        var ok1 = Assert.IsType<OkObjectResult>(result1.Result);
        var confirmation1 = Assert.IsType<BookingConfirmationDto>(ok1.Value);

        // Act 2: Duplicate checkout request with exact same parameters (simulated double-click / network retry)
        var result2 = await bookingController.Checkout(checkoutRequest, CancellationToken.None);
        var ok2 = Assert.IsType<OkObjectResult>(result2.Result);
        var confirmation2 = Assert.IsType<BookingConfirmationDto>(ok2.Value);

        // Assert: Both return the identical booking reference and ticket
        Assert.Equal(confirmation1.BookingId, confirmation2.BookingId);
        Assert.Equal(confirmation1.BookingReference, confirmation2.BookingReference);
        Assert.Equal(confirmation1.TicketId, confirmation2.TicketId);

        // Assert: Database has exactly 1 new booking for this hold and passenger
        var bookings = await context.Bookings
            .Where(b => b.Id != testData.BookingA.Id && b.PassengerId == testData.PassengerA.Id && b.ServiceId == testData.Service.Id)
            .ToListAsync();
        Assert.Single(bookings);

        // Assert: Database has exactly 1 ticket
        var tickets = await context.Tickets
            .Where(t => t.BookingId == confirmation1.BookingId)
            .ToListAsync();
        Assert.Single(tickets);
    }

    // =========================================================================
    // TEST 10: Expired or invalid holds are handled safely
    // =========================================================================

    [Fact]
    public async Task ExpiredHold_CannotConfirmBooking_Returns410Gone()
    {
        // Arrange
        var dbName = $"SecTest_Pay_Expired_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        // Set hold expiration in the past
        testData.HoldA.HeldUntil = DateTime.UtcNow.AddMinutes(-5);
        await context.SaveChangesAsync();

        var txnId = $"TXN-EXPIRED-HOLD-{Guid.NewGuid().ToString()[..6]}";
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = txnId,
            Amount = testData.Service.BaseFare,
            Currency = "LKR",
            Status = PaymentStatus.Success,
            HoldId = testData.HoldA.Id,
            PassengerId = testData.PassengerA.Id,
            ProcessedAt = DateTime.UtcNow
        };
        await context.PaymentAttempts.AddAsync(payment);
        await context.SaveChangesAsync();

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = txnId,
            PassengerName = testData.UserA.FullName
        };

        // Act: Attempt checkout on expired hold
        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);

        // Assert: 410 Gone
        var goneResult = Assert.IsType<ObjectResult>(checkoutResult.Result);
        Assert.Equal(StatusCodes.Status410Gone, goneResult.StatusCode);

        // Assert: Hold marked Expired in DB
        var holdInDb = await context.SeatHolds.FindAsync(testData.HoldA.Id);
        Assert.NotNull(holdInDb);
        Assert.Equal(SeatHoldStatus.Expired, holdInDb.Status);
    }

    // =========================================================================
    // TEST 11: Existing ownership security tests still pass (Integration validation)
    // =========================================================================

    [Fact]
    public async Task OwnershipVerification_FromFix01_RemainsEnforcedAcrossEndpoints()
    {
        // Arrange
        var dbName = $"SecTest_Pay_OwnershipIntact_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        // Passenger A attempts to access Passenger B's booking
        var result = await bookingController.GetBookingById(testData.BookingB.Id, CancellationToken.None);
        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    // =========================================================================
    // TEST 12: Legitimate Flutter payment and checkout workflow completes successfully
    // =========================================================================

    [Fact]
    public async Task FlutterWorkflow_ChargeSandboxPayment_ThenExecuteCheckout_SucceedsSeamlessly()
    {
        // Arrange
        var dbName = $"SecTest_Pay_FlutterFlow_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedPaymentTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var paymentLogger = new Mock<ILogger<PaymentController>>();
        var paymentController = new PaymentController(bookingService, paymentLogger.Object, context, currentUserService);
        SetControllerUser(paymentController, testData.UserA.Id, "Passenger");

        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        // Step 1 in Flutter: POST /payments/confirm-sandbox-charge with holdId
        var chargeRequest = new PaymentChargeRequestDto
        {
            HoldId = testData.HoldA.Id,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "NIMAL SILVA",
            ExpiryDate = "08/28",
            Cvv = "123",
            Amount = testData.Service.BaseFare
        };

        var chargeResult = await paymentController.ProcessSandboxCharge(chargeRequest, CancellationToken.None);
        var okCharge = Assert.IsType<OkObjectResult>(chargeResult.Result);
        var chargeDto = Assert.IsType<PaymentChargeResponseDto>(okCharge.Value);
        Assert.True(chargeDto.IsSuccess);
        Assert.NotNull(chargeDto.TransactionId);

        // Step 2 in Flutter: POST /bookings/checkout with returned transactionId
        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldA.Id,
            PaymentTransactionId = chargeDto.TransactionId,
            PassengerName = "Nimal Silva"
        };

        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);
        var okCheckout = Assert.IsType<OkObjectResult>(checkoutResult.Result);
        var confirmation = Assert.IsType<BookingConfirmationDto>(okCheckout.Value);

        Assert.Equal("Confirmed", confirmation.Status);
        Assert.NotEmpty(confirmation.QrCodePayload);
        Assert.NotEqual(Guid.Empty, confirmation.TicketId);
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

    private static void SetControllerUser(ControllerBase controller, Guid userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new("role", role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private class PaymentTestData
    {
        public required User UserA { get; set; }
        public required PassengerProfile PassengerA { get; set; }
        public required SeatHold HoldA { get; set; }
        public required Seat SeatA { get; set; }
        public required Booking BookingA { get; set; }

        public required User UserB { get; set; }
        public required PassengerProfile PassengerB { get; set; }
        public required SeatHold HoldB { get; set; }
        public required Seat SeatB { get; set; }
        public required Booking BookingB { get; set; }

        public required Service Service { get; set; }
    }

    private static async Task<PaymentTestData> SeedPaymentTestDataAsync(WayPointDbContext context)
    {
        var passengerRole = new Role { Id = Guid.NewGuid(), RoleName = "Passenger" };
        await context.Roles.AddAsync(passengerRole);

        // Passenger A
        var userA = new User
        {
            Id = Guid.NewGuid(),
            Email = "passenger.a@waypoint.lk",
            FullName = "Passenger Alpha",
            RoleId = passengerRole.Id
        };
        var passengerA = new PassengerProfile { Id = Guid.NewGuid(), UserId = userA.Id, User = userA };

        // Passenger B
        var userB = new User
        {
            Id = Guid.NewGuid(),
            Email = "passenger.b@waypoint.lk",
            FullName = "Passenger Beta",
            RoleId = passengerRole.Id
        };
        var passengerB = new PassengerProfile { Id = Guid.NewGuid(), UserId = userB.Id, User = userB };

        await context.Users.AddRangeAsync(userA, userB);
        await context.PassengerProfiles.AddRangeAsync(passengerA, passengerB);

        // Transit Infrastructure
        var route = new Route
        {
            Id = Guid.NewGuid(),
            RouteCode = "RT-PAY-01",
            Name = "Colombo - Ella Express",
            OriginCity = "Colombo",
            DestinationCity = "Ella",
            TotalDistanceKm = 205.0m,
            IsActive = true
        };
        var layout = new SeatLayout
        {
            Id = Guid.NewGuid(),
            Name = "Standard 2x2",
            TotalRows = 10,
            TotalColumns = 4
        };
        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = "NB-PAY-001",
            BusClass = BusClass.Luxury,
            TotalSeatCapacity = 40,
            SeatLayoutId = layout.Id
        };
        var seatA = new Seat
        {
            Id = Guid.NewGuid(),
            SeatLayoutId = layout.Id,
            SeatNumber = "1A",
            RowIndex = 1,
            ColumnIndex = 1
        };
        var seatB = new Seat
        {
            Id = Guid.NewGuid(),
            SeatLayoutId = layout.Id,
            SeatNumber = "2B",
            RowIndex = 1,
            ColumnIndex = 2
        };
        var service = new Service
        {
            Id = Guid.NewGuid(),
            ServiceCode = "SRV-PAY-0800",
            RouteId = route.Id,
            Route = route,
            BusId = bus.Id,
            Bus = bus,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(5),
            BaseFare = 2850.0m,
            Status = ServiceStatus.Scheduled
        };

        await context.Routes.AddAsync(route);
        await context.SeatLayouts.AddAsync(layout);
        await context.Buses.AddAsync(bus);
        await context.Seats.AddRangeAsync(seatA, seatB);
        await context.Services.AddAsync(service);

        // Holds
        var holdA = new SeatHold
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            SeatId = seatA.Id,
            PassengerId = passengerA.Id,
            Passenger = passengerA,
            Service = service,
            Seat = seatA,
            HeldAt = DateTime.UtcNow,
            HeldUntil = DateTime.UtcNow.AddMinutes(10),
            Status = SeatHoldStatus.Held
        };
        var holdB = new SeatHold
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            SeatId = seatB.Id,
            PassengerId = passengerB.Id,
            Passenger = passengerB,
            Service = service,
            Seat = seatB,
            HeldAt = DateTime.UtcNow,
            HeldUntil = DateTime.UtcNow.AddMinutes(10),
            Status = SeatHoldStatus.Held
        };
        await context.SeatHolds.AddRangeAsync(holdA, holdB);

        // Pre-existing Bookings for prior history testing
        var bookingA = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = "WP-PAYA01",
            PassengerId = passengerA.Id,
            Passenger = passengerA,
            ServiceId = service.Id,
            Service = service,
            SeatNumbers = "1A",
            TotalFareAmount = 2850.0m,
            Status = BookingStatus.Confirmed
        };
        var bookingB = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = "WP-PAYB02",
            PassengerId = passengerB.Id,
            Passenger = passengerB,
            ServiceId = service.Id,
            Service = service,
            SeatNumbers = "2B",
            TotalFareAmount = 2850.0m,
            Status = BookingStatus.Confirmed
        };
        await context.Bookings.AddRangeAsync(bookingA, bookingB);

        await context.SaveChangesAsync();

        return new PaymentTestData
        {
            UserA = userA,
            PassengerA = passengerA,
            HoldA = holdA,
            SeatA = seatA,
            BookingA = bookingA,
            UserB = userB,
            PassengerB = passengerB,
            HoldB = holdB,
            SeatB = seatB,
            BookingB = bookingB,
            Service = service
        };
    }
}
