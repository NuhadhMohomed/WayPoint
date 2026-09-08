using WayPoint.Domain.Common;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;

namespace WayPoint.Domain.Entities.Fleet;

public class Bus : BaseEntity
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public BusClass BusClass { get; set; } = BusClass.Standard;
    public int TotalSeatCapacity { get; set; }
    public Guid SeatLayoutId { get; set; }
    public bool IsUnderMaintenance { get; set; } = false;

    // Navigation properties
    public SeatLayout SeatLayout { get; set; } = null!;
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}

public class SeatLayout : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }

    // Navigation properties
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Bus> Buses { get; set; } = new List<Bus>();
}

public class Seat : BaseEntity
{
    public Guid SeatLayoutId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public SeatClass SeatClass { get; set; } = SeatClass.Standard;
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public SeatLayout SeatLayout { get; set; } = null!;
}

public class Driver : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";

    // Navigation properties
    public ICollection<DriverAssignment> Assignments { get; set; } = new List<DriverAssignment>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}

public class DriverAssignment : BaseEntity
{
    public Guid DriverId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Driver Driver { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

public class MaintenanceRecord : BaseEntity
{
    public Guid BusId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public decimal Cost { get; set; }

    // Navigation properties
    public Bus Bus { get; set; } = null!;
}

public class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string IconCode { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ServiceAmenity> ServiceAmenities { get; set; } = new List<ServiceAmenity>();
}

public class ServiceAmenity : BaseEntity
{
    public Guid ServiceId { get; set; }
    public Guid AmenityId { get; set; }

    // Navigation properties
    public Service Service { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
