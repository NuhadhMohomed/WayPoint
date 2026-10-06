using WayPoint.Application.DTOs.Notification;

namespace WayPoint.Application.Common.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(Guid userId, string title, string message, string type = "Info", string? referenceEntityType = null, Guid? referenceEntityId = null, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
}
