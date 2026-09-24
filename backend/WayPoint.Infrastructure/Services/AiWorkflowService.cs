using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Features.AiWorkflows.DTOs;
using WayPoint.Domain.Entities.Ai;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services;

/// <summary>
/// AI Workflow persistence service (FR-AI-005, FR-AI-007, ADR-004).
/// Provides CRUD operations for workflow execution traces stored in PostgreSQL.
/// </summary>
public class AiWorkflowService : IAiWorkflowService
{
    private readonly IWayPointDbContext _db;

    public AiWorkflowService(IWayPointDbContext db)
    {
        _db = db;
    }

    public async Task<AiWorkflowResponseDto> CreateWorkflowAsync(CreateAiWorkflowDto dto)
    {
        var workflow = new AiWorkflow
        {
            Objective = dto.Objective,
            Status = AiWorkflowStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        _db.AiWorkflows.Add(workflow);
        await _db.SaveChangesAsync();

        return MapToResponse(workflow);
    }

    public async Task<AiWorkflowResponseDto> GetWorkflowAsync(Guid workflowId)
    {
        var workflow = await _db.AiWorkflows
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ValidationResults)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workflowId)
            ?? throw new KeyNotFoundException($"AI workflow {workflowId} not found");

        return MapToResponse(workflow);
    }

    public async Task<AiWorkflowResponseDto> UpdateWorkflowStatusAsync(
        Guid workflowId, UpdateAiWorkflowStatusDto dto)
    {
        var workflow = await _db.AiWorkflows
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ValidationResults)
            .FirstOrDefaultAsync(w => w.Id == workflowId)
            ?? throw new KeyNotFoundException($"AI workflow {workflowId} not found");

        workflow.Status = dto.Status;
        workflow.UpdatedAt = DateTime.UtcNow;

        // Set CompletedAt for terminal states
        if (dto.Status is AiWorkflowStatus.Completed or AiWorkflowStatus.SafeFailure)
        {
            workflow.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return MapToResponse(workflow);
    }

    public async Task<AiWorkflowStepResponseDto> AddStepAsync(
        Guid workflowId, CreateAiWorkflowStepDto dto)
    {
        // Validate workflow exists
        var exists = await _db.AiWorkflows.AnyAsync(w => w.Id == workflowId);
        if (!exists)
            throw new KeyNotFoundException($"AI workflow {workflowId} not found");

        var step = new AiWorkflowStep
        {
            AiWorkflowId = workflowId,
            AgentName = dto.AgentName,
            StepOrder = dto.StepOrder,
            StepDescription = dto.StepDescription,
            ExecutedAt = DateTime.UtcNow
        };

        _db.AiWorkflowSteps.Add(step);
        await _db.SaveChangesAsync();

        return MapStepToResponse(step);
    }

    public async Task<AiToolCallResponseDto> AddToolCallAsync(
        Guid stepId, CreateAiToolCallDto dto)
    {
        var exists = await _db.AiWorkflowSteps.AnyAsync(s => s.Id == stepId);
        if (!exists)
            throw new KeyNotFoundException($"AI workflow step {stepId} not found");

        var toolCall = new AiToolCall
        {
            AiWorkflowStepId = stepId,
            ToolName = dto.ToolName,
            ArgumentsJson = dto.ArgumentsJson,
            ResultJson = dto.ResultJson,
            DurationMs = dto.DurationMs,
            ExecutedAt = DateTime.UtcNow
        };

        _db.AiToolCalls.Add(toolCall);
        await _db.SaveChangesAsync();

        return new AiToolCallResponseDto(
            toolCall.Id,
            toolCall.AiWorkflowStepId,
            toolCall.ToolName,
            toolCall.ArgumentsJson,
            toolCall.ResultJson,
            toolCall.DurationMs,
            toolCall.ExecutedAt
        );
    }

    public async Task<AiValidationResultResponseDto> AddValidationResultAsync(
        Guid stepId, CreateAiValidationResultDto dto)
    {
        var exists = await _db.AiWorkflowSteps.AnyAsync(s => s.Id == stepId);
        if (!exists)
            throw new KeyNotFoundException($"AI workflow step {stepId} not found");

        var validation = new AiValidationResult
        {
            AiWorkflowStepId = stepId,
            RuleName = dto.RuleName,
            Passed = dto.Passed,
            ValidationDetails = dto.ValidationDetails
        };

        _db.AiValidationResults.Add(validation);
        await _db.SaveChangesAsync();

        return new AiValidationResultResponseDto(
            validation.Id,
            validation.AiWorkflowStepId,
            validation.RuleName,
            validation.Passed,
            validation.ValidationDetails
        );
    }

    // ======================================================================
    // Mapping Helpers
    // ======================================================================

    private static AiWorkflowResponseDto MapToResponse(AiWorkflow workflow)
    {
        return new AiWorkflowResponseDto(
            workflow.Id,
            workflow.Objective,
            workflow.Status,
            workflow.StartedAt,
            workflow.CompletedAt,
            workflow.Steps.OrderBy(s => s.StepOrder).Select(MapStepToResponse).ToList()
        );
    }

    private static AiWorkflowStepResponseDto MapStepToResponse(AiWorkflowStep step)
    {
        return new AiWorkflowStepResponseDto(
            step.Id,
            step.AiWorkflowId,
            step.AgentName,
            step.StepOrder,
            step.StepDescription,
            step.ExecutedAt,
            step.ToolCalls.Select(tc => new AiToolCallResponseDto(
                tc.Id,
                tc.AiWorkflowStepId,
                tc.ToolName,
                tc.ArgumentsJson,
                tc.ResultJson,
                tc.DurationMs,
                tc.ExecutedAt
            )).ToList(),
            step.ValidationResults.Select(vr => new AiValidationResultResponseDto(
                vr.Id,
                vr.AiWorkflowStepId,
                vr.RuleName,
                vr.Passed,
                vr.ValidationDetails
            )).ToList()
        );
    }
}
