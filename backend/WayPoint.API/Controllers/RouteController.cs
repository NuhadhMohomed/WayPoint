using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/routes")]
public sealed class RouteController : ControllerBase
{
    private readonly IJourneyPlanningService journeyPlanningService;

    public RouteController(IJourneyPlanningService journeyPlanningService)
    {
        this.journeyPlanningService = journeyPlanningService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RouteDto>>> GetRoutes([FromQuery] string? origin, [FromQuery] string? destination, [FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        return Ok(await journeyPlanningService.GetRoutesAsync(origin, destination, activeOnly, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RouteDetailsDto>> GetRoute(Guid id, CancellationToken cancellationToken)
    {
        var route = await journeyPlanningService.GetRouteAsync(id, cancellationToken);
        return route is null ? NotFound() : Ok(route);
    }

    [Authorize(Policy = "RequireOperator")]
    [HttpPost]
    public async Task<ActionResult<RouteDto>> CreateRoute([FromBody] CreateRouteDto request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new ProblemDetails { Title = "Invalid route", Detail = validationError, Status = StatusCodes.Status400BadRequest });

        var route = await journeyPlanningService.CreateRouteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRoute), new { id = route.Id }, route);
    }

    private static string? Validate(CreateRouteDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RouteNumber) || string.IsNullOrWhiteSpace(request.OriginCity) || string.IsNullOrWhiteSpace(request.DestinationCity)) return "Route number, origin, and destination are required.";
        if (request.EstimatedDurationMinutes < 0) return "Estimated duration cannot be negative.";
        if (request.Stops.Count < 2) return "A route must contain at least two stops.";
        if (request.Stops.Any(stop => string.IsNullOrWhiteSpace(stop.StopName) || stop.SequenceOrder < 1 || stop.ArrivalOffsetMinutes < 0 || stop.DistanceFromOriginKm < 0)) return "Stops must have names and non-negative sequence, time, and distance values.";
        if (request.Stops.Select(stop => stop.SequenceOrder).Distinct().Count() != request.Stops.Count) return "Stop sequence numbers must be unique.";
        return null;
    }
}