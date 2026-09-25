using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Fleet;

/// <summary>
/// Complex business operation: Replacement Resource Feasibility Solver.
/// Evaluates whether alternative buses (BR-RESOURCE-001: capacity match) and
/// drivers (BR-RESOURCE-002: 8h rest rule) can support a disrupted service.
/// </summary>
public class ResourceFeasibilityService : IResourceFeasibilityService
{
    private readonly IWayPointDbContext _context;

    public ResourceFeasibilityService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<ResourceFeasibilityResponseDto> EvaluateFeasibilityAsync(ResourceFeasibilityRequestDto dto)
    {
        var result = new ResourceFeasibilityResponseDto();

        // 1. Find candidate replacement buses
        //    - Not under maintenance
        //    - Seat capacity >= required capacity (BR-RESOURCE-001)
        //    - Not assigned to conflicting service schedules at the required departure time
        var candidateBuses = await FindFeasibleBusesAsync(dto);
        result.FeasibleBuses = candidateBuses;

        // 2. Find candidate replacement drivers
        //    - Status == "Active"
        //    - Has >= 8 hours off-duty rest before required departure (BR-RESOURCE-002)
        //    - Not assigned to overlapping service schedules
        var candidateDrivers = await FindFeasibleDriversAsync(dto);
        result.FeasibleDrivers = candidateDrivers;

        // 3. Determine overall feasibility
        result.IsFeasible = candidateBuses.Count > 0 && candidateDrivers.Count > 0;

        if (result.IsFeasible)
        {
            result.Summary = $"Feasible: {candidateBuses.Count} replacement bus(es) and " +
                             $"{candidateDrivers.Count} available driver(s) found for " +
                             $"departure at {dto.RequiredDepartureTime:g} requiring " +
                             $"{dto.RequiredSeatCapacity} seats.";
        }
        else
        {
            var issues = new List<string>();
            if (candidateBuses.Count == 0)
                issues.Add($"No available buses with capacity >= {dto.RequiredSeatCapacity} seats");
            if (candidateDrivers.Count == 0)
                issues.Add("No available drivers with sufficient rest hours");

            result.Summary = $"Not feasible: {string.Join("; ", issues)}.";
        }

        return result;
    }

    private async Task<List<FeasibleBusDto>> FindFeasibleBusesAsync(ResourceFeasibilityRequestDto dto)
    {
        // Get all buses that are not under maintenance and have sufficient capacity
        var candidateBuses = await _context.Buses
            .Where(b => !b.IsUnderMaintenance
                        && b.TotalSeatCapacity >= dto.RequiredSeatCapacity)
            .ToListAsync();

        var feasibleBuses = new List<FeasibleBusDto>();

        foreach (var bus in candidateBuses)
        {
            // Check if this bus has a conflicting service assignment at the required time
            // A bus is busy if it has a service where:
            //   DepartureTime < RequiredDepartureTime + EstimatedDuration AND
            //   ArrivalTime > RequiredDepartureTime
            // Simplified: check if bus is assigned to any service overlapping with required time window
            var hasConflict = await _context.Services
                .AnyAsync(s => s.BusId == bus.Id
                               && s.Status != ServiceStatus.Cancelled
                               && s.DepartureTime <= dto.RequiredDepartureTime.AddHours(6) // Assume max 6h service
                               && s.ArrivalTime >= dto.RequiredDepartureTime);

            if (!hasConflict)
            {
                feasibleBuses.Add(new FeasibleBusDto
                {
                    BusId = bus.Id,
                    RegistrationNumber = bus.RegistrationNumber,
                    BusClass = bus.BusClass,
                    SeatCapacity = bus.TotalSeatCapacity
                });
            }
        }

        return feasibleBuses.OrderByDescending(b => b.SeatCapacity).ToList();
    }

    private async Task<List<FeasibleDriverDto>> FindFeasibleDriversAsync(ResourceFeasibilityRequestDto dto)
    {
        // Get all active drivers
        var activeDrivers = await _context.Drivers
            .Where(d => d.Status == "Active")
            .ToListAsync();

        var feasibleDrivers = new List<FeasibleDriverDto>();

        foreach (var driver in activeDrivers)
        {
            // Get all assignments for this driver
            var assignments = await _context.DriverAssignments
                .Where(da => da.DriverId == driver.Id)
                .Include(da => da.Service)
                .ToListAsync();

            // Check for schedule overlap
            var hasOverlap = assignments.Any(da =>
                da.Service.Status != ServiceStatus.Cancelled
                && da.Service.DepartureTime < dto.RequiredDepartureTime.AddHours(6)
                && da.Service.ArrivalTime > dto.RequiredDepartureTime);

            if (hasOverlap) continue;

            // BR-RESOURCE-002: Check 8-hour rest rule
            // Find the closest preceding service arrival
            var closestPrecedingArrival = assignments
                .Where(da => da.Service.ArrivalTime <= dto.RequiredDepartureTime)
                .OrderByDescending(da => da.Service.ArrivalTime)
                .Select(da => da.Service.ArrivalTime)
                .FirstOrDefault();

            double restHours;
            if (closestPrecedingArrival == default)
            {
                // No previous assignment — fully rested
                restHours = 24.0;
            }
            else
            {
                restHours = (dto.RequiredDepartureTime - closestPrecedingArrival).TotalHours;
            }

            if (restHours >= 8.0)
            {
                feasibleDrivers.Add(new FeasibleDriverDto
                {
                    DriverId = driver.Id,
                    FullName = driver.FullName,
                    RestHoursCompleted = Math.Round(restHours, 1)
                });
            }
        }

        return feasibleDrivers.OrderByDescending(d => d.RestHoursCompleted).ToList();
    }
}
