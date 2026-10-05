using System.Text.Json.Serialization;

namespace WayPoint.Application.Features.JourneyPlanning.DTOs;

public class AiJourneyRecommendationRequestDto
{
    public string Objective { get; set; } = string.Empty;
    public int PassengerCount { get; set; } = 1;
    public DateTime? TravelDate { get; set; }
}

public class AiJourneyRecommendationResponseDto
{
    public Guid WorkflowId { get; set; }
    public string Status { get; set; } = "Completed";
    public string AgentReasoning { get; set; } = string.Empty;
    public bool IsAiFallback { get; set; }
    public List<CandidateJourneyDto> Candidates { get; set; } = new();
}

public class AiCandidateRouteDto
{
    [JsonPropertyName("service_id")]
    public Guid ServiceId { get; set; }

    [JsonPropertyName("service_code")]
    public string ServiceCode { get; set; } = string.Empty;

    [JsonPropertyName("route_number")]
    public string RouteNumber { get; set; } = string.Empty;

    [JsonPropertyName("origin")]
    public string Origin { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("departure_time")]
    public DateTime DepartureTime { get; set; }

    [JsonPropertyName("arrival_time")]
    public DateTime ArrivalTime { get; set; }

    [JsonPropertyName("total_fare")]
    public decimal TotalFare { get; set; }

    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("is_connecting")]
    public bool IsConnecting { get; set; }

    [JsonPropertyName("match_score")]
    public decimal MatchScore { get; set; }

    [JsonPropertyName("available_seats")]
    public int AvailableSeats { get; set; }

    [JsonPropertyName("bus_class")]
    public string BusClass { get; set; } = string.Empty;
}

public class AiWorkflowResultDto
{
    [JsonPropertyName("workflow_id")]
    public Guid WorkflowId { get; set; }

    [JsonPropertyName("workflow_type")]
    public string WorkflowType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("steps_completed")]
    public int StepsCompleted { get; set; }

    [JsonPropertyName("candidate_routes")]
    public List<AiCandidateRouteDto> CandidateRoutes { get; set; } = new();

    [JsonPropertyName("agent_reasoning")]
    public string AgentReasoning { get; set; } = string.Empty;
}
