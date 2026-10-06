using System.Text.Json;
using WayPoint.Application.Features.JourneyPlanning.DTOs;
using Xunit;

namespace WayPoint.Tests;

public class AiDtoDeserializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Deserializes_Synthetic_ServiceId_Without_Throwing()
    {
        var json = """
        {
            "workflow_id": "550e8400-e29b-41d4-a716-446655440000",
            "status": "Completed",
            "agent_reasoning": "Recommended direct AC route",
            "candidate_routes": [
                {
                    "service_id": "SRV-COL-ELLA-0630",
                    "origin": "Colombo",
                    "destination": "Ella",
                    "total_fare": 2400,
                    "duration_minutes": 360,
                    "available_seats": 14,
                    "bus_class": "Super Line Luxury AC"
                }
            ]
        }
        """;

        var result = JsonSerializer.Deserialize<AiWorkflowResultDto>(json, Options);
        Assert.NotNull(result);
        Assert.Single(result!.CandidateRoutes);
        Assert.Equal("SRV-COL-ELLA-0630", result.CandidateRoutes[0].ServiceId);
    }
}
