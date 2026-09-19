using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
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
    /// Retrieves booking history for a passenger or operational manifest (US-PASS-005).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<HistoricalBookingDto>>> GetBookings(
        [FromQuery] Guid? passengerId,
        CancellationToken cancellationToken)
    {
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
