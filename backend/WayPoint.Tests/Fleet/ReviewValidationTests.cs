using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Services.Fleet;
using Xunit;

namespace WayPoint.Tests.Fleet;

public class ReviewValidationTests
{
    [Fact]
    public async Task SubmitBusReview_WithValidBooking_ShouldSucceed()
    {
        // Arrange
        var mockContext = new Mock<IWayPointDbContext>();
        
        var passengerId = Guid.NewGuid();
        var busId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = bookingId,
                PassengerId = passengerId,
                Status = BookingStatus.Confirmed,
                Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-1), BusId = busId }
            }
        }.AsQueryable().BuildMockDbSet();

        var innerList = new List<BusReview>();
        var busReviews = innerList.AsQueryable().BuildMockDbSet();
        
        // Setup AddAsync to add to the inner list so it can be found by GetBusReviewDtoAsync
        busReviews.Setup(d => d.AddAsync(It.IsAny<BusReview>(), It.IsAny<CancellationToken>()))
            .Callback((BusReview review, CancellationToken token) => 
            {
                review.Id = Guid.NewGuid(); // Simulate DB generated ID
                review.Passenger = new PassengerProfile { User = new User { FullName = "Test" } };
                innerList.Add(review);
            })
            .ReturnsAsync((BusReview review, CancellationToken token) => null!);

        mockContext.Setup(c => c.Bookings).Returns(bookings.Object);
        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);

        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto
        {
            BusId = busId,
            PassengerId = passengerId,
            BookingId = bookingId,
            Rating = 5,
            Comment = "Excellent trip"
        };

        // Act
        var result = await service.SubmitBusReviewAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Rating);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitBusReview_WithInvalidRating_ShouldThrowArgumentException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto { Rating = 6 };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBusReviewAsync(request));
    }

    [Fact]
    public async Task SubmitBusReview_WithUnconfirmedBooking_ShouldThrowInvalidOperationException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var passengerId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = bookingId,
                PassengerId = passengerId,
                Status = BookingStatus.Cancelled,
                Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-1) }
            }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.Bookings).Returns(bookings.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto { BookingId = bookingId, PassengerId = passengerId, Rating = 4 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitBusReviewAsync(request));
        Assert.Contains("Only confirmed bookings", ex.Message);
    }

    [Fact]
    public async Task SubmitBusReview_BeforeTripCompletes_ShouldThrowInvalidOperationException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var passengerId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = bookingId,
                PassengerId = passengerId,
                Status = BookingStatus.Confirmed,
                Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(1) } // Future
            }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.Bookings).Returns(bookings.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto { BookingId = bookingId, PassengerId = passengerId, Rating = 4 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitBusReviewAsync(request));
        Assert.Contains("after it has completed", ex.Message);
    }

    [Fact]
    public async Task SubmitBusReview_AfterReviewWindow_ShouldThrowInvalidOperationException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var passengerId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = bookingId,
                PassengerId = passengerId,
                Status = BookingStatus.Confirmed,
                Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-8) } // 8 days ago
            }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.Bookings).Returns(bookings.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto { BookingId = bookingId, PassengerId = passengerId, Rating = 4 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitBusReviewAsync(request));
        Assert.Contains("Review window has expired", ex.Message);
    }

    [Fact]
    public async Task SubmitBusReview_DuplicateReview_ShouldThrowInvalidOperationException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var passengerId = Guid.NewGuid();
        var busId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = bookingId,
                PassengerId = passengerId,
                Status = BookingStatus.Confirmed,
                Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-1), BusId = busId }
            }
        }.AsQueryable().BuildMockDbSet();

        var busReviews = new List<BusReview>
        {
            new BusReview { BusId = busId, BookingId = bookingId }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.Bookings).Returns(bookings.Object);
        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto { BusId = busId, BookingId = bookingId, PassengerId = passengerId, Rating = 4 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitBusReviewAsync(request));
        Assert.Contains("already reviewed", ex.Message);
    }

    [Fact]
    public async Task SubmitBusReview_WithProfanity_ShouldThrowArgumentException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto { Rating = 2, Comment = "This trip was shit!" };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBusReviewAsync(request));
        Assert.Contains("inappropriate language", ex.Message);
    }

    [Fact]
    public async Task UpdateBusReview_WithinWindow_ShouldSucceed()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var reviewId = Guid.NewGuid();

        var busReviews = new List<BusReview>
        {
            new BusReview 
            { 
                Id = reviewId, 
                Rating = 4, 
                Booking = new Booking { Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-2) } },
                Passenger = new PassengerProfile { User = new User { FullName = "Test" } }
            }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new UpdateReviewDto { Rating = 5, Comment = "Updated comment", IsAnonymous = true };

        var result = await service.UpdateBusReviewAsync(reviewId, request);
        Assert.NotNull(result);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Updated comment", result.Comment);
        Assert.True(result.IsAnonymous);
        Assert.Equal("Anonymous Passenger", result.PassengerName);
    }

    [Fact]
    public async Task DeleteBusReview_ByOwner_ShouldSucceed()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var reviewId = Guid.NewGuid();
        var passengerId = Guid.NewGuid();

        var busReviews = new List<BusReview>
        {
            new BusReview { Id = reviewId, PassengerId = passengerId }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);
        mockContext.Setup(c => c.BusReviews.Remove(It.IsAny<BusReview>()));
        var service = new ReviewService(mockContext.Object);

        await service.DeleteBusReviewAsync(reviewId, passengerId);

        mockContext.Verify(c => c.BusReviews.Remove(It.IsAny<BusReview>()), Times.Once);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitBusReview_Anonymous_ShouldReturnAnonymousName()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var passengerId = Guid.NewGuid();
        var busId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = bookingId,
                PassengerId = passengerId,
                Status = BookingStatus.Confirmed,
                Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-1), BusId = busId }
            }
        }.AsQueryable().BuildMockDbSet();

        var innerList = new List<BusReview>();
        var busReviews = innerList.AsQueryable().BuildMockDbSet();
        busReviews.Setup(d => d.AddAsync(It.IsAny<BusReview>(), It.IsAny<CancellationToken>()))
            .Callback((BusReview review, CancellationToken token) =>
            {
                review.Id = Guid.NewGuid();
                review.IsAnonymous = true;
                review.Passenger = new PassengerProfile { User = new User { FullName = "Real Name" } };
                innerList.Add(review);
            })
            .ReturnsAsync((BusReview review, CancellationToken token) => null!);

        mockContext.Setup(c => c.Bookings).Returns(bookings.Object);
        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new CreateBusReviewDto
        {
            BusId = busId,
            PassengerId = passengerId,
            BookingId = bookingId,
            Rating = 4,
            Comment = "Good bus",
            IsAnonymous = true
        };

        var result = await service.SubmitBusReviewAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Anonymous Passenger", result.PassengerName);
    }

    [Fact]
    public async Task UpdateBusReview_AfterWindow_ShouldThrowInvalidOperationException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var reviewId = Guid.NewGuid();

        var busReviews = new List<BusReview>
        {
            new BusReview
            {
                Id = reviewId,
                Rating = 3,
                Booking = new Booking { Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-8) } }, // 8 days ago
                Passenger = new PassengerProfile { User = new User { FullName = "Test" } }
            }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new UpdateReviewDto { Rating = 5, Comment = "Changed my mind" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateBusReviewAsync(reviewId, request));
        Assert.Contains("Review window has expired", ex.Message);
    }

    [Fact]
    public async Task DeleteBusReview_ByNonOwner_ShouldThrowInvalidOperationException()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var reviewId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var busReviews = new List<BusReview>
        {
            new BusReview { Id = reviewId, PassengerId = ownerId }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);
        var service = new ReviewService(mockContext.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteBusReviewAsync(reviewId, attackerId));
        Assert.Contains("your own reviews", ex.Message);
    }

    [Fact]
    public async Task SubmitDriverReview_WithValidBooking_ShouldSucceed()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var passengerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = bookingId,
                PassengerId = passengerId,
                Status = BookingStatus.Confirmed,
                Service = new Service { ArrivalTime = DateTime.UtcNow.AddDays(-1), DriverId = driverId }
            }
        }.AsQueryable().BuildMockDbSet();

        var innerList = new List<DriverReview>();
        var driverReviews = innerList.AsQueryable().BuildMockDbSet();
        driverReviews.Setup(d => d.AddAsync(It.IsAny<DriverReview>(), It.IsAny<CancellationToken>()))
            .Callback((DriverReview review, CancellationToken token) =>
            {
                review.Id = Guid.NewGuid();
                review.Passenger = new PassengerProfile { User = new User { FullName = "Test" } };
                innerList.Add(review);
            })
            .ReturnsAsync((DriverReview review, CancellationToken token) => null!);

        mockContext.Setup(c => c.Bookings).Returns(bookings.Object);
        mockContext.Setup(c => c.DriverReviews).Returns(driverReviews.Object);
        var service = new ReviewService(mockContext.Object);

        var request = new CreateDriverReviewDto
        {
            DriverId = driverId,
            PassengerId = passengerId,
            BookingId = bookingId,
            Rating = 5,
            Comment = "Excellent driver"
        };

        var result = await service.SubmitDriverReviewAsync(request);

        Assert.NotNull(result);
        Assert.Equal(5, result.Rating);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBusRatingSummary_ShouldReturnCorrectAverageAndDistribution()
    {
        var mockContext = new Mock<IWayPointDbContext>();
        var busId = Guid.NewGuid();

        var buses = new List<Bus>
        {
            new Bus { Id = busId, RegistrationNumber = "NB-1234" }
        }.AsQueryable().BuildMockDbSet();

        var busReviews = new List<BusReview>
        {
            new BusReview { BusId = busId, Rating = 5 },
            new BusReview { BusId = busId, Rating = 4 },
            new BusReview { BusId = busId, Rating = 4 },
            new BusReview { BusId = busId, Rating = 3 },
            new BusReview { BusId = busId, Rating = 1 }
        }.AsQueryable().BuildMockDbSet();

        mockContext.Setup(c => c.Buses).Returns(buses.Object);
        mockContext.Setup(c => c.BusReviews).Returns(busReviews.Object);
        var service = new ReviewService(mockContext.Object);

        var summary = await service.GetBusRatingSummaryAsync(busId);

        Assert.Equal(busId, summary.BusId);
        Assert.Equal("NB-1234", summary.RegistrationNumber);
        Assert.Equal(5, summary.TotalReviews);
        Assert.Equal(3.4, summary.AverageRating, 1); // (5+4+4+3+1)/5 = 3.4
        Assert.Equal(1, summary.RatingDistribution[0]); // 1-star: 1
        Assert.Equal(0, summary.RatingDistribution[1]); // 2-star: 0
        Assert.Equal(1, summary.RatingDistribution[2]); // 3-star: 1
        Assert.Equal(2, summary.RatingDistribution[3]); // 4-star: 2
        Assert.Equal(1, summary.RatingDistribution[4]); // 5-star: 1
    }
}
