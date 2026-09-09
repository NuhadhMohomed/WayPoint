using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Entities.Fleet;

namespace WayPoint.Infrastructure.Services.Fleet;

public class DriverService : IDriverService
{
    private readonly IWayPointDbContext _context;

    public DriverService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponseDto<DriverDto>> GetDriversAsync(DriverFilterParams filter)
    {
        var query = _context.Drivers
            .Include(d => d.Assignments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(d => d.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.FullName.ToLower().Contains(searchLower) ||
                d.LicenseNumber.ToLower().Contains(searchLower) ||
                d.PhoneNumber.Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        var drivers = await query
            .OrderBy(d => d.FullName)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DriverDto
            {
                Id = d.Id,
                FullName = d.FullName,
                LicenseNumber = d.LicenseNumber,
                PhoneNumber = d.PhoneNumber,
                Status = d.Status,
                AssignmentCount = d.Assignments.Count,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<DriverDto>(drivers, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<DriverDto> GetDriverByIdAsync(Guid id)
    {
        var driver = await _context.Drivers
            .Include(d => d.Assignments)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver == null)
            throw new KeyNotFoundException($"Driver with ID '{id}' was not found.");

        return new DriverDto
        {
            Id = driver.Id,
            FullName = driver.FullName,
            LicenseNumber = driver.LicenseNumber,
            PhoneNumber = driver.PhoneNumber,
            Status = driver.Status,
            AssignmentCount = driver.Assignments.Count,
            CreatedAt = driver.CreatedAt
        };
    }

    public async Task<DriverDto> CreateDriverAsync(CreateDriverDto dto)
    {
        // Validate unique license number
        var normalizedLicense = dto.LicenseNumber.Trim().ToUpperInvariant();
        if (await _context.Drivers.AnyAsync(d => d.LicenseNumber.ToUpper() == normalizedLicense))
        {
            throw new InvalidOperationException($"A driver with license number '{normalizedLicense}' already exists.");
        }

        var driver = new Driver
        {
            FullName = dto.FullName.Trim(),
            LicenseNumber = normalizedLicense,
            PhoneNumber = dto.PhoneNumber.Trim(),
            Status = "Active"
        };

        await _context.Drivers.AddAsync(driver);
        await _context.SaveChangesAsync();

        return new DriverDto
        {
            Id = driver.Id,
            FullName = driver.FullName,
            LicenseNumber = driver.LicenseNumber,
            PhoneNumber = driver.PhoneNumber,
            Status = driver.Status,
            AssignmentCount = 0,
            CreatedAt = driver.CreatedAt
        };
    }

    /// <summary>
    /// Assigns a driver to a service departure with schedule overlap detection (BR-TIME-001)
    /// and 8-hour rest gap validation (BR-RESOURCE-002).
    /// </summary>
    public async Task<DriverAssignmentDto> AssignDriverAsync(AssignDriverDto dto)
    {
        // Validate driver exists and is active
        var driver = await _context.Drivers.FindAsync(dto.DriverId);
        if (driver == null)
            throw new KeyNotFoundException($"Driver with ID '{dto.DriverId}' was not found.");

        if (driver.Status != "Active")
            throw new InvalidOperationException($"Driver '{driver.FullName}' is not active (current status: {driver.Status}).");

        // Validate service exists
        var service = await _context.Services.FindAsync(dto.ServiceId);
        if (service == null)
            throw new KeyNotFoundException($"Service with ID '{dto.ServiceId}' was not found.");

        // Check for duplicate assignment
        var existingAssignment = await _context.DriverAssignments
            .AnyAsync(da => da.DriverId == dto.DriverId && da.ServiceId == dto.ServiceId);

        if (existingAssignment)
            throw new InvalidOperationException($"Driver '{driver.FullName}' is already assigned to service '{service.ServiceCode}'.");

        // Get all existing assignments for this driver with their service times
        var existingAssignments = await _context.DriverAssignments
            .Where(da => da.DriverId == dto.DriverId)
            .Include(da => da.Service)
            .ToListAsync();

        // BR-TIME-001: Check for schedule overlap
        // A schedule overlaps if: NewDeparture < ExistingArrival AND NewArrival > ExistingDeparture
        foreach (var assignment in existingAssignments)
        {
            var existingDeparture = assignment.Service.DepartureTime;
            var existingArrival = assignment.Service.ArrivalTime;

            if (service.DepartureTime < existingArrival && service.ArrivalTime > existingDeparture)
            {
                throw new InvalidOperationException(
                    $"Schedule conflict: Driver '{driver.FullName}' is already assigned to service " +
                    $"'{assignment.Service.ServiceCode}' ({existingDeparture:g} – {existingArrival:g}), " +
                    $"which overlaps with the requested service '{service.ServiceCode}' " +
                    $"({service.DepartureTime:g} – {service.ArrivalTime:g}).");
            }
        }

        // BR-RESOURCE-002: Check 8-hour rest gap
        // Find the closest preceding service (arrival before this departure)
        var closestPrecedingArrival = existingAssignments
            .Where(da => da.Service.ArrivalTime <= service.DepartureTime)
            .OrderByDescending(da => da.Service.ArrivalTime)
            .Select(da => da.Service.ArrivalTime)
            .FirstOrDefault();

        if (closestPrecedingArrival != default)
        {
            var restGap = service.DepartureTime - closestPrecedingArrival;
            if (restGap.TotalHours < 8)
            {
                throw new InvalidOperationException(
                    $"Rest compliance violation (BR-RESOURCE-002): Driver '{driver.FullName}' has only " +
                    $"{restGap.TotalHours:F1} hours of rest before the requested departure at " +
                    $"{service.DepartureTime:g}. Minimum 8 hours required. Previous service arrived at " +
                    $"{closestPrecedingArrival:g}.");
            }
        }

        // Also check if the driver needs rest after this service before a subsequent one
        var closestFollowingDeparture = existingAssignments
            .Where(da => da.Service.DepartureTime >= service.ArrivalTime)
            .OrderBy(da => da.Service.DepartureTime)
            .Select(da => da.Service.DepartureTime)
            .FirstOrDefault();

        if (closestFollowingDeparture != default)
        {
            var restGapAfter = closestFollowingDeparture - service.ArrivalTime;
            if (restGapAfter.TotalHours < 8)
            {
                throw new InvalidOperationException(
                    $"Rest compliance violation (BR-RESOURCE-002): Assigning this service would leave " +
                    $"driver '{driver.FullName}' with only {restGapAfter.TotalHours:F1} hours of rest " +
                    $"before the next service departing at {closestFollowingDeparture:g}. " +
                    $"Minimum 8 hours required.");
            }
        }

        // All validations passed — create assignment
        var driverAssignment = new DriverAssignment
        {
            DriverId = dto.DriverId,
            ServiceId = dto.ServiceId,
            AssignedAt = DateTime.UtcNow
        };

        await _context.DriverAssignments.AddAsync(driverAssignment);
        await _context.SaveChangesAsync();

        return new DriverAssignmentDto
        {
            Id = driverAssignment.Id,
            DriverId = driver.Id,
            DriverName = driver.FullName,
            ServiceId = service.Id,
            ServiceCode = service.ServiceCode,
            DepartureTime = service.DepartureTime,
            ArrivalTime = service.ArrivalTime,
            AssignedAt = driverAssignment.AssignedAt
        };
    }

    public async Task<List<DriverAssignmentDto>> GetDriverAssignmentsAsync(Guid driverId)
    {
        var driverExists = await _context.Drivers.AnyAsync(d => d.Id == driverId);
        if (!driverExists)
            throw new KeyNotFoundException($"Driver with ID '{driverId}' was not found.");

        return await _context.DriverAssignments
            .Where(da => da.DriverId == driverId)
            .Include(da => da.Service)
            .Include(da => da.Driver)
            .OrderBy(da => da.Service.DepartureTime)
            .Select(da => new DriverAssignmentDto
            {
                Id = da.Id,
                DriverId = da.DriverId,
                DriverName = da.Driver.FullName,
                ServiceId = da.ServiceId,
                ServiceCode = da.Service.ServiceCode,
                DepartureTime = da.Service.DepartureTime,
                ArrivalTime = da.Service.ArrivalTime,
                AssignedAt = da.AssignedAt
            })
            .ToListAsync();
    }
}
