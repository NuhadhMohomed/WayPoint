using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.API.Controllers;

/// <summary>
/// Manages rebooking proposal creation, retrieval, and transactional execution.
/// </summary>
[ApiController]
[Route("api/v1/rebooking")]
[Authorize(Policy = "RequireOperator")]
public class RebookingController : ControllerBase
{
    private readonly IRebookingService _rebookingService;

    public RebookingController(IRebookingService rebookingService)
    {
        _rebookingService = rebookingService;
    }

    /// <summary>
    /// Create a rebooking proposal (stub — no AI). Links a disruption case to a replacement service.
    /// Sets initial status based on impact severity classification (BR-APPROVAL-001).
    /// </summary>
    [HttpPost("generate-proposal")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateProposal([FromBody] CreateRebookingProposalDto dto)
    {
        if (dto.DisruptionCaseId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "DisruptionCaseId is required."
            });
        }

        if (dto.ReplacementServiceId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "ReplacementServiceId is required."
            });
        }

        try
        {
            var result = await _rebookingService.CreateProposalAsync(dto);
            return CreatedAtAction(nameof(GetProposalsByDisruption),
                new { disruptionId = result.DisruptionCaseId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Proposal Conflict",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// List all rebooking proposals for a given disruption case.
    /// </summary>
    [HttpGet("disruption/{disruptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProposalsByDisruption(Guid disruptionId)
    {
        var result = await _rebookingService.GetProposalsByDisruptionAsync(disruptionId);
        return Ok(result);
    }

    /// <summary>
    /// Execute an approved rebooking proposal transactionally (BR-REBOOK-001, BR-APPLY-002).
    /// Requires Transport Manager or Admin role.
    /// Transfers bookings, releases old seats, locks new seats, re-issues tickets,
    /// and enforces fare protection guarantee (BR-REBOOK-002).
    /// </summary>
    [HttpPost("{id:guid}/execute")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteRebooking(Guid id)
    {
        try
        {
            var result = await _rebookingService.ExecuteApprovedRebookingAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Proposal Not Found",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Approval Required",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Rebooking Execution Failed",
                Detail = ex.Message
            });
        }
    }
}
