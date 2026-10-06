using WayPoint.Application.Features.JourneyPlanning.DTOs;

namespace WayPoint.Application.Features.JourneyPlanning;

public interface IAiRecommendationClient
{
    Task<AiWorkflowResultDto> GetJourneyRecommendationAsync(string objective, CancellationToken cancellationToken = default);
}
