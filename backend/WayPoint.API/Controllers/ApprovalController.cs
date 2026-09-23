using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.API.Controllers;

/// <summary>
/// Transport Manager approval workbench — pending approval queue and decision submission.
/// All endpoints require TransportManager or Admin role (BR-APPROVAL-001, BR-APPROVAL-002).
/// </summary>
[ApiController]
[Route("api/v1/approvals")]
[Authorize(Policy = "RequireManager")]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    /// <summary>
    /// Retrieve all rebooking proposals currently in PendingManagerApproval status
    /// with associated disruption context.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals()
    {
        var result = await _approvalService.GetPendingApprovalsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Submit a Transport Manager decision on a pending rebooking proposal.
    /// Creates an immutable ApprovalDecision record (BR-APPROVAL-002).
    /// Decision types: Approve, Reject, RequestRevision.
    /// </summary>
    [HttpPost("{id:guid}/decision")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitDecision(Guid id, [FromBody] ApprovalDecisionRequestDto dto)
    {
        var managerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(managerIdClaim) || !Guid.TryParse(managerIdClaim, out var managerId))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication Error",
                Detail = "Unable to determine manager identity from token."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Comments))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Comments are required when submitting an approval decision."
            });
        }

        try
        {
            var result = await _approvalService.SubmitDecisionAsync(id, managerId, dto);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Decision Error",
                Detail = ex.Message
            });
        }
    }
}
