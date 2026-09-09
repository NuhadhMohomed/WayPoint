using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Fleet;

public interface IResourceFeasibilityService
{
    /// <summary>
    /// Evaluates replacement resource feasibility for a disrupted service by checking unassigned
    /// fleet inventory (BR-RESOURCE-001: capacity match) and driver availability
    /// (BR-RESOURCE-002: 8-hour rest rule).
    /// </summary>
    Task<ResourceFeasibilityResponseDto> EvaluateFeasibilityAsync(ResourceFeasibilityRequestDto dto);
}
