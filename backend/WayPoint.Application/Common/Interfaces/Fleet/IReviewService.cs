using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Fleet;

public interface IReviewService
{
    Task<BusReviewDto> SubmitBusReviewAsync(CreateBusReviewDto dto);
    Task<DriverReviewDto> SubmitDriverReviewAsync(CreateDriverReviewDto dto);
    
    Task<BusReviewDto> UpdateBusReviewAsync(Guid reviewId, UpdateReviewDto dto);
    Task<DriverReviewDto> UpdateDriverReviewAsync(Guid reviewId, UpdateReviewDto dto);
    
    Task DeleteBusReviewAsync(Guid reviewId, Guid passengerId);
    Task DeleteDriverReviewAsync(Guid reviewId, Guid passengerId);
    
    Task<PaginatedResponseDto<BusReviewDto>> GetBusReviewsAsync(Guid busId, ReviewFilterParams filter);
    Task<PaginatedResponseDto<DriverReviewDto>> GetDriverReviewsAsync(Guid driverId, ReviewFilterParams filter);
    
    Task<BusRatingSummaryDto> GetBusRatingSummaryAsync(Guid busId);
    Task<DriverRatingSummaryDto> GetDriverRatingSummaryAsync(Guid driverId);
}
