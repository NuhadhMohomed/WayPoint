using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/journeys")]
public sealed class JourneySearchController : ControllerBase
{
    private readonly IJourneyPlanningService journeyPlanningService;

    public JourneySearchController(IJourneyPlanningService journeyPlanningService)
    {
        this.journeyPlanningService = journeyPlanningService;
    }

    [Authorize]
    [HttpPost("search")]
    public async Task<ActionResult<JourneySearchResponseDto>> Search([FromBody] JourneySearchRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OriginCity) || string.IsNullOrWhiteSpace(request.DestinationCity)) return BadRequest(new ProblemDetails { Title = "Invalid journey search", Detail = "Origin and destination are required.", Status = StatusCodes.Status400BadRequest });
        if (request.PassengerCount is < 1 or > 20) return BadRequest(new ProblemDetails { Title = "Invalid journey search", Detail = "Passenger count must be between 1 and 20.", Status = StatusCodes.Status400BadRequest });
        if (request.TravelDate == default) return BadRequest(new ProblemDetails { Title = "Invalid journey search", Detail = "Travel date is required.", Status = StatusCodes.Status400BadRequest });

        Guid? passengerId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
        return Ok(await journeyPlanningService.SearchAsync(request, passengerId, cancellationToken));
    }
}