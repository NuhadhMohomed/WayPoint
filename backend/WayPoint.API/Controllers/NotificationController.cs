using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <summary>
    /// Retrieves notifications for the authenticated user (disruption alerts, booking updates).
    /// </summary>
    /// <remarks>
    /// TODO: Implement notification query service backed by a Notifications table.
    /// Currently returns an empty collection as a contract placeholder.
    /// </remarks>
    [HttpGet("my-notifications")]
    public IActionResult GetMyNotifications()
    {
        // Stub: notification persistence and query service not yet implemented
        return Ok(new { items = new List<object>(), totalCount = 0 });
    }
}
