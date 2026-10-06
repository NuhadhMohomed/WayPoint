using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Fleet;

public interface IDriverService
{
    Task<PaginatedResponseDto<DriverDto>> GetDriversAsync(DriverFilterParams filter);
    Task<DriverDto> GetDriverByIdAsync(Guid id);
    Task<DriverDto> CreateDriverAsync(CreateDriverDto dto);
    Task<DriverAssignmentDto> AssignDriverAsync(AssignDriverDto dto);
    Task<List<DriverAssignmentDto>> GetDriverAssignmentsAsync(Guid driverId);
}
