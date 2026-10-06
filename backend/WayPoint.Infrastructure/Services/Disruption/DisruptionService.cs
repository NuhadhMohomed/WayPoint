using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.DisruptionManagement.DTOs;
using WayPoint.Domain.Entities.Disruption;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Disruption;

/// <summary>
/// Manages disruption case lifecycle: logging, querying, and impact assessment.
/// Auto-computes AffectedPassengerCount from confirmed bookings (with manual override).
/// Transitions service status to Disrupted on logging (BR-DISRUPT-002).
/// </summary>
public class DisruptionService : IDisruptionService
{
    private readonly IWayPointDbContext _context;

    public DisruptionService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<DisruptionCaseDto> LogDisruptionAsync(LogDisruptionDto dto)
    {
        // Validate the disrupted service exists
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == dto.DisruptedServiceId);
        if (service == null)
            throw new KeyNotFoundException($"Service with ID '{dto.DisruptedServiceId}' was not found.");

        // Auto-compute affected passenger count from confirmed bookings (Q3: auto-compute + manual override)
        int affectedCount;
        if (dto.AffectedPassengerCountOverride.HasValue && dto.AffectedPassengerCountOverride.Value > 0)
        {
            affectedCount = dto.AffectedPassengerCountOverride.Value;
        }
        else
        {
            affectedCount = await _context.Bookings
                .CountAsync(b => b.ServiceId == dto.DisruptedServiceId
                              && b.Status == BookingStatus.Confirmed);
        }

        // Create the disruption case record (BR-DISRUPT-001)
        var disruptionCase = new DisruptionCase
        {
            DisruptedServiceId = dto.DisruptedServiceId,
            Reason = dto.Reason,
            Severity = dto.Severity,
            AffectedPassengerCount = affectedCount,
            Status = DisruptionStatus.Logged
        };

        await _context.DisruptionCases.AddAsync(disruptionCase);

        // Transition service status to Disrupted (BR-DISRUPT-002)
        service.Status = ServiceStatus.Disrupted;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DisruptionCaseDto
        {
            Id = disruptionCase.Id,
            DisruptedServiceId = disruptionCase.DisruptedServiceId,
            ServiceCode = service.ServiceCode,
            Reason = disruptionCase.Reason,
            Severity = disruptionCase.Severity,
            AffectedPassengerCount = disruptionCase.AffectedPassengerCount,
            Status = disruptionCase.Status,
            CreatedAt = disruptionCase.CreatedAt
        };
    }

    public async Task<PaginatedResponseDto<DisruptionCaseDto>> GetDisruptionsAsync(DisruptionFilterParams filter)
    {
        var query = _context.DisruptionCases
            .Include(dc => dc.DisruptedService)
            .AsQueryable();

        // Apply filters
        if (filter.Severity.HasValue)
            query = query.Where(dc => dc.Severity == filter.Severity.Value);

        if (filter.Status.HasValue)
            query = query.Where(dc => dc.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.Trim().ToLower();
            query = query.Where(dc =>
                dc.Reason.ToLower().Contains(searchLower) ||
                dc.DisruptedService.ServiceCode.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(dc => dc.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(dc => new DisruptionCaseDto
            {
                Id = dc.Id,
                DisruptedServiceId = dc.DisruptedServiceId,
                ServiceCode = dc.DisruptedService.ServiceCode,
                Reason = dc.Reason,
                Severity = dc.Severity,
                AffectedPassengerCount = dc.AffectedPassengerCount,
                Status = dc.Status,
                CreatedAt = dc.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<DisruptionCaseDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<DisruptionCaseDto> GetDisruptionByIdAsync(Guid id)
    {
        var dc = await _context.DisruptionCases
            .Include(d => d.DisruptedService)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dc == null)
            throw new KeyNotFoundException($"Disruption case with ID '{id}' was not found.");

        return new DisruptionCaseDto
        {
            Id = dc.Id,
            DisruptedServiceId = dc.DisruptedServiceId,
            ServiceCode = dc.DisruptedService.ServiceCode,
            Reason = dc.Reason,
            Severity = dc.Severity,
            AffectedPassengerCount = dc.AffectedPassengerCount,
            Status = dc.Status,
            CreatedAt = dc.CreatedAt
        };
    }

    public async Task<DisruptionImpactDto> GetDisruptionImpactAsync(Guid disruptionId)
    {
        var disruptionCase = await _context.DisruptionCases
            .Include(dc => dc.DisruptedService)
            .FirstOrDefaultAsync(dc => dc.Id == disruptionId);

        if (disruptionCase == null)
            throw new KeyNotFoundException($"Disruption case with ID '{disruptionId}' was not found.");

        // Query confirmed bookings on the disrupted service
        var affectedBookings = await _context.Bookings
            .Include(b => b.Passenger)
                .ThenInclude(p => p.User)
            .Where(b => b.ServiceId == disruptionCase.DisruptedServiceId
                     && b.Status == BookingStatus.Confirmed)
            .ToListAsync();

        var totalBookedPassengers = affectedBookings.Count;
        var revenueAtRisk = affectedBookings.Sum(b => b.TotalFareAmount);
        var affectedEmails = affectedBookings
            .Select(b => b.Passenger?.User?.Email)
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .ToList()!;
        var affectedBookingIds = affectedBookings.Select(b => b.Id).ToList();

        // Classify impact severity (BR-APPROVAL-001)
        // High-impact if: service is cancelled, severity is Critical, or >10 passengers affected
        var requiresManagerApproval = disruptionCase.Severity == DisruptionSeverity.Critical
            || disruptionCase.DisruptedService.Status == ServiceStatus.Cancelled
            || totalBookedPassengers > 10;

        var classification = requiresManagerApproval ? "High" : "Low";

        return new DisruptionImpactDto
        {
            DisruptionCaseId = disruptionCase.Id,
            ServiceCode = disruptionCase.DisruptedService.ServiceCode,
            Severity = disruptionCase.Severity,
            TotalBookedPassengers = totalBookedPassengers,
            RevenueAtRisk = revenueAtRisk,
            RequiresManagerApproval = requiresManagerApproval,
            ImpactClassification = classification,
            AffectedPassengerEmails = affectedEmails!,
            AffectedBookingIds = affectedBookingIds
        };
    }
}
