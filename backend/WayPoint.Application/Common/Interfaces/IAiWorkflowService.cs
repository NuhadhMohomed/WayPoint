using WayPoint.Application.Features.AiWorkflows.DTOs;

namespace WayPoint.Application.Common.Interfaces;

/// <summary>
/// Service interface for AI workflow persistence (FR-AI-005, FR-AI-007).
/// Provides CRUD operations for AiWorkflow, AiWorkflowStep, AiToolCall,
/// and AiValidationResult entities.
/// </summary>
public interface IAiWorkflowService
{
    /// <summary>
    /// Create a new AI workflow record.
    /// </summary>
    Task<AiWorkflowResponseDto> CreateWorkflowAsync(CreateAiWorkflowDto dto);

    /// <summary>
    /// Get a workflow by ID with all nested steps, tool calls, and validations.
    /// </summary>
    Task<AiWorkflowResponseDto> GetWorkflowAsync(Guid workflowId);

    /// <summary>
    /// Update the workflow status (Running, PendingManagerApproval, Completed, SafeFailure).
    /// </summary>
    Task<AiWorkflowResponseDto> UpdateWorkflowStatusAsync(Guid workflowId, UpdateAiWorkflowStatusDto dto);

    /// <summary>
    /// Add a step to an existing workflow.
    /// </summary>
    Task<AiWorkflowStepResponseDto> AddStepAsync(Guid workflowId, CreateAiWorkflowStepDto dto);

    /// <summary>
    /// Add a tool call record to an existing step.
    /// </summary>
    Task<AiToolCallResponseDto> AddToolCallAsync(Guid stepId, CreateAiToolCallDto dto);

    /// <summary>
    /// Add a validation result to an existing step.
    /// </summary>
    Task<AiValidationResultResponseDto> AddValidationResultAsync(Guid stepId, CreateAiValidationResultDto dto);
}
