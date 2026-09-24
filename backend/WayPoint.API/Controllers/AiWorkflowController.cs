using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Features.AiWorkflows.DTOs;

namespace WayPoint.API.Controllers;

/// <summary>
/// AI Workflow persistence endpoints (FR-AI-005, FR-AI-007).
/// Called by the AI microservice to persist workflow execution traces.
/// 
/// Endpoints:
///   POST   /api/v1/ai/workflows                         - Create workflow
///   GET    /api/v1/ai/workflows/{id}                     - Get workflow (full trace)
///   PUT    /api/v1/ai/workflows/{id}/status              - Update status
///   POST   /api/v1/ai/workflows/{id}/steps               - Add step
///   POST   /api/v1/ai/workflows/steps/{stepId}/tool-calls      - Add tool call
///   POST   /api/v1/ai/workflows/steps/{stepId}/validations     - Add validation
/// </summary>
[ApiController]
[Route("api/v1/ai/workflows")]
public class AiWorkflowController : ControllerBase
{
    private readonly IAiWorkflowService _aiWorkflowService;

    public AiWorkflowController(IAiWorkflowService aiWorkflowService)
    {
        _aiWorkflowService = aiWorkflowService;
    }

    /// <summary>
    /// Create a new AI workflow record.
    /// Called at the start of each AI workflow execution.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateAiWorkflowDto dto)
    {
        try
        {
            var result = await _aiWorkflowService.CreateWorkflowAsync(dto);
            return CreatedAtAction(nameof(GetWorkflow), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to create AI workflow",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get a workflow by ID with the full execution trace.
    /// Returns nested steps, tool calls, and validation results.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWorkflow(Guid id)
    {
        try
        {
            var result = await _aiWorkflowService.GetWorkflowAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "AI Workflow Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Update the workflow status.
    /// Valid transitions: Running -> PendingManagerApproval | Completed | SafeFailure.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateWorkflowStatus(
        Guid id, [FromBody] UpdateAiWorkflowStatusDto dto)
    {
        try
        {
            var result = await _aiWorkflowService.UpdateWorkflowStatusAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "AI Workflow Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Add a step to an existing workflow.
    /// Each agent invocation creates one step.
    /// </summary>
    [HttpPost("{id:guid}/steps")]
    public async Task<IActionResult> AddStep(
        Guid id, [FromBody] CreateAiWorkflowStepDto dto)
    {
        try
        {
            var result = await _aiWorkflowService.AddStepAsync(id, dto);
            return Created($"api/v1/ai/workflows/{id}/steps/{result.Id}", result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "AI Workflow Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Add a tool call record to an existing step.
    /// </summary>
    [HttpPost("steps/{stepId:guid}/tool-calls")]
    public async Task<IActionResult> AddToolCall(
        Guid stepId, [FromBody] CreateAiToolCallDto dto)
    {
        try
        {
            var result = await _aiWorkflowService.AddToolCallAsync(stepId, dto);
            return Created($"api/v1/ai/workflows/steps/{stepId}/tool-calls/{result.Id}", result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "AI Workflow Step Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Add a validation result to an existing step.
    /// </summary>
    [HttpPost("steps/{stepId:guid}/validations")]
    public async Task<IActionResult> AddValidationResult(
        Guid stepId, [FromBody] CreateAiValidationResultDto dto)
    {
        try
        {
            var result = await _aiWorkflowService.AddValidationResultAsync(stepId, dto);
            return Created($"api/v1/ai/workflows/steps/{stepId}/validations/{result.Id}", result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "AI Workflow Step Not Found",
                Detail = ex.Message
            });
        }
    }
}
