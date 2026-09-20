using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Domain.Entities.Ai;
using WayPoint.Domain.Enums;

namespace WayPoint.API.Controllers;

/// <summary>
/// AI Workflow State Persistence Controller.
/// Supports the Python AI microservice in persisting workflow execution traces
/// to PostgreSQL via the authoritative ASP.NET Core API (ADR-004).
/// Tables: AiWorkflows, AiWorkflowSteps, AiToolCalls, AiValidationResults.
/// </summary>
[ApiController]
[Route("api/v1/ai")]
public sealed class AiWorkflowController : ControllerBase
{
    private readonly IWayPointDbContext context;

    public AiWorkflowController(IWayPointDbContext context)
    {
        this.context = context;
    }

    // ── POST /api/v1/ai/workflows ────────────────────────────────────────────

    [Authorize(Roles = "Operator,TransportManager,Admin")]
    [HttpPost("workflows")]
    public async Task<ActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Objective))
            return BadRequest(new ProblemDetails { Title = "Invalid workflow", Detail = "Objective is required.", Status = StatusCodes.Status400BadRequest });

        var workflow = new AiWorkflow
        {
            Objective = request.Objective.Trim(),
            Status = AiWorkflowStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        await context.AiWorkflows.AddAsync(workflow, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new
        {
            workflow.Id,
            workflow.Objective,
            Status = workflow.Status.ToString(),
            workflow.StartedAt
        });
    }

    // ── POST /api/v1/ai/workflows/{workflowId}/steps ─────────────────────────

    [Authorize(Roles = "Operator,TransportManager,Admin")]
    [HttpPost("workflows/{workflowId:guid}/steps")]
    public async Task<ActionResult> LogStep(Guid workflowId, [FromBody] LogStepRequest request, CancellationToken cancellationToken)
    {
        var workflow = await context.AiWorkflows.FindAsync(new object[] { workflowId }, cancellationToken);
        if (workflow is null)
            return NotFound(new ProblemDetails { Title = "Workflow not found", Detail = $"No workflow with ID {workflowId}.", Status = StatusCodes.Status404NotFound });

        var step = new AiWorkflowStep
        {
            AiWorkflowId = workflowId,
            AgentName = request.AgentName?.Trim() ?? string.Empty,
            StepOrder = request.StepOrder,
            StepDescription = request.StepDescription?.Trim() ?? string.Empty,
            ExecutedAt = DateTime.UtcNow
        };

        await context.AiWorkflowSteps.AddAsync(step, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new
        {
            step.Id,
            step.AgentName,
            step.StepOrder,
            step.ExecutedAt
        });
    }

    // ── POST /api/v1/ai/workflow-steps/{stepId}/tool-calls ───────────────────

    [Authorize(Roles = "Operator,TransportManager,Admin")]
    [HttpPost("workflow-steps/{stepId:guid}/tool-calls")]
    public async Task<ActionResult> LogToolCall(Guid stepId, [FromBody] LogToolCallRequest request, CancellationToken cancellationToken)
    {
        var step = await context.AiWorkflowSteps.FindAsync(new object[] { stepId }, cancellationToken);
        if (step is null)
            return NotFound(new ProblemDetails { Title = "Step not found", Detail = $"No workflow step with ID {stepId}.", Status = StatusCodes.Status404NotFound });

        var toolCall = new AiToolCall
        {
            AiWorkflowStepId = stepId,
            ToolName = request.ToolName?.Trim() ?? string.Empty,
            ArgumentsJson = request.ArgumentsJson ?? "{}",
            ResultJson = request.ResultJson ?? "{}",
            DurationMs = request.DurationMs,
            ExecutedAt = DateTime.UtcNow
        };

        await context.AiToolCalls.AddAsync(toolCall, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { toolCall.Id, toolCall.ToolName, toolCall.DurationMs });
    }

    // ── POST /api/v1/ai/workflow-steps/{stepId}/validations ──────────────────

    [Authorize(Roles = "Operator,TransportManager,Admin")]
    [HttpPost("workflow-steps/{stepId:guid}/validations")]
    public async Task<ActionResult> LogValidation(Guid stepId, [FromBody] LogValidationRequest request, CancellationToken cancellationToken)
    {
        var step = await context.AiWorkflowSteps.FindAsync(new object[] { stepId }, cancellationToken);
        if (step is null)
            return NotFound(new ProblemDetails { Title = "Step not found", Detail = $"No workflow step with ID {stepId}.", Status = StatusCodes.Status404NotFound });

        var validation = new AiValidationResult
        {
            AiWorkflowStepId = stepId,
            RuleName = request.RuleName?.Trim() ?? string.Empty,
            Passed = request.Passed,
            ValidationDetails = request.ValidationDetails?.Trim()
        };

        await context.AiValidationResults.AddAsync(validation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { validation.Id, validation.RuleName, validation.Passed });
    }

    // ── PATCH /api/v1/ai/workflows/{workflowId}/complete ─────────────────────

    [Authorize(Roles = "Operator,TransportManager,Admin")]
    [HttpPatch("workflows/{workflowId:guid}/complete")]
    public async Task<ActionResult> CompleteWorkflow(Guid workflowId, [FromBody] CompleteWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = await context.AiWorkflows.FindAsync(new object[] { workflowId }, cancellationToken);
        if (workflow is null)
            return NotFound(new ProblemDetails { Title = "Workflow not found", Detail = $"No workflow with ID {workflowId}.", Status = StatusCodes.Status404NotFound });

        if (!Enum.TryParse<AiWorkflowStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            return BadRequest(new ProblemDetails { Title = "Invalid status", Detail = $"'{request.Status}' is not a valid workflow status. Use: Completed, SafeFailure.", Status = StatusCodes.Status400BadRequest });

        workflow.Status = parsedStatus;
        workflow.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { workflow.Id, Status = workflow.Status.ToString(), workflow.CompletedAt });
    }

    // ── GET /api/v1/ai/workflows/{workflowId} ────────────────────────────────

    [Authorize(Roles = "Operator,TransportManager,Admin")]
    [HttpGet("workflows/{workflowId:guid}")]
    public async Task<ActionResult> GetWorkflowTrace(Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await context.AiWorkflows
            .AsNoTracking()
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ValidationResults)
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        if (workflow is null)
            return NotFound(new ProblemDetails { Title = "Workflow not found", Detail = $"No workflow with ID {workflowId}.", Status = StatusCodes.Status404NotFound });

        return Ok(new
        {
            workflow.Id,
            workflow.Objective,
            Status = workflow.Status.ToString(),
            workflow.StartedAt,
            workflow.CompletedAt,
            Steps = workflow.Steps.Select(step => new
            {
                step.Id,
                step.AgentName,
                step.StepOrder,
                step.StepDescription,
                step.ExecutedAt,
                ToolCalls = step.ToolCalls.Select(tc => new
                {
                    tc.Id,
                    tc.ToolName,
                    tc.ArgumentsJson,
                    tc.ResultJson,
                    tc.DurationMs,
                    tc.ExecutedAt
                }),
                ValidationResults = step.ValidationResults.Select(vr => new
                {
                    vr.Id,
                    vr.RuleName,
                    vr.Passed,
                    vr.ValidationDetails
                })
            })
        });
    }
}

// ── Request DTOs (self-contained) ────────────────────────────────────────────

public sealed record CreateWorkflowRequest(string Objective);
public sealed record LogStepRequest(string AgentName, int StepOrder, string StepDescription);
public sealed record LogToolCallRequest(string ToolName, string ArgumentsJson, string ResultJson, int DurationMs);
public sealed record LogValidationRequest(string RuleName, bool Passed, string? ValidationDetails);
public sealed record CompleteWorkflowRequest(string Status);

