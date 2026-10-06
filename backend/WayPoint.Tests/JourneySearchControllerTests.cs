using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WayPoint.API.Controllers;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;
using Xunit;

namespace WayPoint.Tests;

public class JourneySearchControllerTests
{
    private static JourneySearchController CreateController(
        Mock<IJourneyPlanningService> mockJourneyService,
        Mock<IAiRecommendationClient> mockAiClient)
    {
        var controller = new JourneySearchController(mockJourneyService.Object, mockAiClient.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    }, "TestAuth"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAiRecommendations_ValidObjective_ReturnsAiRecommendations()
    {
        var mockAi = new Mock<IAiRecommendationClient>();
        mockAi.Setup(x => x.GetJourneyRecommendationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AiWorkflowResultDto
              {
                  Status = "Completed",
                  CandidateRoutes = new List<AiCandidateRouteDto>
                  {
                      new()
                      {
                          ServiceId = Guid.NewGuid(),
                          ServiceCode = "SRV-COL-ELLA",
                          RouteNumber = "EX-08",
                          Origin = "Colombo",
                          Destination = "Ella",
                          DepartureTime = DateTime.UtcNow.AddHours(2),
                          ArrivalTime = DateTime.UtcNow.AddHours(8),
                          TotalFare = 2500m,
                          DurationMinutes = 360,
                          IsConnecting = false,
                          MatchScore = 0.95m,
                          AvailableSeats = 15,
                          BusClass = "SemiLuxury"
                      }
                  },
                  AgentReasoning = "Scenic AC match"
              });

        var mockService = new Mock<IJourneyPlanningService>();
        var controller = CreateController(mockService, mockAi);

        var actionResult = await controller.GetAiRecommendations(
            new AiJourneyRecommendationRequestDto { Objective = "Bus to Ella with AC" },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<AiJourneyRecommendationResponseDto>(okResult.Value);
        Assert.False(response.IsAiFallback);
        Assert.Equal("Scenic AC match", response.AgentReasoning);
        Assert.Single(response.Candidates);
        Assert.Equal("EX-08", response.Candidates[0].RouteNumber);
        Assert.Equal(0.95m, response.Candidates[0].MatchScore);
    }

    [Fact]
    public async Task GetAiRecommendations_AiServiceThrowsException_TriggersSafeFailureFallback()
    {
        var mockAi = new Mock<IAiRecommendationClient>();
        mockAi.Setup(x => x.GetJourneyRecommendationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("AI service unavailable"));

        var mockService = new Mock<IJourneyPlanningService>();
        mockService.Setup(x => x.SearchAsync(It.IsAny<JourneySearchRequestDto>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new JourneySearchResponseDto
                   {
                       Candidates = new List<CandidateJourneyDto>
                       {
                           new()
                           {
                               ServiceId = Guid.NewGuid(),
                               RouteNumber = "EX-08",
                               OriginCity = "Colombo",
                               DestinationCity = "Ella",
                               MatchScore = 0.85m
                           }
                       }
                   });

        var controller = CreateController(mockService, mockAi);

        var actionResult = await controller.GetAiRecommendations(
            new AiJourneyRecommendationRequestDto { Objective = "Bus to Ella" },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<AiJourneyRecommendationResponseDto>(okResult.Value);
        Assert.True(response.IsAiFallback);
        Assert.Equal("SafeFailure", response.Status);
        Assert.Contains("AI offline", response.AgentReasoning);
        Assert.Single(response.Candidates);
    }

    [Fact]
    public async Task GetAiRecommendations_EmptyObjective_ReturnsBadRequest()
    {
        var mockAi = new Mock<IAiRecommendationClient>();
        var mockService = new Mock<IJourneyPlanningService>();
        var controller = CreateController(mockService, mockAi);

        var actionResult = await controller.GetAiRecommendations(
            new AiJourneyRecommendationRequestDto { Objective = "   " },
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }
}
