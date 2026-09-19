using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/seats/layouts")]
[Authorize(Policy = "RequireOperator")]
public class SeatLayoutController : ControllerBase
{
    private readonly ISeatLayoutService _seatLayoutService;

    public SeatLayoutController(ISeatLayoutService seatLayoutService)
    {
        _seatLayoutService = seatLayoutService;
    }

    /// <summary>
    /// List all seat layout templates with summary information.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSeatLayouts()
    {
        var layouts = await _seatLayoutService.GetSeatLayoutsAsync();
        return Ok(layouts);
    }

    /// <summary>
    /// Get a specific seat layout with all seat definitions.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSeatLayoutById(Guid id)
    {
        try
        {
            var layout = await _seatLayoutService.GetSeatLayoutByIdAsync(id);
            return Ok(layout);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Seat Layout Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Create a new visual seat layout template with seat coordinate definitions.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSeatLayout([FromBody] CreateSeatLayoutDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Layout name is required."
            });
        }

        try
        {
            var layout = await _seatLayoutService.CreateSeatLayoutAsync(dto);
            return CreatedAtAction(nameof(GetSeatLayoutById), new { id = layout.Id }, layout);
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
    }
}
