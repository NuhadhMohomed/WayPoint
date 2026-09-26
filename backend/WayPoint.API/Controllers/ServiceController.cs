using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Features.JourneyPlanning;

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
    public async Task<IActionResult> GetServices([FromQuery] DateTime? date, [FromQuery] Guid? routeId, CancellationToken cancellationToken)
    {
        return Ok(await journeyPlanningService.GetServicesAsync(date, routeId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetService(Guid id, CancellationToken cancellationToken)
    {
        var service = await journeyPlanningService.GetServiceAsync(id, cancellationToken);
        return service is null ? NotFound() : Ok(service);
    }
}