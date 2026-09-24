using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;
using WayPoint.Domain.Entities.Disruption;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Disruption;

/// <summary>
/// Manages service alert broadcasting for public transit notices (BR-NOTIF-001).
/// </summary>
public class ServiceAlertService : IServiceAlertService
{
    private readonly IWayPointDbContext _context;

    public ServiceAlertService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceAlertDto>> GetActiveAlertsAsync()
    {
        var alerts = await _context.ServiceAlerts
            .Include(sa => sa.Service)
            .OrderByDescending(sa => sa.PostedAt)
            .Take(50)
            .Select(sa => new ServiceAlertDto
            {
                Id = sa.Id,
                ServiceId = sa.ServiceId,
                ServiceCode = sa.Service.ServiceCode,
                Title = sa.Title,
                Message = sa.Message,
                PostedAt = sa.PostedAt
            })
            .ToListAsync();

        return alerts;
    }

    public async Task<ServiceAlertDto> BroadcastAlertAsync(CreateServiceAlertDto dto)
    {
        // Validate the service exists
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == dto.ServiceId);
        if (service == null)
            throw new KeyNotFoundException($"Service with ID '{dto.ServiceId}' was not found.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Alert title is required.");

        var alert = new ServiceAlert
        {
            ServiceId = dto.ServiceId,
            Title = dto.Title.Trim(),
            Message = dto.Message.Trim(),
            PostedAt = DateTime.UtcNow
        };

        await _context.ServiceAlerts.AddAsync(alert);
        await _context.SaveChangesAsync();

        return new ServiceAlertDto
        {
            Id = alert.Id,
            ServiceId = alert.ServiceId,
            ServiceCode = service.ServiceCode,
            Title = alert.Title,
            Message = alert.Message,
            PostedAt = alert.PostedAt
        };
    }
}
