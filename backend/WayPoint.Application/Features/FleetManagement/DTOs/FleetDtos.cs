using WayPoint.Domain.Enums;

namespace WayPoint.Application.Features.FleetManagement.DTOs;

// ─── Bus DTOs ───

public class BusDto
{
    public Guid Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public BusClass BusClass { get; set; }
    public int TotalSeatCapacity { get; set; }
    public Guid? SeatLayoutId { get; set; }
    public string? SeatLayoutName { get; set; }
    public bool IsUnderMaintenance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBusDto
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public BusClass BusClass { get; set; } = BusClass.Standard;
    public int TotalSeatCapacity { get; set; }
    public Guid? SeatLayoutId { get; set; }
}

public class MaintenanceToggleDto
{
    public bool IsUnderMaintenance { get; set; }
    public string? Description { get; set; }
    public decimal? Cost { get; set; }
}

// ─── Seat Layout DTOs ───

public class SeatLayoutDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public List<SeatDto> Seats { get; set; } = new();
}

public class SeatLayoutSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public int TotalSeats { get; set; }
}

public class CreateSeatLayoutDto
{
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public List<CreateSeatDto> Seats { get; set; } = new();
}

public class CreateSeatDto
{
    public string SeatNumber { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public SeatClass SeatClass { get; set; } = SeatClass.Standard;
}

public class SeatDto
{
    public Guid Id { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public SeatClass SeatClass { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
}

// ─── Driver DTOs ───

public class DriverDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int AssignmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDriverDto
{
    public string FullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class AssignDriverDto
{
    public Guid DriverId { get; set; }
    public Guid ServiceId { get; set; }
}

public class DriverAssignmentDto
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime AssignedAt { get; set; }
}

// ─── Seat Availability Matrix ───

public class ServiceSeatMatrixDto
{
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int HeldSeats { get; set; }
    public int BookedSeats { get; set; }
    public List<SeatDto> Seats { get; set; } = new();
}

// ─── Resource Feasibility DTOs ───

public class ResourceFeasibilityRequestDto
{
    public Guid DisruptedServiceId { get; set; }
    public int RequiredSeatCapacity { get; set; }
    public DateTime RequiredDepartureTime { get; set; }
}

public class ResourceFeasibilityResponseDto
{
    public bool IsFeasible { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<FeasibleBusDto> FeasibleBuses { get; set; } = new();
    public List<FeasibleDriverDto> FeasibleDrivers { get; set; } = new();
}

public class FeasibleBusDto
{
    public Guid BusId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public BusClass BusClass { get; set; }
    public int SeatCapacity { get; set; }
}

public class FeasibleDriverDto
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public double RestHoursCompleted { get; set; }
}

// ─── Filter / Pagination Parameters ───

public class BusFilterParams
{
    public BusClass? BusClass { get; set; }
    public bool? IsUnderMaintenance { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DriverFilterParams
{
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ─── Review DTOs ───

public class CreateBusReviewDto
{
    public Guid BusId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; } = false;
}

public class CreateDriverReviewDto
{
    public Guid DriverId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; } = false;
}

public class UpdateReviewDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; } = false;
}

public class BusReviewDto
{
    public Guid Id { get; set; }
    public Guid BusId { get; set; }
    public string PassengerName { get; set; } = string.Empty;  // "Anonymous" if IsAnonymous
    public bool IsAnonymous { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DriverReviewDto
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public string PassengerName { get; set; } = string.Empty;  // "Anonymous" if IsAnonymous
    public bool IsAnonymous { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BusRatingSummaryDto
{
    public Guid BusId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int[] RatingDistribution { get; set; } = new int[5]; // [0]=1-star count, ..., [4]=5-star count
}

public class DriverRatingSummaryDto
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int[] RatingDistribution { get; set; } = new int[5];
}

public class ReviewFilterParams
{
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
