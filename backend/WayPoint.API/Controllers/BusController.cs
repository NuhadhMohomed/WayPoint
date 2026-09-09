using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/buses")]
[Authorize(Policy = "RequireOperator")]
public class BusController : ControllerBase
{
    private readonly IBusService _busService;

    public BusController(IBusService busService)
    {
        _busService = busService;
    }

    /// <summary>
    /// List fleet buses with optional filtering and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBuses([FromQuery] BusFilterParams filter)
    {
        var result = await _busService.GetBusesAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a single bus by ID including seat layout details.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBusById(Guid id)
    {
        try
        {
            var bus = await _busService.GetBusByIdAsync(id);
            return Ok(bus);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Bus Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Register a new bus in the fleet.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBus([FromBody] CreateBusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RegistrationNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Registration number is required."
            });
        }

        if (dto.TotalSeatCapacity <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Total seat capacity must be greater than zero."
            });
        }

        try
        {
            var bus = await _busService.CreateBusAsync(dto);
            return CreatedAtAction(nameof(GetBusById), new { id = bus.Id }, bus);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate Registration",
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Seat Layout",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Toggle bus maintenance status and log a maintenance record.
    /// </summary>
    [HttpPut("{id:guid}/maintenance")]
    public async Task<IActionResult> ToggleMaintenance(Guid id, [FromBody] MaintenanceToggleDto dto)
    {
        try
        {
            var bus = await _busService.ToggleMaintenanceAsync(id, dto);
            return Ok(bus);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Bus Not Found",
                Detail = ex.Message
            });
        }
    }
}
