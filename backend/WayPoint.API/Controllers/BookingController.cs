using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Application.DTOs.Common;

using WayPoint.API.Middleware;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/bookings")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IWayPointDbContext _context;
    private readonly ILogger<BookingController> _logger;
    private readonly IHostEnvironment? _environment;

    public BookingController(
        IBookingService bookingService,
        IWayPointDbContext context,
        ILogger<BookingController> logger,
        IHostEnvironment? environment = null)
    {
        _bookingService = bookingService;
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Executes transactional atomic checkout converting an active 10-minute seat hold into a confirmed booking (US-PASS-004).
    /// Enforces BOLA ownership verification: Only the hold owner (or authorized staff) may checkout.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<BookingConfirmationDto>> Checkout(
        [FromBody] BookingCheckoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole("Admin") || User.IsInRole("TransportManager") || User.IsInRole("Operator");

        var hold = await _context.SeatHolds
            .Include(h => h.Passenger)
            .FirstOrDefaultAsync(h => (request.ReservationGroupId.HasValue && h.ReservationGroupId == request.ReservationGroupId.Value) ||
                                      h.ReservationGroupId == request.HoldId ||
                                      h.Id == request.HoldId, cancellationToken);

        if (hold == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Hold Not Found",
                Detail = "Active seat hold was not found."
            });
        }

        if (!isStaff && hold.Passenger?.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Checkout ownership violation: User {UserId} attempted to checkout hold {HoldId} belonging to another passenger",
                currentUserId, request.HoldId);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = "You do not have permission to checkout this seat hold."
            });
        }

        try
        {
            _logger.LogInformation("Attempting checkout confirmation for Hold ID: {HoldId}", request.HoldId);
            var confirmation = await _bookingService.ExecuteCheckoutAsync(request, cancellationToken);
            return Ok(confirmation);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Hold Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Checkout failed: {Message}", ex.Message);
            if (ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
                {
                    Status = StatusCodes.Status410Gone,
                    Title = "Hold Expired or Invalid",
                    Detail = ex.Message
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Payment Verification Failed",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during checkout execution.");
            var isDev = _environment?.IsDevelopment() == true;
            var detail = isDev
                ? SensitiveDataSanitizer.Sanitize(ex.Message)
                : "An unexpected error occurred while processing the checkout request. Please try again later.";

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = detail,
                Instance = HttpContext?.Request?.Path
            });
        }
    }

    /// <summary>
    /// Retrieves all scheduled transit corridors and services with their IDs for booking and seat hold operations.
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult<List<ServiceSummaryDto>>> GetServices(CancellationToken cancellationToken)
    {
        var services = await _bookingService.GetAvailableServicesAsync(cancellationToken);
        return Ok(services);
    }

    /// <summary>
    /// Retrieves booking history for the currently authenticated passenger (US-PASS-005, API §4.12).
    /// Resolves PassengerProfile.Id from JWT NameIdentifier claim before querying.
    /// </summary>
    [HttpGet("my-bookings")]
    public async Task<ActionResult<List<HistoricalBookingDto>>> GetMyBookings(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // Resolve PassengerProfile.Id from User.Id (Booking.PassengerId references PassengerProfile, not User)
        var passenger = await _context.PassengerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (passenger == null)
        {
            return Ok(new List<HistoricalBookingDto>());
        }

        try
        {
            var bookings = await _bookingService.GetPassengerBookingsAsync(passenger.Id, cancellationToken);
            return Ok(bookings);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves booking history with optional pagination (API §2 standardized pagination).
    /// Passengers only receive their own bookings. Staff can view all or filter by passengerId.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBookings(
        [FromQuery] Guid? passengerId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole("Admin") || User.IsInRole("TransportManager") || User.IsInRole("Operator");
        Guid? targetPassengerId = passengerId;

        if (!isStaff)
        {
            var passenger = await _context.PassengerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == currentUserId, cancellationToken);

            if (passenger == null)
            {
                if (page.HasValue || pageSize.HasValue)
                {
                    return Ok(new PaginatedResponseDto<HistoricalBookingDto>(new List<HistoricalBookingDto>(), 0, page ?? 1, pageSize ?? 20));
                }
                return Ok(new List<HistoricalBookingDto>());
            }

            // Spoofing check: If passengerId query parameter was specified, it must belong to the caller
            if (passengerId.HasValue && passengerId.Value != passenger.Id && passengerId.Value != passenger.UserId)
            {
                _logger.LogWarning(
                    "Booking history ownership violation: User {UserId} attempted to access bookings of passenger {PassengerId}",
                    currentUserId, passengerId.Value);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have permission to view bookings for another passenger."
                });
            }

            targetPassengerId = passenger.Id;
        }

        try
        {
            // When pagination params are provided, return paginated response
            if (page.HasValue || pageSize.HasValue)
            {
                var paginatedResult = await _bookingService.GetPassengerBookingsPaginatedAsync(
                    targetPassengerId,
                    page ?? 1,
                    pageSize ?? 20,
                    cancellationToken);
                return Ok(paginatedResult);
            }

            // Backward-compatible: return full list when no pagination params
            var bookings = await _bookingService.GetPassengerBookingsAsync(targetPassengerId, cancellationToken);
            return Ok(bookings);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves detailed booking by internal Guid ID.
    /// Enforces BOLA ownership verification: Passengers can only retrieve their own bookings.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HistoricalBookingDto>> GetBookingById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole("Admin") || User.IsInRole("TransportManager") ||
                      User.IsInRole("Operator") || User.IsInRole("Conductor");

        var booking = await _context.Bookings
            .Include(b => b.Passenger)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Booking Not Found",
                Detail = $"Booking with ID '{id}' was not found."
            });
        }

        if (!isStaff && booking.Passenger?.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Booking ownership violation: User {UserId} attempted to access booking {BookingId}",
                currentUserId, id);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = "You do not have permission to view this booking."
            });
        }

        try
        {
            var result = await _bookingService.GetBookingByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves detailed booking by human-readable reference code (e.g., WP-7B92K1).
    /// Enforces BOLA ownership verification: Passengers can only retrieve their own bookings.
    /// </summary>
    [HttpGet("reference/{reference}")]
    public async Task<ActionResult<HistoricalBookingDto>> GetBookingByReference(
        string reference,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole("Admin") || User.IsInRole("TransportManager") ||
                      User.IsInRole("Operator") || User.IsInRole("Conductor");

        var booking = await _context.Bookings
            .Include(b => b.Passenger)
            .FirstOrDefaultAsync(b => b.BookingReference == reference, cancellationToken);

        if (booking == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Booking Not Found",
                Detail = $"Booking reference '{reference}' was not found."
            });
        }

        if (!isStaff && booking.Passenger?.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Booking reference ownership violation: User {UserId} attempted to access booking reference {Reference}",
                currentUserId, reference);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = "You do not have permission to view this booking."
            });
        }

        try
        {
            var result = await _bookingService.GetBookingByReferenceAsync(reference, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Evaluates BR-REFUND-001 departure offset and executes passenger cancellation with tiered refund (US-PASS-005).
    /// Enforces BOLA ownership verification: Passengers can only cancel their own bookings.
    /// </summary>
    [HttpPost("cancel")]
    public async Task<ActionResult<RefundResponseDto>> CancelBooking(
        [FromBody] CancelBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole("Admin") || User.IsInRole("TransportManager") || User.IsInRole("Operator");

        var booking = await _context.Bookings
            .Include(b => b.Passenger)
            .FirstOrDefaultAsync(b => b.BookingReference == request.BookingReference, cancellationToken);

        if (booking == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Booking Not Found",
                Detail = $"Booking reference '{request.BookingReference}' not found."
            });
        }

        if (!isStaff && booking.Passenger?.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Booking cancellation ownership violation: User {UserId} attempted to cancel booking reference {Reference}",
                currentUserId, request.BookingReference);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = "You do not have permission to cancel this booking."
            });
        }

        try
        {
            _logger.LogInformation("Processing cancellation for Reference: {Ref}, Reason: {Reason}",
                request.BookingReference, request.Reason);

            var refund = await _bookingService.CancelBookingAndRefundAsync(request, cancellationToken);
            return Ok(refund);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Booking Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Cancellation Denied",
                Detail = ex.Message
            });
        }
    }
}
