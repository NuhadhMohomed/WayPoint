using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/drivers")]
[Authorize(Policy = "RequireOperator")]
public class DriverController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriverController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    /// <summary>
    /// List drivers with optional filtering and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrivers([FromQuery] DriverFilterParams filter)
    {
        var result = await _driverService.GetDriversAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific driver by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDriverById(Guid id)
    {
        try
        {
            var driver = await _driverService.GetDriverByIdAsync(id);
            return Ok(driver);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Driver Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Add a new driver to the system.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Driver full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.LicenseNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "License number is required."
            });
        }

        try
        {
            var driver = await _driverService.CreateDriverAsync(dto);
            return CreatedAtAction(nameof(GetDriverById), new { id = driver.Id }, driver);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate License Number",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Assign a driver to a service departure with schedule overlap detection (BR-TIME-001)
    /// and 8-hour rest gap validation (BR-RESOURCE-002).
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignDriver([FromBody] AssignDriverDto dto)
    {
        try
        {
            var assignment = await _driverService.AssignDriverAsync(dto);
            return Ok(assignment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Assignment Conflict",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get a driver's assignment timeline (for Gantt chart visualization).
    /// </summary>
    [HttpGet("{id:guid}/assignments")]
    public async Task<IActionResult> GetDriverAssignments(Guid id)
    {
        try
        {
            var assignments = await _driverService.GetDriverAssignmentsAsync(id);
            return Ok(assignments);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Driver Not Found",
                Detail = ex.Message
            });
        }
    }
}
