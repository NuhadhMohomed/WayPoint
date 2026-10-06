using System.Net;
using System.Text.Json;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;
    private readonly Exception? _exceptionToThrow;

    public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    public MockHttpMessageHandler(Exception exceptionToThrow)
    {
        _exceptionToThrow = exceptionToThrow;
        _responseContent = string.Empty;
        _statusCode = HttpStatusCode.InternalServerError;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_exceptionToThrow != null)
        {
            throw _exceptionToThrow;
        }

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

public class AiRecommendationClientTests
{
    [Fact]
    public async Task GetJourneyRecommendationAsync_Success_ReturnsMappedWorkflowResult()
    {
        var jsonResponse = @"{
            ""workflow_id"": ""e0f7f3a2-71c1-4b1f-9b2f-2d7c588e1a12"",
            ""workflow_type"": ""journey_recommendation"",
            ""status"": ""Completed"",
            ""steps_completed"": 3,
            ""candidate_routes"": [
                {
                    ""service_id"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
                    ""service_code"": ""SRV-COL-ELLA-0800"",
                    ""route_number"": ""EX-08"",
                    ""origin"": ""Colombo"",
                    ""destination"": ""Ella"",
                    ""departure_time"": ""2026-10-01T08:00:00"",
                    ""arrival_time"": ""2026-10-01T14:30:00"",
                    ""total_fare"": 2500.0,
                    ""duration_minutes"": 390,
                    ""is_connecting"": false,
                    ""match_score"": 0.95,
                    ""available_seats"": 12,
                    ""bus_class"": ""SemiLuxury""
                }
            ],
            ""agent_reasoning"": ""Selected EX-08 for shortest travel time and guaranteed AC seats.""
        }";

        var handler = new MockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new AiRecommendationClient(httpClient);

        var result = await client.GetJourneyRecommendationAsync("Fastest bus to Ella with AC", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.Single(result.CandidateRoutes);
        Assert.Equal("EX-08", result.CandidateRoutes[0].RouteNumber);
        Assert.Equal(0.95m, result.CandidateRoutes[0].MatchScore);
        Assert.Equal("Selected EX-08 for shortest travel time and guaranteed AC seats.", result.AgentReasoning);
    }

    [Fact]
    public async Task GetJourneyRecommendationAsync_HttpError_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new AiRecommendationClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetJourneyRecommendationAsync("Trip to Ella", CancellationToken.None));
    }
}
