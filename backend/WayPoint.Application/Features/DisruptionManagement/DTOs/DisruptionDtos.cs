using WayPoint.Domain.Enums;

namespace WayPoint.Application.Features.DisruptionManagement.DTOs;

// ──────────────────────────────────────────────────────
// Request / Input DTOs
// ──────────────────────────────────────────────────────

public class LogDisruptionDto
{
    public Guid DisruptedServiceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DisruptionSeverity Severity { get; set; } = DisruptionSeverity.Major;

    /// <summary>
    /// Optional manual override. If null, the service auto-computes from confirmed bookings.
    /// </summary>
    public int? AffectedPassengerCountOverride { get; set; }
}

public class CreateRebookingProposalDto
{
    public Guid DisruptionCaseId { get; set; }
    public Guid ReplacementServiceId { get; set; }
    public string ProposedByAgent { get; set; } = "Manual";
}

public class ApprovalDecisionRequestDto
{
    public ApprovalDecisionType Decision { get; set; }
    public string Comments { get; set; } = string.Empty;
}

public class CreateServiceAlertDto
{
    public Guid ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// ──────────────────────────────────────────────────────
// Filter / Query Params
// ──────────────────────────────────────────────────────

public class DisruptionFilterParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DisruptionSeverity? Severity { get; set; }
    public DisruptionStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
}

public class WorkflowFilterParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public AiWorkflowStatus? Status { get; set; }
}

// ──────────────────────────────────────────────────────
// Response / Output DTOs
// ──────────────────────────────────────────────────────

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
    public string ServiceCode { get; set; } = string.Empty;
    public DisruptionSeverity Severity { get; set; }
    public int TotalBookedPassengers { get; set; }
    public decimal RevenueAtRisk { get; set; }
    public bool RequiresManagerApproval { get; set; }
    public string ImpactClassification { get; set; } = "Low";
    public List<string> AffectedPassengerEmails { get; set; } = new();
    public List<Guid> AffectedBookingIds { get; set; } = new();
}

public class RebookingProposalDto
{
    public Guid Id { get; set; }
    public Guid DisruptionCaseId { get; set; }
    public string OriginalServiceCode { get; set; } = string.Empty;
    public Guid ReplacementServiceId { get; set; }
    public string ReplacementServiceCode { get; set; } = string.Empty;
    public string ProposedByAgent { get; set; } = string.Empty;
    public RebookingStatus Status { get; set; }
    public int AffectedPassengersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingApprovalDto
{
    public Guid RebookingProposalId { get; set; }
    public Guid DisruptionCaseId { get; set; }
    public string OriginalServiceCode { get; set; } = string.Empty;
    public string ReplacementServiceCode { get; set; } = string.Empty;
    public int AffectedPassengersCount { get; set; }
    public decimal RevenueAtRisk { get; set; }
    public string DisruptionReason { get; set; } = string.Empty;
    public DisruptionSeverity Severity { get; set; }
    public string ProposedByAgent { get; set; } = string.Empty;
    public DateTime ProposedAt { get; set; }
}

public class ApprovalDecisionDto
{
    public Guid Id { get; set; }
    public Guid RebookingProposalId { get; set; }
    public Guid ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public ApprovalDecisionType Decision { get; set; }
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; }
}

public class RebookingExecutionResultDto
{
    public bool Success { get; set; }
    public int PassengersRebooked { get; set; }
    public int SeatsReleased { get; set; }
    public int SeatsLocked { get; set; }
    public int TicketsReissued { get; set; }
    public decimal FareDifferenceRefunded { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class ServiceAlertDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
}

// ──────────────────────────────────────────────────────
// AI Workflow Observability DTOs
// ──────────────────────────────────────────────────────

public class AiWorkflowDto
{
    public Guid Id { get; set; }
    public string Objective { get; set; } = string.Empty;
    public AiWorkflowStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalDurationMs { get; set; }
    public List<AiWorkflowStepDto> Steps { get; set; } = new();
}

public class AiWorkflowStepDto
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string StepDescription { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public List<AiToolCallDto> ToolCalls { get; set; } = new();
    public List<AiValidationResultDto> ValidationResults { get; set; } = new();
}

public class AiToolCallDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public int DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class AiValidationResultDto
{
    public Guid Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? ValidationDetails { get; set; }
}
