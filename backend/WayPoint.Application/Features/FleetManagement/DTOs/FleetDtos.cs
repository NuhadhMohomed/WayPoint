using WayPoint.Domain.Enums;

namespace WayPoint.Application.Features.FleetManagement.DTOs;

public class BusDto
{
    public Guid Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public BusClass BusClass { get; set; }
    public int TotalSeatCapacity { get; set; }
    public Guid? SeatLayoutId { get; set; }
    public bool IsUnderMaintenance { get; set; }
}

public class CreateBusDto
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public BusClass BusClass { get; set; } = BusClass.Standard;
    public int TotalSeatCapacity { get; set; }
    public Guid? SeatLayoutId { get; set; }
}

public class SeatLayoutDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public List<SeatDto> Seats { get; set; } = new();
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

public class DriverDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}

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
