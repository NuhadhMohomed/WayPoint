using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WayPoint.API.Controllers;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class RebookingAiProposalTests
{
    [Fact]
    public async Task GenerateAiProposal_ValidRequest_ReturnsAiResultAndCreatesProposal()
    {
        var disruptionCaseId = Guid.NewGuid();
        var replacementServiceId = Guid.NewGuid();

        var mockAi = new Mock<IAiRecommendationClient>();
        mockAi.Setup(x => x.GetDisruptionRebookingAsync(
                disruptionCaseId.ToString(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AiWorkflowResultDto
              {
                  WorkflowId = Guid.NewGuid(),
                  Status = "PendingManagerApproval",
                  AgentReasoning = "Found alternate express AC coach",
                  CandidateRoutes = new List<AiCandidateRouteDto>
                  {
                      new()
                      {
                          ServiceId = replacementServiceId.ToString(),
                          ServiceCode = "SRV-REP-01",
                          RouteNumber = "EX-08",
                          Origin = "Colombo",
                          Destination = "Ella",
                          DepartureTime = DateTime.UtcNow.AddHours(1),
                          ArrivalTime = DateTime.UtcNow.AddHours(7),
                          TotalFare = 2600m,
                          DurationMinutes = 360,
                          AvailableSeats = 18,
                          BusClass = "Luxury AC"
                      }
                  }
              });

        var mockRebookingService = new Mock<IRebookingService>();
        var proposalId = Guid.NewGuid();
        mockRebookingService.Setup(x => x.CreateProposalAsync(It.IsAny<CreateRebookingProposalDto>()))
                            .ReturnsAsync(new RebookingProposalDto
                            {
                                Id = proposalId,
                                DisruptionCaseId = disruptionCaseId,
                                ReplacementServiceId = replacementServiceId,
                                Status = RebookingStatus.PendingManagerApproval
                            });

        var controller = new RebookingController(mockRebookingService.Object, mockAi.Object);

        var actionResult = await controller.GenerateAiProposal(
            new GenerateAiRebookingProposalDto { DisruptionCaseId = disruptionCaseId },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<GenerateAiRebookingProposalResultDto>(okResult.Value);
        Assert.False(response.IsAiFallback);
        Assert.Equal("PendingManagerApproval", response.Status);
        Assert.Equal(proposalId, response.ProposalId);
        Assert.Single(response.Candidates);
        Assert.Equal(replacementServiceId, response.Candidates[0].ServiceId);
    }

    [Fact]
    public async Task GenerateAiProposal_AiException_ReturnsSafeFailureFallback()
    {
        var disruptionCaseId = Guid.NewGuid();

        var mockAi = new Mock<IAiRecommendationClient>();
        mockAi.Setup(x => x.GetDisruptionRebookingAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("AI service unavailable"));

        var mockRebookingService = new Mock<IRebookingService>();
        var controller = new RebookingController(mockRebookingService.Object, mockAi.Object);

        var actionResult = await controller.GenerateAiProposal(
            new GenerateAiRebookingProposalDto { DisruptionCaseId = disruptionCaseId },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<GenerateAiRebookingProposalResultDto>(okResult.Value);
        Assert.True(response.IsAiFallback);
        Assert.Equal("SafeFailure", response.Status);
        Assert.Null(response.ProposalId);
    }

    [Fact]
    public async Task GenerateAiProposal_EmptyCaseId_ReturnsBadRequest()
    {
        var mockAi = new Mock<IAiRecommendationClient>();
        var mockRebookingService = new Mock<IRebookingService>();
        var controller = new RebookingController(mockRebookingService.Object, mockAi.Object);

        var actionResult = await controller.GenerateAiProposal(
            new GenerateAiRebookingProposalDto { DisruptionCaseId = Guid.Empty },
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task AiRecommendationClient_GetDisruptionRebookingAsync_DeserializesCorrectly()
    {
        var jsonResponse = @"{
            ""workflow_id"": ""550e8400-e29b-41d4-a716-446655440000"",
            ""workflow_type"": ""disruption_rebooking"",
            ""status"": ""PendingManagerApproval"",
            ""steps_completed"": 5,
            ""candidate_routes"": [
                {
                    ""service_id"": ""SRV-REP-99"",
                    ""service_code"": ""SRV-99"",
                    ""route_number"": ""EX-99"",
                    ""origin"": ""Colombo"",
                    ""destination"": ""Ella"",
                    ""departure_time"": ""2026-10-10T08:00:00"",
                    ""arrival_time"": ""2026-10-10T14:00:00"",
                    ""total_fare"": 2500.0,
                    ""duration_minutes"": 360,
                    ""is_connecting"": false,
                    ""match_score"": 0.98,
                    ""available_seats"": 10,
                    ""bus_class"": ""Luxury AC""
                }
            ],
            ""agent_reasoning"": ""Optimal replacement selected""
        }";

        var handler = new MockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new AiRecommendationClient(httpClient);

        var result = await client.GetDisruptionRebookingAsync("CASE-123", "Recovery", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("PendingManagerApproval", result.Status);
        Assert.Single(result.CandidateRoutes);
        Assert.Equal("SRV-REP-99", result.CandidateRoutes[0].ServiceId);
        Assert.Equal("Optimal replacement selected", result.AgentReasoning);
    }
}
