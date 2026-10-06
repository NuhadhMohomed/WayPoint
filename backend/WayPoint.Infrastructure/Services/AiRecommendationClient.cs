using System.Net.Http.Json;
using System.Text.Json;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;

namespace WayPoint.Infrastructure.Services;

public class AiRecommendationClient : IAiRecommendationClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiRecommendationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AiWorkflowResultDto> GetJourneyRecommendationAsync(string objective, CancellationToken cancellationToken = default)
    {
        var requestBody = new { objective };
        var response = await _httpClient.PostAsJsonAsync("/api/ai/journey-recommendation", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AiWorkflowResultDto>(JsonOptions, cancellationToken);
        return result ?? new AiWorkflowResultDto { Status = "Completed" };
    }
}
