using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Fleet;

public interface ISeatAvailabilityService
{
    /// <summary>
    /// Computes the real-time seat availability matrix for a given service by cross-referencing
    /// the bus seat layout against active server-side holds and confirmed bookings (BR-SEAT-001).
    /// </summary>
    Task<ServiceSeatMatrixDto> GetSeatAvailabilityAsync(Guid serviceId);
}
