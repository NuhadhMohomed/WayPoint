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
/// Security Fix 01: Broken Object Level Authorization (BOLA) and Passenger Ownership Verification Tests.
/// Validates that passengers can access only resources they own, spoofed passenger IDs are rejected,
/// unauthenticated requests are denied, and staff/conductor workflows operate properly.
/// </summary>
public class PassengerOwnershipSecurityTests
{
    private readonly IConfiguration _configuration;

    public PassengerOwnershipSecurityTests()
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
    // TEST 1: Passenger A can access their own booking
    // =========================================================================

    [Fact]
    public async Task PassengerA_CanAccessTheirOwnBooking_ByIdReferenceAndList()
    {
        // Arrange
        var dbName = $"SecTest_OwnBooking_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var logger = new Mock<ILogger<BookingController>>();
        var controller = new BookingController(bookingService, context, logger.Object);
        SetControllerUser(controller, testData.UserA.Id, "Passenger");

        // Act 1: Get by internal ID
        var getByIdResult = await controller.GetBookingById(testData.BookingA.Id, CancellationToken.None);
        var okById = Assert.IsType<OkObjectResult>(getByIdResult.Result);
        var bookingDtoById = Assert.IsType<HistoricalBookingDto>(okById.Value);
        Assert.Equal(testData.BookingA.Id, bookingDtoById.BookingId);

        // Act 2: Get by human-readable reference
        var getByRefResult = await controller.GetBookingByReference(testData.BookingA.BookingReference, CancellationToken.None);
        var okByRef = Assert.IsType<OkObjectResult>(getByRefResult.Result);
        var bookingDtoByRef = Assert.IsType<HistoricalBookingDto>(okByRef.Value);
        Assert.Equal(testData.BookingA.BookingReference, bookingDtoByRef.BookingReference);

        // Act 3: List bookings (with no passengerId parameter, defaults to own)
        var getListResult = await controller.GetBookings(null, null, null, CancellationToken.None);
        var okList = Assert.IsType<OkObjectResult>(getListResult);
        var bookings = Assert.IsType<List<HistoricalBookingDto>>(okList.Value);
        Assert.Contains(bookings, b => b.BookingId == testData.BookingA.Id);
        Assert.DoesNotContain(bookings, b => b.BookingId == testData.BookingB.Id);
    }

    // =========================================================================
    // TEST 2: Passenger A cannot access Passenger B's booking
    // =========================================================================

    [Fact]
    public async Task PassengerA_CannotAccessPassengerBBooking_Returns403Forbidden()
    {
        // Arrange
        var dbName = $"SecTest_BOLA_Booking_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var logger = new Mock<ILogger<BookingController>>();
        var controller = new BookingController(bookingService, context, logger.Object);
        SetControllerUser(controller, testData.UserA.Id, "Passenger");

        // Act 1: Attempt to access Passenger B's booking by Guid ID
        var getByIdResult = await controller.GetBookingById(testData.BookingB.Id, CancellationToken.None);
        var statusById = Assert.IsType<ObjectResult>(getByIdResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusById.StatusCode);

        // Act 2: Attempt to access Passenger B's booking by Reference
        var getByRefResult = await controller.GetBookingByReference(testData.BookingB.BookingReference, CancellationToken.None);
        var statusByRef = Assert.IsType<ObjectResult>(getByRefResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusByRef.StatusCode);

        // Act 3: Attempt to list bookings supplying Passenger B's profile ID
        var getListResult = await controller.GetBookings(testData.PassengerB.Id, null, null, CancellationToken.None);
        var statusList = Assert.IsType<ObjectResult>(getListResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusList.StatusCode);
    }

    // =========================================================================
    // TEST 3: Passenger A cannot cancel Passenger B's booking
    // =========================================================================

    [Fact]
    public async Task PassengerA_CannotCancelPassengerBBooking_Returns403Forbidden_AndBookingRemainsConfirmed()
    {
        // Arrange
        var dbName = $"SecTest_Cancel_BOLA_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var logger = new Mock<ILogger<BookingController>>();
        var controller = new BookingController(bookingService, context, logger.Object);
        SetControllerUser(controller, testData.UserA.Id, "Passenger");

        var cancelRequest = new CancelBookingRequestDto
        {
            BookingReference = testData.BookingB.BookingReference,
            Reason = "Malicious cancellation attempt"
        };

        // Act: Passenger A attempts to cancel Passenger B's booking
        var cancelResult = await controller.CancelBooking(cancelRequest, CancellationToken.None);

        // Assert: 403 Forbidden returned
        var statusResult = Assert.IsType<ObjectResult>(cancelResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);

        // Assert: In database, Booking B remains Confirmed
        var bookingInDb = await context.Bookings.FindAsync(testData.BookingB.Id);
        Assert.NotNull(bookingInDb);
        Assert.Equal(BookingStatus.Confirmed, bookingInDb.Status);
    }

    // =========================================================================
    // TEST 4: Passenger A cannot use Passenger B's seat hold
    // =========================================================================

    [Fact]
    public async Task PassengerA_CannotUsePassengerBSeatHold_ForCheckoutOrRelease_Returns403Forbidden()
    {
        // Arrange
        var dbName = $"SecTest_Hold_BOLA_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);

        // BookingController for Checkout test
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetControllerUser(bookingController, testData.UserA.Id, "Passenger");

        // SeatHoldController for Hold operations
        var holdLogger = new Mock<ILogger<SeatHoldController>>();
        var holdController = new SeatHoldController(bookingService, holdLogger.Object, context, currentUserService);
        SetControllerUser(holdController, testData.UserA.Id, "Passenger");

        // Act 1: Passenger A attempts to checkout Passenger B's seat hold
        var checkoutRequest = new BookingCheckoutRequestDto
        {
            HoldId = testData.HoldB.Id,
            PaymentTransactionId = "TXN-TEST-123",
            PassengerName = "Passenger A"
        };
        var checkoutResult = await bookingController.Checkout(checkoutRequest, CancellationToken.None);
        var checkoutStatus = Assert.IsType<ObjectResult>(checkoutResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, checkoutStatus.StatusCode);

        // Act 2: Passenger A attempts to release Passenger B's seat hold
        var releaseResult = await holdController.ReleaseSeatHold(testData.HoldB.Id, CancellationToken.None);
        var releaseStatus = Assert.IsType<ObjectResult>(releaseResult);
        Assert.Equal(StatusCodes.Status403Forbidden, releaseStatus.StatusCode);

        // Act 3: Passenger A attempts to view status of Passenger B's seat hold
        var statusResult = await holdController.GetSeatHoldStatus(testData.HoldB.Id, CancellationToken.None);
        var inspectStatus = Assert.IsType<ObjectResult>(statusResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, inspectStatus.StatusCode);

        // Assert: Hold B remains active in Held state
        var holdInDb = await context.SeatHolds.FindAsync(testData.HoldB.Id);
        Assert.NotNull(holdInDb);
        Assert.Equal(SeatHoldStatus.Held, holdInDb.Status);
    }

    // =========================================================================
    // TEST 5: Passenger A cannot access another passenger's payment information
    // =========================================================================

    [Fact]
    public async Task PassengerA_CannotProcessPaymentForAnotherPassengersHold_Returns403Forbidden()
    {
        // Arrange
        var dbName = $"SecTest_Payment_BOLA_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var logger = new Mock<ILogger<PaymentController>>();
        var controller = new PaymentController(bookingService, logger.Object, context, currentUserService);
        SetControllerUser(controller, testData.UserA.Id, "Passenger");

        var paymentRequest = new PaymentChargeRequestDto
        {
            HoldId = testData.HoldB.Id,
            CardNumber = "4000 0000 0000 0001",
            CardholderName = "PASSENGER A",
            ExpiryDate = "12/28",
            Cvv = "123",
            Amount = 2850.0m
        };

        // Act: Passenger A attempts to charge a payment against Passenger B's hold
        var chargeResult = await controller.ProcessSandboxCharge(paymentRequest, CancellationToken.None);

        // Assert: 403 Forbidden
        var status = Assert.IsType<ObjectResult>(chargeResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
    }

    // =========================================================================
    // TEST 6: Passenger A cannot retrieve another passenger's private ticket
    // =========================================================================

    [Fact]
    public async Task PassengerA_CannotRetrievePassengerBPrivateTicket_Returns403Forbidden()
    {
        // Arrange
        var dbName = $"SecTest_Ticket_BOLA_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var logger = new Mock<ILogger<TicketController>>();
        var controller = new TicketController(bookingService, logger.Object);
        SetControllerUser(controller, testData.UserA.Id, "Passenger");

        // Act: Passenger A attempts to view Passenger B's ticket
        var getTicketResult = await controller.GetTicket(testData.TicketB.Id, CancellationToken.None);

        // Assert: 403 Forbidden
        var status = Assert.IsType<ObjectResult>(getTicketResult);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);

        // Act 2: Passenger B accesses their own ticket
        var userBService = CreateMockCurrentUserService(testData.UserB.Id, "Passenger");
        var bookingServiceB = new BookingService(context, _configuration, userBService);
        var controllerB = new TicketController(bookingServiceB, logger.Object);
        SetControllerUser(controllerB, testData.UserB.Id, "Passenger");
        var ownTicketResult = await controllerB.GetTicket(testData.TicketB.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(ownTicketResult);
        Assert.Equal(200, ok.StatusCode);
    }

    // =========================================================================
    // TEST 7: Requests without valid authentication are rejected
    // =========================================================================

    [Fact]
    public async Task UnauthenticatedRequests_AreRejectedWith401Unauthorized()
    {
        // Arrange
        var dbName = $"SecTest_Unauth_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var unauthUserService = CreateMockCurrentUserService(null, null);
        var bookingService = new BookingService(context, _configuration, unauthUserService);

        var bookingLogger = new Mock<ILogger<BookingController>>();
        var bookingController = new BookingController(bookingService, context, bookingLogger.Object);
        SetUnauthenticatedControllerUser(bookingController);

        var ticketLogger = new Mock<ILogger<TicketController>>();
        var ticketController = new TicketController(bookingService, ticketLogger.Object);
        SetUnauthenticatedControllerUser(ticketController);

        // Act & Assert 1: GetBookings without authentication
        var listResult = await bookingController.GetBookings(null, null, null, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(listResult);

        // Act & Assert 2: GetBookingById without authentication
        var byIdResult = await bookingController.GetBookingById(testData.BookingA.Id, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(byIdResult.Result);

        // Act & Assert 3: Cancel without authentication
        var cancelResult = await bookingController.CancelBooking(
            new CancelBookingRequestDto { BookingReference = testData.BookingA.BookingReference }, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(cancelResult.Result);

        // Act & Assert 4: Checkout without authentication
        var checkoutResult = await bookingController.Checkout(
            new BookingCheckoutRequestDto { HoldId = testData.HoldA.Id, PaymentTransactionId = "TXN" }, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(checkoutResult.Result);

        // Act & Assert 5: GetTicket without authentication
        var ticketResult = await ticketController.GetTicket(testData.TicketA.Id, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(ticketResult);
    }

    // =========================================================================
    // TEST 8: Legitimate authorized administrator / operator / conductor workflows continue working
    // =========================================================================

    [Fact]
    public async Task StaffWorkflows_AdminOperatorConductor_ContinueWorkingAsAuthorized()
    {
        // Arrange
        var dbName = $"SecTest_Staff_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        // 1. Admin workflow: Can access any booking by ID and view full manifest
        var adminUserService = CreateMockCurrentUserService(testData.AdminUser.Id, "Admin");
        var adminBookingService = new BookingService(context, _configuration, adminUserService);
        var bookingLogger = new Mock<ILogger<BookingController>>();
        var adminBookingController = new BookingController(adminBookingService, context, bookingLogger.Object);
        SetControllerUser(adminBookingController, testData.AdminUser.Id, "Admin");

        var adminGetResult = await adminBookingController.GetBookingById(testData.BookingB.Id, CancellationToken.None);
        var adminOk = Assert.IsType<OkObjectResult>(adminGetResult.Result);
        Assert.Equal(200, adminOk.StatusCode);

        // 2. Conductor workflow: Can inspect any ticket and verify QR
        var conductorUserService = CreateMockCurrentUserService(testData.ConductorUser.Id, "Conductor");
        var conductorBookingService = new BookingService(context, _configuration, conductorUserService);
        var ticketLogger = new Mock<ILogger<TicketController>>();
        var ticketController = new TicketController(conductorBookingService, ticketLogger.Object);
        SetControllerUser(ticketController, testData.ConductorUser.Id, "Conductor");

        var conductorTicketResult = await ticketController.GetTicket(testData.TicketB.Id, CancellationToken.None);
        var conductorOk = Assert.IsType<OkObjectResult>(conductorTicketResult);
        Assert.Equal(200, conductorOk.StatusCode);

        // 3. Operator workflow: Can view booking manifest with all passenger bookings
        var operatorUserService = CreateMockCurrentUserService(testData.OperatorUser.Id, "Operator");
        var operatorBookingService = new BookingService(context, _configuration, operatorUserService);
        var operatorController = new BookingController(operatorBookingService, context, bookingLogger.Object);
        SetControllerUser(operatorController, testData.OperatorUser.Id, "Operator");

        var manifestResult = await operatorController.GetBookings(null, null, null, CancellationToken.None);
        var manifestOk = Assert.IsType<OkObjectResult>(manifestResult);
        var allBookings = Assert.IsType<List<HistoricalBookingDto>>(manifestOk.Value);
        Assert.Contains(allBookings, b => b.BookingId == testData.BookingA.Id);
        Assert.Contains(allBookings, b => b.BookingId == testData.BookingB.Id);
    }

    // =========================================================================
    // TEST 9: Spoofing a passenger ID in the request does not bypass authorization
    // =========================================================================

    [Fact]
    public async Task SpoofingPassengerId_InRequest_IsRejectedWith403Forbidden()
    {
        // Arrange
        var dbName = $"SecTest_Spoof_{Guid.NewGuid()}";
        using var context = CreateDbContext(dbName);
        var testData = await SeedSecurityTestDataAsync(context);

        var currentUserService = CreateMockCurrentUserService(testData.UserA.Id, "Passenger");
        var bookingService = new BookingService(context, _configuration, currentUserService);
        var holdLogger = new Mock<ILogger<SeatHoldController>>();
        var holdController = new SeatHoldController(bookingService, holdLogger.Object, context, currentUserService);
        SetControllerUser(holdController, testData.UserA.Id, "Passenger");

        // Act: Passenger A calls CreateSeatHold while supplying Passenger B's PassengerId in request body
        var spoofedHoldRequest = new SeatHoldRequestDto
        {
            ServiceId = testData.Service.Id,
            SeatNumbers = new List<string> { "SPOOF-01" },
            PassengerId = testData.PassengerB.Id, // Spoofed ID
            PassengerName = "Passenger B Spoofed"
        };

        var holdResult = await holdController.CreateSeatHold(spoofedHoldRequest, CancellationToken.None);

        // Assert: 403 Forbidden returned
        var status = Assert.IsType<ObjectResult>(holdResult.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);

        // Act 2: Service-level direct invocation check
        var serviceException = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bookingService.CreateSeatHoldAsync(spoofedHoldRequest)
        );
        Assert.Contains("Cannot create seat hold for another passenger", serviceException.Message);
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

    private static void SetUnauthenticatedControllerUser(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private class SecurityTestData
    {
        public required User UserA { get; set; }
        public required PassengerProfile PassengerA { get; set; }
        public required Booking BookingA { get; set; }
        public required SeatHold HoldA { get; set; }
        public required Ticket TicketA { get; set; }

        public required User UserB { get; set; }
        public required PassengerProfile PassengerB { get; set; }
        public required Booking BookingB { get; set; }
        public required SeatHold HoldB { get; set; }
        public required Ticket TicketB { get; set; }

        public required User AdminUser { get; set; }
        public required User OperatorUser { get; set; }
        public required User ConductorUser { get; set; }

        public required Service Service { get; set; }
    }

    private static async Task<SecurityTestData> SeedSecurityTestDataAsync(WayPointDbContext context)
    {
        var passengerRole = new Role { Id = Guid.NewGuid(), RoleName = "Passenger" };
        var adminRole = new Role { Id = Guid.NewGuid(), RoleName = "Admin" };
        var operatorRole = new Role { Id = Guid.NewGuid(), RoleName = "Operator" };
        var conductorRole = new Role { Id = Guid.NewGuid(), RoleName = "Conductor" };

        await context.Roles.AddRangeAsync(passengerRole, adminRole, operatorRole, conductorRole);

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

        // Staff
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@waypoint.lk",
            FullName = "Administrator User",
            RoleId = adminRole.Id
        };
        var operatorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "operator@waypoint.lk",
            FullName = "Bus Operator User",
            RoleId = operatorRole.Id
        };
        var conductorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "conductor@waypoint.lk",
            FullName = "Bus Conductor User",
            RoleId = conductorRole.Id
        };

        await context.Users.AddRangeAsync(userA, userB, adminUser, operatorUser, conductorUser);
        await context.PassengerProfiles.AddRangeAsync(passengerA, passengerB);

        // Transit Route & Service
        var route = new Route
        {
            Id = Guid.NewGuid(),
            RouteCode = "RT-SEC-01",
            Name = "Colombo - Ella Express",
            OriginCity = "Colombo",
            DestinationCity = "Ella",
            TotalDistanceKm = 205.0m,
            IsActive = true
        };
        var layout = new SeatLayout
        {
            Id = Guid.NewGuid(),
            Name = "Standard Layout",
            TotalRows = 10,
            TotalColumns = 4
        };
        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = "NB-SEC-001",
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
            ServiceCode = "SRV-SEC-0800",
            RouteId = route.Id,
            Route = route,
            BusId = bus.Id,
            Bus = bus,
            DepartureTime = DateTime.UtcNow.AddDays(3),
            ArrivalTime = DateTime.UtcNow.AddDays(3).AddHours(5),
            BaseFare = 2850.0m,
            Status = ServiceStatus.Scheduled
        };

        await context.Routes.AddAsync(route);
        await context.SeatLayouts.AddAsync(layout);
        await context.Buses.AddAsync(bus);
        await context.Seats.AddRangeAsync(seatA, seatB);
        await context.Services.AddAsync(service);

        // Seat Holds
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

        // Bookings
        var bookingA = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = "WP-SECA01",
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
            BookingReference = "WP-SECB02",
            PassengerId = passengerB.Id,
            Passenger = passengerB,
            ServiceId = service.Id,
            Service = service,
            SeatNumbers = "2B",
            TotalFareAmount = 2850.0m,
            Status = BookingStatus.Confirmed
        };

        // Tickets
        var ticketA = new Ticket
        {
            Id = Guid.NewGuid(),
            BookingId = bookingA.Id,
            Booking = bookingA,
            QrCodePayload = "WP|REF:WP-SECA01|SRV:SRV-SEC-0800|SEATS:1A|PASS:Passenger Alpha|HMAC:11111111",
            Status = TicketStatus.Issued
        };
        var ticketB = new Ticket
        {
            Id = Guid.NewGuid(),
            BookingId = bookingB.Id,
            Booking = bookingB,
            QrCodePayload = "WP|REF:WP-SECB02|SRV:SRV-SEC-0800|SEATS:2B|PASS:Passenger Beta|HMAC:22222222",
            Status = TicketStatus.Issued
        };

        await context.SeatHolds.AddRangeAsync(holdA, holdB);
        await context.Bookings.AddRangeAsync(bookingA, bookingB);
        await context.Tickets.AddRangeAsync(ticketA, ticketB);
        await context.SaveChangesAsync();

        return new SecurityTestData
        {
            UserA = userA,
            PassengerA = passengerA,
            BookingA = bookingA,
            HoldA = holdA,
            TicketA = ticketA,
            UserB = userB,
            PassengerB = passengerB,
            BookingB = bookingB,
            HoldB = holdB,
            TicketB = ticketB,
            AdminUser = adminUser,
            OperatorUser = operatorUser,
            ConductorUser = conductorUser,
            Service = service
        };
    }
}
