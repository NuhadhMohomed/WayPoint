using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Fleet;

public class SeatLayoutService : ISeatLayoutService
{
    private readonly IWayPointDbContext _context;

    public SeatLayoutService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<List<SeatLayoutSummaryDto>> GetSeatLayoutsAsync()
    {
        return await _context.SeatLayouts
            .Include(sl => sl.Seats)
            .OrderByDescending(sl => sl.CreatedAt)
            .Select(sl => new SeatLayoutSummaryDto
            {
                Id = sl.Id,
                Name = sl.Name,
                TotalRows = sl.TotalRows,
                TotalColumns = sl.TotalColumns,
                TotalSeats = sl.Seats.Count
            })
            .ToListAsync();
    }

    public async Task<SeatLayoutDto> GetSeatLayoutByIdAsync(Guid id)
    {
        var layout = await _context.SeatLayouts
            .Include(sl => sl.Seats)
            .FirstOrDefaultAsync(sl => sl.Id == id);

        if (layout == null)
            throw new KeyNotFoundException($"Seat layout with ID '{id}' was not found.");

        return MapToDto(layout);
    }

    public async Task<SeatLayoutDto> CreateSeatLayoutAsync(CreateSeatLayoutDto dto)
    {
        // Validate grid bounds
        if (dto.TotalRows <= 0 || dto.TotalColumns <= 0)
            throw new ArgumentException("TotalRows and TotalColumns must be greater than zero.");

        // Validate all seat coordinates are within grid bounds
        foreach (var seat in dto.Seats)
        {
            if (seat.RowIndex < 0 || seat.RowIndex >= dto.TotalRows)
                throw new ArgumentException($"Seat '{seat.SeatNumber}' has RowIndex {seat.RowIndex} which is outside grid bounds (0 to {dto.TotalRows - 1}).");

            if (seat.ColumnIndex < 0 || seat.ColumnIndex >= dto.TotalColumns)
                throw new ArgumentException($"Seat '{seat.SeatNumber}' has ColumnIndex {seat.ColumnIndex} which is outside grid bounds (0 to {dto.TotalColumns - 1}).");
        }

        // Validate unique seat numbers within layout
        var seatNumbers = dto.Seats.Select(s => s.SeatNumber.Trim().ToUpperInvariant()).ToList();
        if (seatNumbers.Count != seatNumbers.Distinct().Count())
            throw new ArgumentException("Duplicate seat numbers found in the layout definition.");

        // Validate unique coordinates
        var coordinates = dto.Seats.Select(s => (s.RowIndex, s.ColumnIndex)).ToList();
        if (coordinates.Count != coordinates.Distinct().Count())
            throw new ArgumentException("Duplicate seat coordinates found in the layout definition.");

        var layout = new SeatLayout
        {
            Name = dto.Name.Trim(),
            TotalRows = dto.TotalRows,
            TotalColumns = dto.TotalColumns,
            Seats = dto.Seats.Select(s => new Seat
            {
                SeatNumber = s.SeatNumber.Trim().ToUpperInvariant(),
                RowIndex = s.RowIndex,
                ColumnIndex = s.ColumnIndex,
                SeatClass = s.SeatClass
            }).ToList()
        };

        await _context.SeatLayouts.AddAsync(layout);
        await _context.SaveChangesAsync();

        return MapToDto(layout);
    }

    private static SeatLayoutDto MapToDto(SeatLayout layout)
    {
        return new SeatLayoutDto
        {
            Id = layout.Id,
            Name = layout.Name,
            TotalRows = layout.TotalRows,
            TotalColumns = layout.TotalColumns,
            Seats = layout.Seats
                .OrderBy(s => s.RowIndex)
                .ThenBy(s => s.ColumnIndex)
                .Select(s => new SeatDto
                {
                    Id = s.Id,
                    SeatNumber = s.SeatNumber,
                    RowIndex = s.RowIndex,
                    ColumnIndex = s.ColumnIndex,
                    SeatClass = s.SeatClass,
                    Status = SeatStatus.Available
                })
                .ToList()
        };
    }
}
