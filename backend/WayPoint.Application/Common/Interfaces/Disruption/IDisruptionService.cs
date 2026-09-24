using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Disruption;

public interface IDisruptionService
{
    /// <summary>
    /// Log a new disruption case (BR-DISRUPT-001). Auto-computes AffectedPassengerCount
    /// from confirmed bookings unless a manual override is provided.
    /// Transitions affected service status to Disrupted (BR-DISRUPT-002).
    /// </summary>
    Task<DisruptionCaseDto> LogDisruptionAsync(LogDisruptionDto dto);

    /// <summary>
    /// List active disruption cases with severity/status filtering and pagination.
    /// </summary>
    Task<PaginatedResponseDto<DisruptionCaseDto>> GetDisruptionsAsync(DisruptionFilterParams filter);

    /// <summary>
    /// Get a single disruption case by ID.
    /// </summary>
    Task<DisruptionCaseDto> GetDisruptionByIdAsync(Guid id);

    /// <summary>
    /// Calculate passenger impact metrics for a disruption case.
    /// Returns affected passenger count, revenue at risk, affected emails,
    /// and whether the disruption requires manager approval (BR-APPROVAL-001).
    /// </summary>
    Task<DisruptionImpactDto> GetDisruptionImpactAsync(Guid disruptionId);
}
