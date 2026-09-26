using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/bookings/hold")]
public class SeatHoldController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<SeatHoldController> _logger;

    public SeatHoldController(IBookingService bookingService, ILogger<SeatHoldController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to temporarily hold seats for 10 minutes (US-PASS-003).
    /// Returns 409 Conflict if any seat is already held or booked.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SeatHoldResponseDto>> CreateSeatHold([FromBody] SeatHoldRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bookingService.CreateSeatHoldAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Service Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Seat hold conflict: {Message}", ex.Message);
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Seat Hold Conflict",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Releases an active seat hold before expiration upon passenger cancellation or abandonment.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> ReleaseSeatHold(Guid id, CancellationToken cancellationToken)
    {
        var released = await _bookingService.ReleaseSeatHoldAsync(id, cancellationToken);
        if (!released)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Seat Hold Not Found",
                Detail = $"No active seat hold found with ID '{id}'."
            });
        }

        return Ok(new { success = true, message = "Seat hold released successfully." });
    }

    /// <summary>
    /// Inspects the status and remaining seconds of an active seat hold.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<SeatHoldResponseDto>> GetSeatHoldStatus(Guid id, CancellationToken cancellationToken)
    {
        var hold = await _bookingService.GetSeatHoldStatusAsync(id, cancellationToken);
        if (hold == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Seat Hold Not Found",
                Detail = $"Seat hold '{id}' was not found."
            });
        }

        return Ok(hold);
    }
}
