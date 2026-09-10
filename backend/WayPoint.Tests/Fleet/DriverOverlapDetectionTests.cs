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

public class DriverOverlapDetectionTests
{
    [Fact]
    public async Task AssignDriver_WithNoOverlapAndSufficientRest_ShouldSucceed()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        
        var drivers = new List<Driver>
        {
            new Driver { Id = driverId, FullName = "John Doe", Status = "Active" }
        };

        var services = new List<Service>
        {
            new Service 
            { 
                Id = serviceId, 
                ServiceCode = "SVC-001", 
                DepartureTime = DateTime.UtcNow.AddHours(20),
                ArrivalTime = DateTime.UtcNow.AddHours(25)
            }
        };

        var assignments = new List<DriverAssignment>(); // No existing assignments

        var mockContext = CreateMockContext(drivers, services, assignments);
        var driverService = new DriverService(mockContext.Object);

        // Act
        var result = await driverService.AssignDriverAsync(new AssignDriverDto
        {
            DriverId = driverId,
            ServiceId = serviceId
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driverId, result.DriverId);
        Assert.Equal(serviceId, result.ServiceId);
    }

    [Fact]
    public async Task AssignDriver_WithOverlappingSchedule_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var existingServiceId = Guid.NewGuid();
        var newServiceId = Guid.NewGuid();
        
        var drivers = new List<Driver>
        {
            new Driver { Id = driverId, FullName = "John Doe", Status = "Active" }
        };

        var existingService = new Service 
        { 
            Id = existingServiceId, 
            ServiceCode = "SVC-001", 
            DepartureTime = DateTime.UtcNow.AddHours(10),
            ArrivalTime = DateTime.UtcNow.AddHours(15)
        };

        var newService = new Service 
        { 
            Id = newServiceId, 
            ServiceCode = "SVC-002", 
            // Overlaps with existing: starts at 14, ends at 19
            DepartureTime = DateTime.UtcNow.AddHours(14),
            ArrivalTime = DateTime.UtcNow.AddHours(19)
        };

        var services = new List<Service> { existingService, newService };

        var assignments = new List<DriverAssignment>
        {
            new DriverAssignment
            {
                Id = Guid.NewGuid(),
                DriverId = driverId,
                ServiceId = existingServiceId,
                Service = existingService
            }
        };

        var mockContext = CreateMockContext(drivers, services, assignments);
        var driverService = new DriverService(mockContext.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            driverService.AssignDriverAsync(new AssignDriverDto
            {
                DriverId = driverId,
                ServiceId = newServiceId
            }));
            
        Assert.Contains("Schedule conflict", ex.Message);
    }

    [Fact]
    public async Task AssignDriver_WithInsufficientRestGap_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var existingServiceId = Guid.NewGuid();
        var newServiceId = Guid.NewGuid();
        
        var drivers = new List<Driver>
        {
            new Driver { Id = driverId, FullName = "John Doe", Status = "Active" }
        };

        var existingService = new Service 
        { 
            Id = existingServiceId, 
            ServiceCode = "SVC-001", 
            DepartureTime = DateTime.UtcNow.AddHours(10),
            ArrivalTime = DateTime.UtcNow.AddHours(15) // Arrives at 15
        };

        var newService = new Service 
        { 
            Id = newServiceId, 
            ServiceCode = "SVC-002", 
            DepartureTime = DateTime.UtcNow.AddHours(20), // Departs at 20 (only 5 hours rest)
            ArrivalTime = DateTime.UtcNow.AddHours(25)
        };

        var services = new List<Service> { existingService, newService };

        var assignments = new List<DriverAssignment>
        {
            new DriverAssignment
            {
                Id = Guid.NewGuid(),
                DriverId = driverId,
                ServiceId = existingServiceId,
                Service = existingService
            }
        };

        var mockContext = CreateMockContext(drivers, services, assignments);
        var driverService = new DriverService(mockContext.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            driverService.AssignDriverAsync(new AssignDriverDto
            {
                DriverId = driverId,
                ServiceId = newServiceId
            }));
            
        Assert.Contains("Rest compliance violation (BR-RESOURCE-002)", ex.Message);
        Assert.Contains("5.0 hours of rest", ex.Message);
    }

    [Fact]
    public async Task AssignDriver_WithExactly8HoursRestGap_ShouldSucceed()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var existingServiceId = Guid.NewGuid();
        var newServiceId = Guid.NewGuid();
        
        var drivers = new List<Driver>
        {
            new Driver { Id = driverId, FullName = "John Doe", Status = "Active" }
        };

        var existingService = new Service 
        { 
            Id = existingServiceId, 
            ServiceCode = "SVC-001", 
            DepartureTime = DateTime.UtcNow.AddHours(10),
            ArrivalTime = DateTime.UtcNow.AddHours(15) // Arrives at 15
        };

        var newService = new Service 
        { 
            Id = newServiceId, 
            ServiceCode = "SVC-002", 
            DepartureTime = DateTime.UtcNow.AddHours(23), // Departs at 23 (exactly 8 hours rest)
            ArrivalTime = DateTime.UtcNow.AddHours(28)
        };

        var services = new List<Service> { existingService, newService };

        var assignments = new List<DriverAssignment>
        {
            new DriverAssignment
            {
                Id = Guid.NewGuid(),
                DriverId = driverId,
                ServiceId = existingServiceId,
                Service = existingService
            }
        };

        var mockContext = CreateMockContext(drivers, services, assignments);
        var driverService = new DriverService(mockContext.Object);

        // Act
        var result = await driverService.AssignDriverAsync(new AssignDriverDto
        {
            DriverId = driverId,
            ServiceId = newServiceId
        });

        // Assert
        Assert.NotNull(result);
    }

    private static Mock<IWayPointDbContext> CreateMockContext(
        List<Driver> drivers, 
        List<Service> services, 
        List<DriverAssignment> assignments)
    {
        var mockContext = new Mock<IWayPointDbContext>();
        
        var mockDrivers = drivers.AsQueryable().BuildMockDbSet();
        // Setup FindAsync for Drivers
        mockDrivers.Setup(m => m.FindAsync(It.IsAny<object[]>()))
            .Returns<object[]>(ids => new ValueTask<Driver?>(drivers.FirstOrDefault(d => d.Id == (Guid)ids[0])));
            
        var mockServices = services.AsQueryable().BuildMockDbSet();
        // Setup FindAsync for Services
        mockServices.Setup(m => m.FindAsync(It.IsAny<object[]>()))
            .Returns<object[]>(ids => new ValueTask<Service?>(services.FirstOrDefault(s => s.Id == (Guid)ids[0])));
            
        var mockAssignments = assignments.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.Drivers).Returns(mockDrivers.Object);
        mockContext.Setup(c => c.Services).Returns(mockServices.Object);
        mockContext.Setup(c => c.DriverAssignments).Returns(mockAssignments.Object);
        
        return mockContext;
    }
}
