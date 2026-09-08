namespace WayPoint.Application.Features.JourneyPlanning.DTOs;

public class RouteDto
{
    public Guid Id { get; set; }
    public string RouteNumber { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public List<RouteStopDto> Stops { get; set; } = new();
}

public class RouteStopDto
{
    public Guid Id { get; set; }
    public string StopName { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int ArrivalOffsetMinutes { get; set; }
    public decimal DistanceFromOriginKm { get; set; }
}

public class CreateRouteDto
{
    public string RouteNumber { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public List<CreateRouteStopDto> Stops { get; set; } = new();
}

public class CreateRouteStopDto
{
    public string StopName { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int ArrivalOffsetMinutes { get; set; }
    public decimal DistanceFromOriginKm { get; set; }
}

public class JourneySearchRequestDto
{
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime TravelDate { get; set; }
    public int PassengerCount { get; set; } = 1;
    public bool RequiresAc { get; set; } = false;
    public bool DirectOnly { get; set; } = false;
}

public class CandidateJourneyDto
{
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string RouteNumber { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal TotalFare { get; set; }
    public string BusClass { get; set; } = string.Empty;
    public bool IsConnecting { get; set; }
    public int AvailableSeatsCount { get; set; }
    public decimal MatchScore { get; set; }
}
