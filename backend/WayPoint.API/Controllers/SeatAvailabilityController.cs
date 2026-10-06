using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Fleet;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/services")]
public class SeatAvailabilityController : ControllerBase
{
    private readonly ISeatAvailabilityService _seatAvailabilityService;

    public SeatAvailabilityController(ISeatAvailabilityService seatAvailabilityService)
    {
        _seatAvailabilityService = seatAvailabilityService;
    }

    /// <summary>
    /// Get real-time seat availability matrix for a specific service (BR-SEAT-001).
    /// Cross-references seat layout against active holds and confirmed bookings.
    /// </summary>
    [HttpGet("{serviceId:guid}/seats")]
    public async Task<IActionResult> GetSeatAvailability(Guid serviceId)
    {
        try
        {
            var matrix = await _seatAvailabilityService.GetSeatAvailabilityAsync(serviceId);
            return Ok(matrix);
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
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Seat Layout Error",
                Detail = ex.Message
            });
        }
    }
}
