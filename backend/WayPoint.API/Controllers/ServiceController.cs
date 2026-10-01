using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/services")]
public sealed class ServiceController : ControllerBase
{
    private readonly IJourneyPlanningService journeyPlanningService;

    public ServiceController(IJourneyPlanningService journeyPlanningService)
    {
        this.journeyPlanningService = journeyPlanningService;
    }

    [HttpGet]
    public async Task<IActionResult> GetServices(
        [FromQuery] DateTime? date,
        [FromQuery] Guid? routeId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        // When pagination params are provided, return paginated response
        if (page.HasValue || pageSize.HasValue)
        {
            var paginatedResult = await journeyPlanningService.GetServicesPaginatedAsync(
                date, routeId, page ?? 1, pageSize ?? 20, cancellationToken);
            return Ok(paginatedResult);
        }

        // Backward-compatible: return full list when no pagination params
        return Ok(await journeyPlanningService.GetServicesAsync(date, routeId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetService(Guid id, CancellationToken cancellationToken)
    {
        var service = await journeyPlanningService.GetServiceAsync(id, cancellationToken);
        return service is null ? NotFound() : Ok(service);
    }

    /// <summary>
    /// Schedule a new departure service (API §4.6).
    /// </summary>
    [Authorize(Policy = "RequireOperator")]
    [HttpPost]
    public async Task<IActionResult> CreateService(
        [FromBody] CreateServiceDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = await journeyPlanningService.CreateServiceAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Route Not Found",
                Detail = ex.Message
            });
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