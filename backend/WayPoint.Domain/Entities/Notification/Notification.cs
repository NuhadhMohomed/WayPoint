using WayPoint.Domain.Common;

namespace WayPoint.Domain.Entities.Notification;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, DisruptionAlert, BookingUpdate, ApprovalRequired
    public string? ReferenceEntityType { get; set; }
    public Guid? ReferenceEntityId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
