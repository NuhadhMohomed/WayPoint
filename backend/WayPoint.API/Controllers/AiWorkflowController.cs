using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.API.Controllers;

/// <summary>
/// AI workflow observability — read-only access to workflow execution traces,
/// steps, tool calls, and validation results (FR-AI-007).
/// </summary>
[ApiController]
[Route("api/v1/ai/workflows")]
[Authorize(Policy = "RequireOperator")]
public class AiWorkflowController : ControllerBase
{
    private readonly IAiWorkflowQueryService _workflowQueryService;

    public AiWorkflowController(IAiWorkflowQueryService workflowQueryService)
    {
        _workflowQueryService = workflowQueryService;
    }

    /// <summary>
    /// Retrieve a single AI workflow execution with all steps, tool calls,
    /// and validation results for observability inspection.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowById(Guid id)
    {
        try
        {
            var result = await _workflowQueryService.GetWorkflowByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Workflow Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// List AI workflow executions with optional status filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkflows([FromQuery] WorkflowFilterParams filter)
    {
        var result = await _workflowQueryService.GetWorkflowsAsync(filter);
        return Ok(result);
    }
}
