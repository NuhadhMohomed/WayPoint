using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.API.Controllers;

/// <summary>
/// Public service alerts — passengers can view active alerts, operators can broadcast new ones.
/// </summary>
[ApiController]
[Route("api/v1/alerts")]
public class ServiceAlertController : ControllerBase
{
    private readonly IServiceAlertService _serviceAlertService;

    public ServiceAlertController(IServiceAlertService serviceAlertService)
    {
        _serviceAlertService = serviceAlertService;
    }

    /// <summary>
    /// List all active public service alerts. No authentication required.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveAlerts()
    {
        var result = await _serviceAlertService.GetActiveAlertsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Broadcast a new public service alert linked to a specific service.
    /// Requires Operator role.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BroadcastAlert([FromBody] CreateServiceAlertDto dto)
    {
        if (dto.ServiceId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "ServiceId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Alert title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Alert message is required."
            });
        }

        try
        {
            var result = await _serviceAlertService.BroadcastAlertAsync(dto);
            return CreatedAtAction(nameof(GetActiveAlerts), null, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Service Not Found",
                Detail = ex.Message
            });
        }
    }
}
