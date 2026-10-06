using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.API.Controllers;

/// <summary>
/// Manages disruption case lifecycle — logging, listing, and impact assessment.
/// </summary>
[ApiController]
[Route("api/v1/disruptions")]
[Authorize(Policy = "RequireOperator")]
public class DisruptionController : ControllerBase
{
    private readonly IDisruptionService _disruptionService;

    public DisruptionController(IDisruptionService disruptionService)
    {
        _disruptionService = disruptionService;
    }

    /// <summary>
    /// Log a new disruption case. Auto-computes affected passenger count
    /// from confirmed bookings unless a manual override is provided (BR-DISRUPT-001).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogDisruption([FromBody] LogDisruptionDto dto)
    {
        if (dto.DisruptedServiceId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "DisruptedServiceId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Disruption reason is required."
            });
        }

        try
        {
            var result = await _disruptionService.LogDisruptionAsync(dto);
            return CreatedAtAction(nameof(GetDisruptionById), new { id = result.Id }, result);
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
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Disruption Conflict",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// List active disruption cases with optional severity/status filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDisruptions([FromQuery] DisruptionFilterParams filter)
    {
        var result = await _disruptionService.GetDisruptionsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a single disruption case by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisruptionById(Guid id)
    {
        try
        {
            var result = await _disruptionService.GetDisruptionByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Disruption Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Calculate passenger impact metrics for a disruption case.
    /// Returns affected passenger count, revenue at risk, and whether manager approval is required.
    /// </summary>
    [HttpGet("{id:guid}/impact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisruptionImpact(Guid id)
    {
        try
        {
            var result = await _disruptionService.GetDisruptionImpactAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Disruption Not Found",
                Detail = ex.Message
            });
        }
    }
}
