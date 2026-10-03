using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces;

namespace WayPoint.API.Controllers;

/// <summary>
/// Passenger notification endpoints (API §4.18, FR-NOTIFY-001).
/// Provides disruption alerts and booking update notifications.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid? GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : null;
    }

    /// <summary>
    /// Retrieves notifications for the authenticated user (disruption alerts, booking updates).
    /// </summary>
    [HttpGet("my-notifications")]
    public async Task<IActionResult> GetMyNotifications(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Valid user ID claim not found in authentication token."
            });
        }

        var items = await _notificationService.GetUserNotificationsAsync(userId.Value, cancellationToken);
        var itemList = items.ToList();

        return Ok(new
        {
            items = itemList,
            totalCount = itemList.Count
        });
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var success = await _notificationService.MarkAsReadAsync(id, userId.Value, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }
}
