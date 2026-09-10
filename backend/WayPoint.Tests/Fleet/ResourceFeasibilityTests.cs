using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Services.Fleet;
using Xunit;

namespace WayPoint.Tests.Fleet;

public class ResourceFeasibilityTests
{
    [Fact]
    public async Task EvaluateFeasibility_WithAvailableBusAndDriver_ShouldReturnFeasible()
    {
        // Arrange
        var request = new ResourceFeasibilityRequestDto
        {
            DisruptedServiceId = Guid.NewGuid(),
            RequiredSeatCapacity = 40,
            RequiredDepartureTime = DateTime.UtcNow.AddHours(2)
        };

        var buses = new List<Bus>
        {
            new Bus
            {
                Id = Guid.NewGuid(),
                RegistrationNumber = "ND-1234",
                TotalSeatCapacity = 45, // >= 40
                IsUnderMaintenance = false
            }
        };

        var drivers = new List<Driver>
        {
            new Driver
            {
                Id = Guid.NewGuid(),
                FullName = "Jane Smith",
                Status = "Active"
            }
        };

        var services = new List<Service>(); // No conflicting services
        var driverAssignments = new List<DriverAssignment>(); // Fully rested driver

        var mockContext = CreateMockContext(buses, drivers, services, driverAssignments);
        var service = new ResourceFeasibilityService(mockContext.Object);

        // Act
        var result = await service.EvaluateFeasibilityAsync(request);

        // Assert
        Assert.True(result.IsFeasible);
        Assert.Single(result.FeasibleBuses);
        Assert.Single(result.FeasibleDrivers);
        Assert.Contains("Feasible:", result.Summary);
    }

    [Fact]
    public async Task EvaluateFeasibility_WithNoBusesMeetingCapacity_ShouldReturnNotFeasible()
    {
        // Arrange
        var request = new ResourceFeasibilityRequestDto
        {
            DisruptedServiceId = Guid.NewGuid(),
            RequiredSeatCapacity = 50,
            RequiredDepartureTime = DateTime.UtcNow.AddHours(2)
        };

        var buses = new List<Bus>
        {
            new Bus
            {
                Id = Guid.NewGuid(),
                RegistrationNumber = "ND-1234",
                TotalSeatCapacity = 45, // < 50
                IsUnderMaintenance = false
            }
        };

        var drivers = new List<Driver>
        {
            new Driver { Id = Guid.NewGuid(), FullName = "Jane Smith", Status = "Active" }
        };

        var mockContext = CreateMockContext(buses, drivers, new List<Service>(), new List<DriverAssignment>());
        var service = new ResourceFeasibilityService(mockContext.Object);

        // Act
        var result = await service.EvaluateFeasibilityAsync(request);

        // Assert
        Assert.False(result.IsFeasible);
        Assert.Empty(result.FeasibleBuses);
        Assert.Contains("No available buses with capacity >= 50", result.Summary);
    }

    [Fact]
    public async Task EvaluateFeasibility_WithAllBusesUnderMaintenance_ShouldReturnNotFeasible()
    {
        // Arrange
        var request = new ResourceFeasibilityRequestDto
        {
            DisruptedServiceId = Guid.NewGuid(),
            RequiredSeatCapacity = 40,
            RequiredDepartureTime = DateTime.UtcNow.AddHours(2)
        };

        var buses = new List<Bus>
        {
            new Bus
            {
                Id = Guid.NewGuid(),
                RegistrationNumber = "ND-1234",
                TotalSeatCapacity = 45,
                IsUnderMaintenance = true // Under maintenance
            }
        };

        var drivers = new List<Driver>
        {
            new Driver { Id = Guid.NewGuid(), FullName = "Jane Smith", Status = "Active" }
        };

        var mockContext = CreateMockContext(buses, drivers, new List<Service>(), new List<DriverAssignment>());
        var service = new ResourceFeasibilityService(mockContext.Object);

        // Act
        var result = await service.EvaluateFeasibilityAsync(request);

        // Assert
        Assert.False(result.IsFeasible);
        Assert.Empty(result.FeasibleBuses);
    }

    [Fact]
    public async Task EvaluateFeasibility_WithDriverLackingRest_ShouldReturnNotFeasible()
    {
        // Arrange
        var request = new ResourceFeasibilityRequestDto
        {
            DisruptedServiceId = Guid.NewGuid(),
            RequiredSeatCapacity = 40,
            RequiredDepartureTime = DateTime.UtcNow.AddHours(10)
        };

        var buses = new List<Bus>
        {
            new Bus
            {
                Id = Guid.NewGuid(),
                RegistrationNumber = "ND-1234",
                TotalSeatCapacity = 45,
                IsUnderMaintenance = false
            }
        };

        var driverId = Guid.NewGuid();
        var drivers = new List<Driver>
        {
            new Driver { Id = driverId, FullName = "Jane Smith", Status = "Active" }
        };

        var previousService = new Service
        {
            Id = Guid.NewGuid(),
            DepartureTime = DateTime.UtcNow.AddHours(1),
            ArrivalTime = DateTime.UtcNow.AddHours(5) // Arrives 5h before required departure
        };

        var services = new List<Service> { previousService };
        var driverAssignments = new List<DriverAssignment>
        {
            new DriverAssignment
            {
                Id = Guid.NewGuid(),
                DriverId = driverId,
                ServiceId = previousService.Id,
                Service = previousService
            }
        };

        var mockContext = CreateMockContext(buses, drivers, services, driverAssignments);
        var service = new ResourceFeasibilityService(mockContext.Object);

        // Act
        var result = await service.EvaluateFeasibilityAsync(request);

        // Assert
        Assert.False(result.IsFeasible);
        Assert.Empty(result.FeasibleDrivers); // No rested drivers
        Assert.Single(result.FeasibleBuses); // Bus was fine
        Assert.Contains("No available drivers with sufficient rest hours", result.Summary);
    }

    private static Mock<IWayPointDbContext> CreateMockContext(
        List<Bus> buses,
        List<Driver> drivers,
        List<Service> services,
        List<DriverAssignment> assignments)
    {
        var mockContext = new Mock<IWayPointDbContext>();
        
        var mockBuses = buses.AsQueryable().BuildMockDbSet();
        mockContext.Setup(c => c.Buses).Returns(mockBuses.Object);

        var mockDrivers = drivers.AsQueryable().BuildMockDbSet();
        mockContext.Setup(c => c.Drivers).Returns(mockDrivers.Object);

        var mockServices = services.AsQueryable().BuildMockDbSet();
        mockContext.Setup(c => c.Services).Returns(mockServices.Object);

        var mockAssignments = assignments.AsQueryable().BuildMockDbSet();
        mockContext.Setup(c => c.DriverAssignments).Returns(mockAssignments.Object);

        return mockContext;
    }
}
