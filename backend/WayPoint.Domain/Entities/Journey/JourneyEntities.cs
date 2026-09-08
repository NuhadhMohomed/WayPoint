using WayPoint.Domain.Common;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Enums;

namespace WayPoint.Domain.Entities.Journey;

public class Route : BaseEntity
{
    public string RouteCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public decimal TotalDistanceKm { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    public ICollection<BoardingPoint> BoardingPoints { get; set; } = new List<BoardingPoint>();
    public ICollection<TouristDestination> TouristDestinations { get; set; } = new List<TouristDestination>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}

public class RouteStop : BaseEntity
{
    public Guid RouteId { get; set; }
    public string StopName { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int ArrivalOffsetMinutes { get; set; }
    public decimal DistanceFromOriginKm { get; set; }

    // Navigation properties
    public Route Route { get; set; } = null!;
}

public class BoardingPoint : BaseEntity
{
    public Guid RouteId { get; set; }
    public string PointName { get; set; } = string.Empty;
    public string? Landmark { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Navigation properties
    public Route Route { get; set; } = null!;
}

public class TouristDestination : BaseEntity
{
    public Guid RouteId { get; set; }
    public string AttractionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    // Navigation properties
    public Route Route { get; set; } = null!;
}

public class Service : BaseEntity
{
    public string ServiceCode { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    public Guid BusId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal BaseFare { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Scheduled;

    // Navigation properties
    public Route Route { get; set; } = null!;
    public Bus Bus { get; set; } = null!;
    public Driver? Driver { get; set; }
    public ICollection<FareRule> FareRules { get; set; } = new List<FareRule>();
    public ICollection<ServiceAmenity> ServiceAmenities { get; set; } = new List<ServiceAmenity>();
}

public class FareRule : BaseEntity
{
    public Guid ServiceId { get; set; }
    public BusClass BusClass { get; set; } = BusClass.Standard;
    public decimal RatePerKm { get; set; }
    public decimal ClassMultiplier { get; set; } = 1.0m;

    // Navigation properties
    public Service Service { get; set; } = null!;
}

public class JourneySearch : BaseEntity
{
    public Guid? PassengerId { get; set; }
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime TravelDate { get; set; }
    public int PassengerCount { get; set; } = 1;
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PassengerProfile? Passenger { get; set; }
    public ICollection<JourneyCandidate> Candidates { get; set; } = new List<JourneyCandidate>();
}

public class JourneyCandidate : BaseEntity
{
    public Guid JourneySearchId { get; set; }
    public string CandidateType { get; set; } = "Direct"; // Direct or Connecting
    public decimal TotalFare { get; set; }
    public int TotalDurationMinutes { get; set; }
    public decimal MatchScore { get; set; }

    // Navigation properties
    public JourneySearch JourneySearch { get; set; } = null!;
}
