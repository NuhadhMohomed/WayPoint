using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Disruption;

public interface IAiWorkflowQueryService
{
    /// <summary>
    /// Retrieve a single AI workflow execution with all steps, tool calls,
    /// and validation results for observability inspection (FR-AI-007).
    /// </summary>
    Task<AiWorkflowDto> GetWorkflowByIdAsync(Guid workflowId);

    /// <summary>
    /// List AI workflow executions with status filtering and pagination.
    /// </summary>
    Task<PaginatedResponseDto<AiWorkflowDto>> GetWorkflowsAsync(WorkflowFilterParams filter);
}
