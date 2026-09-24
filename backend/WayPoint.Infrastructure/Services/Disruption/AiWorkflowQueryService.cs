using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.DisruptionManagement.DTOs;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services.Disruption;

/// <summary>
/// Read-only query service for AI workflow observability and audit inspection (FR-AI-007).
/// Returns workflow execution traces with steps, tool calls, and validation results.
/// </summary>
public class AiWorkflowQueryService : IAiWorkflowQueryService
{
    private readonly IWayPointDbContext _context;

    public AiWorkflowQueryService(IWayPointDbContext context)
    {
        _context = context;
    }

    public async Task<AiWorkflowDto> GetWorkflowByIdAsync(Guid workflowId)
    {
        var workflow = await _context.AiWorkflows
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ValidationResults)
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        if (workflow == null)
            throw new KeyNotFoundException($"AI Workflow with ID '{workflowId}' was not found.");

        return MapToDto(workflow);
    }

    public async Task<PaginatedResponseDto<AiWorkflowDto>> GetWorkflowsAsync(WorkflowFilterParams filter)
    {
        var query = _context.AiWorkflows.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(w => w.Status == filter.Status.Value);

        var totalCount = await query.CountAsync();

        var workflows = await query
            .OrderByDescending(w => w.StartedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ValidationResults)
            .ToListAsync();

        var items = workflows.Select(MapToDto).ToList();

        return new PaginatedResponseDto<AiWorkflowDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    private static AiWorkflowDto MapToDto(Domain.Entities.Ai.AiWorkflow workflow)
    {
        var totalDurationMs = 0;
        if (workflow.CompletedAt.HasValue)
        {
            totalDurationMs = (int)(workflow.CompletedAt.Value - workflow.StartedAt).TotalMilliseconds;
        }

        return new AiWorkflowDto
        {
            Id = workflow.Id,
            Objective = workflow.Objective,
            Status = workflow.Status,
            StartedAt = workflow.StartedAt,
            CompletedAt = workflow.CompletedAt,
            TotalDurationMs = totalDurationMs,
            Steps = workflow.Steps
                .OrderBy(s => s.StepOrder)
                .Select(s => new AiWorkflowStepDto
                {
                    Id = s.Id,
                    AgentName = s.AgentName,
                    StepOrder = s.StepOrder,
                    StepDescription = s.StepDescription,
                    ExecutedAt = s.ExecutedAt,
                    ToolCalls = s.ToolCalls
                        .OrderBy(tc => tc.ExecutedAt)
                        .Select(tc => new AiToolCallDto
                        {
                            Id = tc.Id,
                            ToolName = tc.ToolName,
                            ArgumentsJson = tc.ArgumentsJson,
                            ResultJson = tc.ResultJson,
                            DurationMs = tc.DurationMs,
                            ExecutedAt = tc.ExecutedAt
                        })
                        .ToList(),
                    ValidationResults = s.ValidationResults
                        .Select(vr => new AiValidationResultDto
                        {
                            Id = vr.Id,
                            RuleName = vr.RuleName,
                            Passed = vr.Passed,
                            ValidationDetails = vr.ValidationDetails
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
