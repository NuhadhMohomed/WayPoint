using WayPoint.Domain.Enums;

namespace WayPoint.Application.Features.DisruptionManagement.DTOs;

public class LogDisruptionDto
{
    public Guid DisruptedServiceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DisruptionSeverity Severity { get; set; } = DisruptionSeverity.Major;
}

public class DisruptionCaseDto
{
    public Guid Id { get; set; }
    public Guid DisruptedServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DisruptionSeverity Severity { get; set; }
    public int AffectedPassengerCount { get; set; }
    public DisruptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DisruptionImpactDto
{
    public Guid DisruptionCaseId { get; set; }
    public int TotalBookedPassengers { get; set; }
    public decimal RevenueAtRisk { get; set; }
    public bool RequiresManagerApproval { get; set; }
    public List<string> AffectedPassengerEmails { get; set; } = new();
}

public class PendingApprovalDto
{
    public Guid RebookingProposalId { get; set; }
    public Guid DisruptionCaseId { get; set; }
    public string OriginalServiceCode { get; set; } = string.Empty;
    public string ReplacementServiceCode { get; set; } = string.Empty;
    public int AffectedPassengersCount { get; set; }
    public string ProposedRemedy { get; set; } = string.Empty;
    public DateTime ProposedAt { get; set; }
}

public class ApprovalDecisionRequestDto
{
    public ApprovalDecisionType Decision { get; set; }
    public string Comments { get; set; } = string.Empty;
}

public class ServiceAlertDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
}
