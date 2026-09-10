using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Services.Fleet;
using Xunit;

namespace WayPoint.Tests.Fleet;

public class SeatMatrixGeneratorTests
{
    [Fact]
    public async Task GetSeatAvailability_WithNoHoldsOrBookings_ShouldReturnAllAvailable()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        
        var seats = new List<Seat>
        {
            new Seat { Id = Guid.NewGuid(), SeatNumber = "1A", RowIndex = 1, ColumnIndex = 1 },
            new Seat { Id = Guid.NewGuid(), SeatNumber = "1B", RowIndex = 1, ColumnIndex = 2 }
        };

        var services = new List<Service>
        {
            new Service
            {
                Id = serviceId,
                ServiceCode = "SVC-TEST",
                Bus = new Bus
                {
                    SeatLayout = new SeatLayout
                    {
                        Seats = seats
                    }
                }
            }
        };

        var mockContext = CreateMockContext(services, new List<SeatHold>(), new List<Domain.Entities.Booking.Booking>());
        var service = new SeatAvailabilityService(mockContext.Object);

        // Act
        var result = await service.GetSeatAvailabilityAsync(serviceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalSeats);
        Assert.Equal(2, result.AvailableSeats);
        Assert.Equal(0, result.HeldSeats);
        Assert.Equal(0, result.BookedSeats);
        Assert.All(result.Seats, s => Assert.Equal(SeatStatus.Available, s.Status));
    }

    [Fact]
    public async Task GetSeatAvailability_WithActiveHolds_ShouldReturnCorrectStatus()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var heldSeatId = Guid.NewGuid();
        var availableSeatId = Guid.NewGuid();
        
        var seats = new List<Seat>
        {
            new Seat { Id = heldSeatId, SeatNumber = "1A", RowIndex = 1, ColumnIndex = 1 },
            new Seat { Id = availableSeatId, SeatNumber = "1B", RowIndex = 1, ColumnIndex = 2 }
        };

        var services = new List<Service>
        {
            new Service
            {
                Id = serviceId,
                Bus = new Bus { SeatLayout = new SeatLayout { Seats = seats } }
            }
        };

        var seatHolds = new List<SeatHold>
        {
            new SeatHold
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                SeatId = heldSeatId,
                Status = SeatHoldStatus.Held,
                HeldUntil = DateTime.UtcNow.AddMinutes(5) // Active hold
            }
        };

        var mockContext = CreateMockContext(services, seatHolds, new List<Domain.Entities.Booking.Booking>());
        var service = new SeatAvailabilityService(mockContext.Object);

        // Act
        var result = await service.GetSeatAvailabilityAsync(serviceId);

        // Assert
        Assert.Equal(2, result.TotalSeats);
        Assert.Equal(1, result.AvailableSeats);
        Assert.Equal(1, result.HeldSeats);
        
        var heldSeat = result.Seats.First(s => s.Id == heldSeatId);
        Assert.Equal(SeatStatus.Held, heldSeat.Status);
    }

    [Fact]
    public async Task GetSeatAvailability_WithExpiredHolds_ShouldReturnAvailable()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        
        var seats = new List<Seat>
        {
            new Seat { Id = seatId, SeatNumber = "1A", RowIndex = 1, ColumnIndex = 1 }
        };

        var services = new List<Service>
        {
            new Service
            {
                Id = serviceId,
                Bus = new Bus { SeatLayout = new SeatLayout { Seats = seats } }
            }
        };

        var seatHolds = new List<SeatHold>
        {
            new SeatHold
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                SeatId = seatId,
                Status = SeatHoldStatus.Held,
                HeldUntil = DateTime.UtcNow.AddMinutes(-5) // Expired hold!
            }
        };

        var mockContext = CreateMockContext(services, seatHolds, new List<Domain.Entities.Booking.Booking>());
        var service = new SeatAvailabilityService(mockContext.Object);

        // Act
        var result = await service.GetSeatAvailabilityAsync(serviceId);

        // Assert
        Assert.Equal(1, result.TotalSeats);
        Assert.Equal(1, result.AvailableSeats);
        Assert.Equal(0, result.HeldSeats);
        Assert.Equal(SeatStatus.Available, result.Seats[0].Status);
    }

    [Fact]
    public async Task GetSeatAvailability_WithConfirmedBooking_ShouldReturnBooked()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        
        var seats = new List<Seat>
        {
            new Seat { Id = Guid.NewGuid(), SeatNumber = "1A", RowIndex = 1, ColumnIndex = 1 },
            new Seat { Id = Guid.NewGuid(), SeatNumber = "1B", RowIndex = 1, ColumnIndex = 2 }
        };

        var services = new List<Service>
        {
            new Service
            {
                Id = serviceId,
                Bus = new Bus { SeatLayout = new SeatLayout { Seats = seats } }
            }
        };

        var bookings = new List<Domain.Entities.Booking.Booking>
        {
            new Domain.Entities.Booking.Booking
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                SeatNumbers = "1A",
                Status = BookingStatus.Confirmed
            }
        };

        var mockContext = CreateMockContext(services, new List<SeatHold>(), bookings);
        var service = new SeatAvailabilityService(mockContext.Object);

        // Act
        var result = await service.GetSeatAvailabilityAsync(serviceId);

        // Assert
        Assert.Equal(1, result.BookedSeats);
        Assert.Equal(1, result.AvailableSeats);
        
        var bookedSeat = result.Seats.First(s => s.SeatNumber == "1A");
        Assert.Equal(SeatStatus.Booked, bookedSeat.Status);
    }

    private static Mock<IWayPointDbContext> CreateMockContext(
        List<Service> services,
        List<SeatHold> seatHolds,
        List<Domain.Entities.Booking.Booking> bookings)
    {
        var mockContext = new Mock<IWayPointDbContext>();
        
        var mockServices = services.AsQueryable().BuildMockDbSet();
        // Setup FindAsync equivalent or just rely on FirstOrDefaultAsync
        mockContext.Setup(c => c.Services).Returns(mockServices.Object);

        var mockHolds = seatHolds.AsQueryable().BuildMockDbSet();
        mockContext.Setup(c => c.SeatHolds).Returns(mockHolds.Object);

        var mockBookings = bookings.AsQueryable().BuildMockDbSet();
        mockContext.Setup(c => c.Bookings).Returns(mockBookings.Object);

        return mockContext;
    }
}
