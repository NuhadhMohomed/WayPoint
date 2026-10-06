using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Application.DTOs.Common;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/bookings")]
[Authorize(Policy = "RequirePassenger")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IWayPointDbContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IBookingService bookingService,
        IWayPointDbContext context,
        ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Executes transactional atomic checkout converting an active 10-minute seat hold into a confirmed booking (US-PASS-004).
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<BookingConfirmationDto>> Checkout(
        [FromBody] BookingCheckoutRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting checkout confirmation for Hold ID: {HoldId}", request.HoldId);
            var confirmation = await _bookingService.ExecuteCheckoutAsync(request, cancellationToken);
            return Ok(confirmation);
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
            return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
            {
                Status = StatusCodes.Status410Gone,
                Title = "Hold Expired or Invalid",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during checkout execution.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Checkout Failed",
                Detail = ex.Message
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

        var bookings = await _bookingService.GetPassengerBookingsAsync(passenger.Id, cancellationToken);
        return Ok(bookings);
    }

    /// <summary>
    /// Retrieves booking history with optional pagination (API §2 standardized pagination).
    /// Supports optional passengerId filter and page/pageSize query parameters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBookings(
        [FromQuery] Guid? passengerId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        // When pagination params are provided, return paginated response
        if (page.HasValue || pageSize.HasValue)
        {
            var paginatedResult = await _bookingService.GetPassengerBookingsPaginatedAsync(
                passengerId,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return Ok(paginatedResult);
        }

        // Backward-compatible: return full list when no pagination params
        var bookings = await _bookingService.GetPassengerBookingsAsync(passengerId, cancellationToken);
        return Ok(bookings);
    }

    /// <summary>
    /// Retrieves detailed booking by internal Guid ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HistoricalBookingDto>> GetBookingById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id, cancellationToken);
        if (booking == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Booking Not Found",
                Detail = $"Booking with ID '{id}' was not found."
            });
        }

        return Ok(booking);
    }

    /// <summary>
    /// Retrieves detailed booking by human-readable reference code (e.g., WP-7B92K1).
    /// </summary>
    [HttpGet("reference/{reference}")]
    public async Task<ActionResult<HistoricalBookingDto>> GetBookingByReference(
        string reference,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetBookingByReferenceAsync(reference, cancellationToken);
        if (booking == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Booking Not Found",
                Detail = $"Booking reference '{reference}' was not found."
            });
        }

        return Ok(booking);
    }

    /// <summary>
    /// Evaluates BR-REFUND-001 departure offset and executes passenger cancellation with tiered refund (US-PASS-005).
    /// Tier 1 (>24h): 90% refund | Tier 2 (12-24h): 50% refund | Tier 3 (&lt;12h): 0% non-refundable.
    /// </summary>
    [HttpPost("cancel")]
    public async Task<ActionResult<RefundResponseDto>> CancelBooking(
        [FromBody] CancelBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing cancellation for Reference: {Ref}, Reason: {Reason}",
                request.BookingReference, request.Reason);

            var refund = await _bookingService.CancelBookingAndRefundAsync(request, cancellationToken);
            return Ok(refund);
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
