using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.FleetManagement.DTOs;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Fleet;

public class ReviewService : IReviewService
{
    private readonly IWayPointDbContext _context;

    // Basic profanity filter
    private static readonly HashSet<string> _bannedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "fuck", "shit", "asshole", "bitch", "crap", "bastard"
    };

    public ReviewService(IWayPointDbContext context)
    {
        _context = context;
    }

    private void ValidateRating(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }
    }

    private string? SanitizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return comment;

        var words = comment.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (_bannedWords.Contains(word))
            {
                throw new ArgumentException("Review contains inappropriate language.");
            }
        }

        return comment.Trim();
    }

    private async Task ValidateBookingForReviewAsync(Guid bookingId, Guid passengerId, Guid? busId, Guid? driverId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new InvalidOperationException("Booking not found.");

        if (booking.PassengerId != passengerId)
            throw new InvalidOperationException("You can only review your own bookings.");

        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be reviewed.");

        if (booking.Service.ArrivalTime > DateTime.UtcNow)
            throw new InvalidOperationException("You can only review a trip after it has completed.");

        if (DateTime.UtcNow > booking.Service.ArrivalTime.AddDays(7))
            throw new InvalidOperationException("Review window has expired. Reviews must be submitted within 7 days of the trip.");

        if (busId.HasValue && booking.Service.BusId != busId.Value)
            throw new InvalidOperationException("The bus being reviewed was not assigned to this booking.");

        if (driverId.HasValue && booking.Service.DriverId != driverId.Value)
            throw new InvalidOperationException("The driver being reviewed was not assigned to this booking.");
    }

    public async Task<BusReviewDto> SubmitBusReviewAsync(CreateBusReviewDto dto)
    {
        ValidateRating(dto.Rating);
        dto.Comment = SanitizeComment(dto.Comment);

        await ValidateBookingForReviewAsync(dto.BookingId, dto.PassengerId, dto.BusId, null);

        var existingReview = await _context.BusReviews
            .FirstOrDefaultAsync(br => br.BusId == dto.BusId && br.BookingId == dto.BookingId);

        if (existingReview != null)
            throw new InvalidOperationException("You have already reviewed this bus for this booking.");

        var review = new BusReview
        {
            BusId = dto.BusId,
            PassengerId = dto.PassengerId,
            BookingId = dto.BookingId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            IsAnonymous = dto.IsAnonymous
        };

        await _context.BusReviews.AddAsync(review);
        await _context.SaveChangesAsync();

        return await GetBusReviewDtoAsync(review.Id);
    }

    public async Task<DriverReviewDto> SubmitDriverReviewAsync(CreateDriverReviewDto dto)
    {
        ValidateRating(dto.Rating);
        dto.Comment = SanitizeComment(dto.Comment);

        await ValidateBookingForReviewAsync(dto.BookingId, dto.PassengerId, null, dto.DriverId);

        var existingReview = await _context.DriverReviews
            .FirstOrDefaultAsync(dr => dr.DriverId == dto.DriverId && dr.BookingId == dto.BookingId);

        if (existingReview != null)
            throw new InvalidOperationException("You have already reviewed this driver for this booking.");

        var review = new DriverReview
        {
            DriverId = dto.DriverId,
            PassengerId = dto.PassengerId,
            BookingId = dto.BookingId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            IsAnonymous = dto.IsAnonymous
        };

        await _context.DriverReviews.AddAsync(review);
        await _context.SaveChangesAsync();

        return await GetDriverReviewDtoAsync(review.Id);
    }

    public async Task<BusReviewDto> UpdateBusReviewAsync(Guid reviewId, UpdateReviewDto dto)
    {
        var review = await _context.BusReviews
            .Include(r => r.Booking)
            .ThenInclude(b => b.Service)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
            throw new KeyNotFoundException("Review not found.");

        if (DateTime.UtcNow > review.Booking.Service.ArrivalTime.AddDays(7))
            throw new InvalidOperationException("Review window has expired. Reviews can only be edited within 7 days of the trip.");

        ValidateRating(dto.Rating);
        review.Rating = dto.Rating;
        review.Comment = SanitizeComment(dto.Comment);
        review.IsAnonymous = dto.IsAnonymous;

        await _context.SaveChangesAsync();

        return await GetBusReviewDtoAsync(review.Id);
    }

    public async Task<DriverReviewDto> UpdateDriverReviewAsync(Guid reviewId, UpdateReviewDto dto)
    {
        var review = await _context.DriverReviews
            .Include(r => r.Booking)
            .ThenInclude(b => b.Service)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
            throw new KeyNotFoundException("Review not found.");

        if (DateTime.UtcNow > review.Booking.Service.ArrivalTime.AddDays(7))
            throw new InvalidOperationException("Review window has expired. Reviews can only be edited within 7 days of the trip.");

        ValidateRating(dto.Rating);
        review.Rating = dto.Rating;
        review.Comment = SanitizeComment(dto.Comment);
        review.IsAnonymous = dto.IsAnonymous;

        await _context.SaveChangesAsync();

        return await GetDriverReviewDtoAsync(review.Id);
    }

    public async Task DeleteBusReviewAsync(Guid reviewId, Guid passengerId)
    {
        var review = await _context.BusReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        
        if (review == null)
            throw new KeyNotFoundException("Review not found.");

        if (review.PassengerId != passengerId)
            throw new InvalidOperationException("You can only delete your own reviews.");

        _context.BusReviews.Remove(review);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDriverReviewAsync(Guid reviewId, Guid passengerId)
    {
        var review = await _context.DriverReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        
        if (review == null)
            throw new KeyNotFoundException("Review not found.");

        if (review.PassengerId != passengerId)
            throw new InvalidOperationException("You can only delete your own reviews.");

        _context.DriverReviews.Remove(review);
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponseDto<BusReviewDto>> GetBusReviewsAsync(Guid busId, ReviewFilterParams filter)
    {
        var query = _context.BusReviews
            .Include(r => r.Passenger)
            .ThenInclude(p => p.User)
            .Where(r => r.BusId == busId);

        if (filter.MinRating.HasValue)
            query = query.Where(r => r.Rating >= filter.MinRating.Value);

        if (filter.MaxRating.HasValue)
            query = query.Where(r => r.Rating <= filter.MaxRating.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new BusReviewDto
            {
                Id = r.Id,
                BusId = r.BusId,
                PassengerName = r.IsAnonymous ? "Anonymous Passenger" : r.Passenger.User.FullName,
                IsAnonymous = r.IsAnonymous,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<BusReviewDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<PaginatedResponseDto<DriverReviewDto>> GetDriverReviewsAsync(Guid driverId, ReviewFilterParams filter)
    {
        var query = _context.DriverReviews
            .Include(r => r.Passenger)
            .ThenInclude(p => p.User)
            .Where(r => r.DriverId == driverId);

        if (filter.MinRating.HasValue)
            query = query.Where(r => r.Rating >= filter.MinRating.Value);

        if (filter.MaxRating.HasValue)
            query = query.Where(r => r.Rating <= filter.MaxRating.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new DriverReviewDto
            {
                Id = r.Id,
                DriverId = r.DriverId,
                PassengerName = r.IsAnonymous ? "Anonymous Passenger" : r.Passenger.User.FullName,
                IsAnonymous = r.IsAnonymous,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<DriverReviewDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<BusRatingSummaryDto> GetBusRatingSummaryAsync(Guid busId)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == busId);
        if (bus == null) throw new KeyNotFoundException("Bus not found");

        var ratings = await _context.BusReviews
            .Where(r => r.BusId == busId)
            .Select(r => r.Rating)
            .ToListAsync();

        var summary = new BusRatingSummaryDto
        {
            BusId = busId,
            RegistrationNumber = bus.RegistrationNumber,
            TotalReviews = ratings.Count,
            AverageRating = ratings.Count > 0 ? ratings.Average() : 0,
            RatingDistribution = new int[5]
        };

        foreach (var r in ratings)
        {
            if (r >= 1 && r <= 5)
            {
                summary.RatingDistribution[r - 1]++;
            }
        }

        return summary;
    }

    public async Task<DriverRatingSummaryDto> GetDriverRatingSummaryAsync(Guid driverId)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
        if (driver == null) throw new KeyNotFoundException("Driver not found");

        var ratings = await _context.DriverReviews
            .Where(r => r.DriverId == driverId)
            .Select(r => r.Rating)
            .ToListAsync();

        var summary = new DriverRatingSummaryDto
        {
            DriverId = driverId,
            FullName = driver.FullName,
            TotalReviews = ratings.Count,
            AverageRating = ratings.Count > 0 ? ratings.Average() : 0,
            RatingDistribution = new int[5]
        };

        foreach (var r in ratings)
        {
            if (r >= 1 && r <= 5)
            {
                summary.RatingDistribution[r - 1]++;
            }
        }

        return summary;
    }

    private async Task<BusReviewDto> GetBusReviewDtoAsync(Guid reviewId)
    {
        var review = await _context.BusReviews
            .Include(r => r.Passenger)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null) throw new KeyNotFoundException("Review not found.");

        return new BusReviewDto
        {
            Id = review.Id,
            BusId = review.BusId,
            PassengerName = review.IsAnonymous ? "Anonymous Passenger" : review.Passenger.User.FullName,
            IsAnonymous = review.IsAnonymous,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }

    private async Task<DriverReviewDto> GetDriverReviewDtoAsync(Guid reviewId)
    {
        var review = await _context.DriverReviews
            .Include(r => r.Passenger)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null) throw new KeyNotFoundException("Review not found.");

        return new DriverReviewDto
        {
            Id = review.Id,
            DriverId = review.DriverId,
            PassengerName = review.IsAnonymous ? "Anonymous Passenger" : review.Passenger.User.FullName,
            IsAnonymous = review.IsAnonymous,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}
