using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/resources")]
[Authorize(Policy = "RequireOperator")]
public class ResourceFeasibilityController : ControllerBase
{
    private readonly IResourceFeasibilityService _feasibilityService;

    public ResourceFeasibilityController(IResourceFeasibilityService feasibilityService)
    {
        _feasibilityService = feasibilityService;
    }

    /// <summary>
    /// Evaluate replacement bus and driver feasibility for a disrupted service.
    /// Checks fleet capacity (BR-RESOURCE-001) and driver rest hours (BR-RESOURCE-002).
    /// </summary>
    [HttpPost("replacement-feasibility")]
    public async Task<IActionResult> EvaluateFeasibility([FromBody] ResourceFeasibilityRequestDto dto)
    {
        if (dto.RequiredSeatCapacity <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Required seat capacity must be greater than zero."
            });
        }

        try
        {
            var result = await _feasibilityService.EvaluateFeasibilityAsync(dto);
            return Ok(result);
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
    }
}
