using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Services.Fleet;
using Xunit;

namespace WayPoint.Tests.Fleet;

public class SeatLayoutValidationTests
{
    [Fact]
    public async Task CreateSeatLayout_WithValidCoordinates_ShouldSucceed()
    {
        // Arrange
        var mockContext = new Mock<IWayPointDbContext>();
        var mockLayouts = new List<SeatLayout>().AsQueryable().BuildMockDbSet();
        mockContext.Setup(c => c.SeatLayouts).Returns(mockLayouts.Object);

        var service = new SeatLayoutService(mockContext.Object);

        var request = new CreateSeatLayoutDto
        {
            Name = "Valid Layout",
            TotalRows = 5,
            TotalColumns = 4,
            Seats = new List<CreateSeatDto>
            {
                new CreateSeatDto { SeatNumber = "1A", RowIndex = 0, ColumnIndex = 0 },
                new CreateSeatDto { SeatNumber = "1B", RowIndex = 0, ColumnIndex = 1 },
            }
        };

        // Act
        var result = await service.CreateSeatLayoutAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Valid Layout", result.Name);
        Assert.Equal(2, result.Seats.Count);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSeatLayout_WithOutOfBoundsCoordinates_ShouldThrowArgumentException()
    {
        // Arrange
        var mockContext = new Mock<IWayPointDbContext>();
        var service = new SeatLayoutService(mockContext.Object);

        var request = new CreateSeatLayoutDto
        {
            Name = "Invalid Layout",
            TotalRows = 5,
            TotalColumns = 4,
            Seats = new List<CreateSeatDto>
            {
                new CreateSeatDto { SeatNumber = "1A", RowIndex = 0, ColumnIndex = 0 },
                // Invalid: RowIndex 5 is out of bounds for TotalRows 5 (0 to 4)
                new CreateSeatDto { SeatNumber = "9Z", RowIndex = 5, ColumnIndex = 0 },
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateSeatLayoutAsync(request));
        Assert.Contains("outside grid bounds", ex.Message);
    }

    [Fact]
    public async Task CreateSeatLayout_WithDuplicateSeatNumbers_ShouldThrowArgumentException()
    {
        // Arrange
        var mockContext = new Mock<IWayPointDbContext>();
        var service = new SeatLayoutService(mockContext.Object);

        var request = new CreateSeatLayoutDto
        {
            Name = "Invalid Layout",
            TotalRows = 5,
            TotalColumns = 4,
            Seats = new List<CreateSeatDto>
            {
                new CreateSeatDto { SeatNumber = "1A", RowIndex = 0, ColumnIndex = 0 },
                // Invalid: duplicate seat number
                new CreateSeatDto { SeatNumber = "1A", RowIndex = 0, ColumnIndex = 1 },
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateSeatLayoutAsync(request));
        Assert.Contains("Duplicate seat numbers", ex.Message);
    }

    [Fact]
    public async Task CreateSeatLayout_WithDuplicateCoordinates_ShouldThrowArgumentException()
    {
        // Arrange
        var mockContext = new Mock<IWayPointDbContext>();
        var service = new SeatLayoutService(mockContext.Object);

        var request = new CreateSeatLayoutDto
        {
            Name = "Invalid Layout",
            TotalRows = 5,
            TotalColumns = 4,
            Seats = new List<CreateSeatDto>
            {
                new CreateSeatDto { SeatNumber = "1A", RowIndex = 0, ColumnIndex = 0 },
                // Invalid: duplicate coordinates
                new CreateSeatDto { SeatNumber = "1B", RowIndex = 0, ColumnIndex = 0 },
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateSeatLayoutAsync(request));
        Assert.Contains("Duplicate seat coordinates", ex.Message);
    }
}
