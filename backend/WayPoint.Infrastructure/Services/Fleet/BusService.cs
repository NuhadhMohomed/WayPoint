using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Entities.Fleet;

namespace WayPoint.Infrastructure.Services.Fleet;

public class BusService : IBusService
{
    private readonly IWayPointDbContext _context;

    public BusService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponseDto<BusDto>> GetBusesAsync(BusFilterParams filter)
    {
        var query = _context.Buses
            .Include(b => b.SeatLayout)
            .AsQueryable();

        // Apply filters
        if (filter.BusClass.HasValue)
            query = query.Where(b => b.BusClass == filter.BusClass.Value);

        if (filter.IsUnderMaintenance.HasValue)
            query = query.Where(b => b.IsUnderMaintenance == filter.IsUnderMaintenance.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.Trim().ToLower();
            query = query.Where(b =>
                b.RegistrationNumber.ToLower().Contains(searchLower) ||
                b.SeatLayout.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        var buses = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new BusDto
            {
                Id = b.Id,
                RegistrationNumber = b.RegistrationNumber,
                BusClass = b.BusClass,
                TotalSeatCapacity = b.TotalSeatCapacity,
                SeatLayoutId = b.SeatLayoutId,
                SeatLayoutName = b.SeatLayout.Name,
                IsUnderMaintenance = b.IsUnderMaintenance,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<BusDto>(buses, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<BusDto> GetBusByIdAsync(Guid id)
    {
        var bus = await _context.Buses
            .Include(b => b.SeatLayout)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bus == null)
            throw new KeyNotFoundException($"Bus with ID '{id}' was not found.");

        return new BusDto
        {
            Id = bus.Id,
            RegistrationNumber = bus.RegistrationNumber,
            BusClass = bus.BusClass,
            TotalSeatCapacity = bus.TotalSeatCapacity,
            SeatLayoutId = bus.SeatLayoutId,
            SeatLayoutName = bus.SeatLayout?.Name,
            IsUnderMaintenance = bus.IsUnderMaintenance,
            CreatedAt = bus.CreatedAt
        };
    }

    public async Task<BusDto> CreateBusAsync(CreateBusDto dto)
    {
        // Validate unique registration number
        var normalizedRegNo = dto.RegistrationNumber.Trim().ToUpperInvariant();
        if (await _context.Buses.AnyAsync(b => b.RegistrationNumber.ToUpper() == normalizedRegNo))
        {
            throw new InvalidOperationException($"A bus with registration number '{normalizedRegNo}' already exists.");
        }

        // Validate seat layout exists if provided
        SeatLayout? seatLayout = null;
        if (dto.SeatLayoutId.HasValue)
        {
            seatLayout = await _context.SeatLayouts.FindAsync(dto.SeatLayoutId.Value);
            if (seatLayout == null)
            {
                throw new KeyNotFoundException($"Seat layout with ID '{dto.SeatLayoutId.Value}' was not found.");
            }
        }

        var bus = new Bus
        {
            RegistrationNumber = normalizedRegNo,
            BusClass = dto.BusClass,
            TotalSeatCapacity = dto.TotalSeatCapacity,
            SeatLayoutId = dto.SeatLayoutId ?? Guid.Empty,
            IsUnderMaintenance = false
        };

        await _context.Buses.AddAsync(bus);
        await _context.SaveChangesAsync();

        return new BusDto
        {
            Id = bus.Id,
            RegistrationNumber = bus.RegistrationNumber,
            BusClass = bus.BusClass,
            TotalSeatCapacity = bus.TotalSeatCapacity,
            SeatLayoutId = bus.SeatLayoutId,
            SeatLayoutName = seatLayout?.Name,
            IsUnderMaintenance = bus.IsUnderMaintenance,
            CreatedAt = bus.CreatedAt
        };
    }

    public async Task<BusDto> ToggleMaintenanceAsync(Guid busId, MaintenanceToggleDto dto)
    {
        var bus = await _context.Buses
            .Include(b => b.SeatLayout)
            .FirstOrDefaultAsync(b => b.Id == busId);

        if (bus == null)
            throw new KeyNotFoundException($"Bus with ID '{busId}' was not found.");

        bus.IsUnderMaintenance = dto.IsUnderMaintenance;
        bus.UpdatedAt = DateTime.UtcNow;

        // Log maintenance record when entering maintenance
        if (dto.IsUnderMaintenance)
        {
            var record = new MaintenanceRecord
            {
                BusId = busId,
                Description = dto.Description ?? "Maintenance status toggled",
                StartedAt = DateTime.UtcNow,
                Cost = dto.Cost ?? 0
            };
            await _context.MaintenanceRecords.AddAsync(record);
        }
        else
        {
            // Mark the latest active maintenance record as completed
            var activeRecord = await _context.MaintenanceRecords
                .Where(m => m.BusId == busId && m.CompletedAt == null)
                .OrderByDescending(m => m.StartedAt)
                .FirstOrDefaultAsync();

            if (activeRecord != null)
            {
                activeRecord.CompletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return new BusDto
        {
            Id = bus.Id,
            RegistrationNumber = bus.RegistrationNumber,
            BusClass = bus.BusClass,
            TotalSeatCapacity = bus.TotalSeatCapacity,
            SeatLayoutId = bus.SeatLayoutId,
            SeatLayoutName = bus.SeatLayout?.Name,
            IsUnderMaintenance = bus.IsUnderMaintenance,
            CreatedAt = bus.CreatedAt
        };
    }
}
