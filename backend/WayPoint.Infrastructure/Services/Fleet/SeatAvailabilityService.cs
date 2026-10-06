using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Fleet;

/// <summary>
/// Core complex business operation: Real-Time Seat Availability Matrix Aggregator.
/// Computes live seat status by cross-referencing the bus seat layout against
/// active server-side SeatHolds and confirmed Bookings (BR-SEAT-001).
/// </summary>
public class SeatAvailabilityService : ISeatAvailabilityService
{
    private readonly IWayPointDbContext _context;

    public SeatAvailabilityService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceSeatMatrixDto> GetSeatAvailabilityAsync(Guid serviceId)
    {
        // 1. Load the service with its bus and seat layout
        var service = await _context.Services
            .Include(s => s.Bus)
                .ThenInclude(b => b.SeatLayout)
                    .ThenInclude(sl => sl.Seats)
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (service == null)
            throw new KeyNotFoundException($"Service with ID '{serviceId}' was not found.");

        if (service.Bus?.SeatLayout == null)
            throw new InvalidOperationException($"Service '{service.ServiceCode}' has no bus or seat layout assigned.");

        var seats = service.Bus.SeatLayout.Seats.ToList();
        var now = DateTime.UtcNow;

        // 2. Query active SeatHolds: ServiceId matches AND HeldUntil > now AND Status == Held
        var activeSeatHolds = await _context.SeatHolds
            .Where(sh => sh.ServiceId == serviceId
                         && sh.HeldUntil > now
                         && sh.Status == SeatHoldStatus.Held)
            .Select(sh => sh.SeatId)
            .ToHashSetAsync();

        // 3. Query confirmed Bookings: ServiceId matches AND Status != Cancelled
        //    Bookings store seat numbers as comma-separated string (e.g., "14A,14B")
        var confirmedBookings = await _context.Bookings
            .Where(b => b.ServiceId == serviceId
                        && b.Status != BookingStatus.Cancelled)
            .Select(b => b.SeatNumbers)
            .ToListAsync();

        // Parse booked seat numbers into a HashSet for O(1) lookup
        var bookedSeatNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seatNumbersCsv in confirmedBookings)
        {
            if (!string.IsNullOrWhiteSpace(seatNumbersCsv))
            {
                foreach (var seatNumber in seatNumbersCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    bookedSeatNumbers.Add(seatNumber);
                }
            }
        }

        // 4. Merge into real-time matrix
        var seatDtos = new List<SeatDto>();
        int availableCount = 0, heldCount = 0, bookedCount = 0;

        foreach (var seat in seats.OrderBy(s => s.RowIndex).ThenBy(s => s.ColumnIndex))
        {
            SeatStatus status;

            if (activeSeatHolds.Contains(seat.Id))
            {
                status = SeatStatus.Held;
                heldCount++;
            }
            else if (bookedSeatNumbers.Contains(seat.SeatNumber))
            {
                status = SeatStatus.Booked;
                bookedCount++;
            }
            else
            {
                status = SeatStatus.Available;
                availableCount++;
            }

            seatDtos.Add(new SeatDto
            {
                Id = seat.Id,
                SeatNumber = seat.SeatNumber,
                RowIndex = seat.RowIndex,
                ColumnIndex = seat.ColumnIndex,
                SeatClass = seat.SeatClass,
                Status = status
            });
        }

        return new ServiceSeatMatrixDto
        {
            ServiceId = serviceId,
            ServiceCode = service.ServiceCode,
            TotalSeats = seats.Count,
            AvailableSeats = availableCount,
            HeldSeats = heldCount,
            BookedSeats = bookedCount,
            Seats = seatDtos
        };
    }
}

/// <summary>
/// Extension method to convert IQueryable Select to HashSet.
/// </summary>
internal static class QueryableExtensions
{
    public static async Task<HashSet<T>> ToHashSetAsync<T>(this IQueryable<T> source)
    {
        var list = await source.ToListAsync();
        return new HashSet<T>(list);
    }
}
