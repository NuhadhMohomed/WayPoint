using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Fleet;

public interface IBusService
{
    Task<PaginatedResponseDto<BusDto>> GetBusesAsync(BusFilterParams filter);
    Task<BusDto> GetBusByIdAsync(Guid id);
    Task<BusDto> CreateBusAsync(CreateBusDto dto);
    Task<BusDto> ToggleMaintenanceAsync(Guid busId, MaintenanceToggleDto dto);
}
