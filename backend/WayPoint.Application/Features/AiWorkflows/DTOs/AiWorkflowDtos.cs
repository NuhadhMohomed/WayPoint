using WayPoint.Domain.Enums;

namespace WayPoint.Application.Features.AiWorkflows.DTOs;

// ==========================================================================
// Create DTOs (inbound from AI microservice)
// ==========================================================================

/// <summary>
/// DTO for POST /api/v1/ai/workflows.
/// </summary>
public record CreateAiWorkflowDto(
    string Objective
);

/// <summary>
/// DTO for PUT /api/v1/ai/workflows/{id}/status.
/// </summary>
public record UpdateAiWorkflowStatusDto(
    AiWorkflowStatus Status
);

/// <summary>
/// DTO for POST /api/v1/ai/workflows/{id}/steps.
/// </summary>
public record CreateAiWorkflowStepDto(
    string AgentName,
    int StepOrder,
    string StepDescription = ""
);

/// <summary>
/// DTO for POST /api/v1/ai/workflows/steps/{stepId}/tool-calls.
/// </summary>
public record CreateAiToolCallDto(
    string ToolName,
    string ArgumentsJson = "{}",
    string ResultJson = "{}",
    int DurationMs = 0
);

/// <summary>
/// DTO for POST /api/v1/ai/workflows/steps/{stepId}/validations.
/// </summary>
public record CreateAiValidationResultDto(
    string RuleName,
    bool Passed,
    string? ValidationDetails = null
);


// ==========================================================================
// Response DTOs (outbound to AI microservice and API consumers)
// ==========================================================================

/// <summary>
/// Full workflow response with nested steps, tool calls, and validations.
/// Maps to AiWorkflow entity.
/// </summary>
public record AiWorkflowResponseDto(
    Guid Id,
    string Objective,
    AiWorkflowStatus Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    List<AiWorkflowStepResponseDto> Steps
);

/// <summary>
/// Workflow step response. Maps to AiWorkflowStep entity.
/// </summary>
public record AiWorkflowStepResponseDto(
    Guid Id,
    Guid AiWorkflowId,
    string AgentName,
    int StepOrder,
    string StepDescription,
    DateTime ExecutedAt,
    List<AiToolCallResponseDto> ToolCalls,
    List<AiValidationResultResponseDto> ValidationResults
);

/// <summary>
/// Tool call response. Maps to AiToolCall entity.
/// </summary>
public record AiToolCallResponseDto(
    Guid Id,
    Guid AiWorkflowStepId,
    string ToolName,
    string ArgumentsJson,
    string ResultJson,
    int DurationMs,
    DateTime ExecutedAt
);

/// <summary>
/// Validation result response. Maps to AiValidationResult entity.
/// </summary>
public record AiValidationResultResponseDto(
    Guid Id,
    Guid AiWorkflowStepId,
    string RuleName,
    bool Passed,
    string? ValidationDetails
);
