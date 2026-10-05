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
    private readonly IAiRecommendationClient aiRecommendationClient;

    public JourneySearchController(
        IJourneyPlanningService journeyPlanningService,
        IAiRecommendationClient aiRecommendationClient)
    {
        this.journeyPlanningService = journeyPlanningService;
        this.aiRecommendationClient = aiRecommendationClient;
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

    [Authorize]
    [HttpPost("ai-recommendation")]
    [ProducesResponseType(typeof(AiJourneyRecommendationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiJourneyRecommendationResponseDto>> GetAiRecommendations(
        [FromBody] AiJourneyRecommendationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Objective))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid journey recommendation request",
                Detail = "Objective is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        Guid? passengerId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

        try
        {
            var aiResult = await aiRecommendationClient.GetJourneyRecommendationAsync(request.Objective.Trim(), cancellationToken);

            var candidates = aiResult.CandidateRoutes.Select(c => new CandidateJourneyDto
            {
                ServiceId = c.ServiceId,
                ServiceCode = c.ServiceCode,
                RouteNumber = c.RouteNumber,
                OriginCity = c.Origin,
                DestinationCity = c.Destination,
                DepartureTime = c.DepartureTime,
                ArrivalTime = c.ArrivalTime,
                TotalFare = c.TotalFare,
                TotalDurationMinutes = c.DurationMinutes,
                BusClass = c.BusClass,
                IsConnecting = c.IsConnecting,
                AvailableSeatsCount = c.AvailableSeats,
                MatchScore = c.MatchScore
            }).ToList();

            return Ok(new AiJourneyRecommendationResponseDto
            {
                WorkflowId = aiResult.WorkflowId != Guid.Empty ? aiResult.WorkflowId : Guid.NewGuid(),
                Status = !string.IsNullOrWhiteSpace(aiResult.Status) ? aiResult.Status : "Completed",
                AgentReasoning = aiResult.AgentReasoning,
                IsAiFallback = false,
                Candidates = candidates
            });
        }
        catch (Exception)
        {
            // Safe Failure Fallback (BR-AIVAL-002)
            var text = request.Objective.ToLowerInvariant();
            var origin = "Colombo";
            var destination = "Ella";
            if (text.Contains("kandy")) destination = "Kandy";
            else if (text.Contains("galle")) destination = "Galle";
            else if (text.Contains("jaffna")) destination = "Jaffna";

            var fallbackRequest = new JourneySearchRequestDto
            {
                OriginCity = origin,
                DestinationCity = destination,
                TravelDate = request.TravelDate ?? DateTime.UtcNow.Date.AddDays(1),
                PassengerCount = request.PassengerCount > 0 ? request.PassengerCount : 1
            };

            var fallbackSearch = await journeyPlanningService.SearchAsync(fallbackRequest, passengerId, cancellationToken);

            return Ok(new AiJourneyRecommendationResponseDto
            {
                WorkflowId = Guid.NewGuid(),
                Status = "SafeFailure",
                AgentReasoning = "Showing verified direct and connecting routes (AI offline).",
                IsAiFallback = true,
                Candidates = fallbackSearch.Candidates
            });
        }
    }
}