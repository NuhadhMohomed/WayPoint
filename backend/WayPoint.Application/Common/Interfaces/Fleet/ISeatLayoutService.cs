using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Fleet;

public interface ISeatLayoutService
{
    Task<List<SeatLayoutSummaryDto>> GetSeatLayoutsAsync();
    Task<SeatLayoutDto> GetSeatLayoutByIdAsync(Guid id);
    Task<SeatLayoutDto> CreateSeatLayoutAsync(CreateSeatLayoutDto dto);
}
