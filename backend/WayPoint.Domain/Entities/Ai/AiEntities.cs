using WayPoint.Domain.Common;
using WayPoint.Domain.Enums;

namespace WayPoint.Domain.Entities.Ai;

public class AiWorkflow : BaseEntity
{
    public string Objective { get; set; } = string.Empty;
    public AiWorkflowStatus Status { get; set; } = AiWorkflowStatus.Running;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public ICollection<AiWorkflowStep> Steps { get; set; } = new List<AiWorkflowStep>();
}

public class AiWorkflowStep : BaseEntity
{
    public Guid AiWorkflowId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string StepDescription { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AiWorkflow AiWorkflow { get; set; } = null!;
    public ICollection<AiToolCall> ToolCalls { get; set; } = new List<AiToolCall>();
    public ICollection<AiValidationResult> ValidationResults { get; set; } = new List<AiValidationResult>();
}

public class AiToolCall : BaseEntity
{
    public Guid AiWorkflowStepId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public int DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AiWorkflowStep AiWorkflowStep { get; set; } = null!;
}

public class AiValidationResult : BaseEntity
{
    public Guid AiWorkflowStepId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? ValidationDetails { get; set; }

    // Navigation properties
    public AiWorkflowStep AiWorkflowStep { get; set; } = null!;
}
