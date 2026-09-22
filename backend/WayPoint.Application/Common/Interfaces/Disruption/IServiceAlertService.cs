using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Disruption;

public interface IServiceAlertService
{
    /// <summary>
    /// List active public service alerts (BR-NOTIF-001).
    /// </summary>
    Task<List<ServiceAlertDto>> GetActiveAlertsAsync();

    /// <summary>
    /// Broadcast a new public service alert linked to a specific service.
    /// </summary>
    Task<ServiceAlertDto> BroadcastAlertAsync(CreateServiceAlertDto dto);
}
