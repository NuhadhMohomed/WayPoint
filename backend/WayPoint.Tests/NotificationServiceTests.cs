using Microsoft.EntityFrameworkCore;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class NotificationServiceTests
{
    private WayPointDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: "NotificationDb_" + Guid.NewGuid())
            .Options;

        return new WayPointDbContext(options);
    }

    [Fact]
    public async Task CreateNotificationAsync_PersistsNotificationSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new NotificationService(context);
        var userId = Guid.NewGuid();

        // Act
        var created = await service.CreateNotificationAsync(
            userId,
            "Delay Alert",
            "Service EX-01 is delayed by 20 minutes due to weather.",
            "DisruptionAlert",
            "Service",
            Guid.NewGuid()
        );

        // Assert
        Assert.NotNull(created);
        Assert.Equal(userId, created.UserId);
        Assert.Equal("Delay Alert", created.Title);
        Assert.Equal("DisruptionAlert", created.Type);
        Assert.False(created.IsRead);
        Assert.Null(created.ReadAt);

        var inDb = await context.Notifications.FirstOrDefaultAsync(n => n.Id == created.Id);
        Assert.NotNull(inDb);
        Assert.Equal("Delay Alert", inDb.Title);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ReturnsOnlyUserNotificationsInDescendingOrder()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new NotificationService(context);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await service.CreateNotificationAsync(user1, "Alert 1", "Message 1");
        await Task.Delay(10);
        await service.CreateNotificationAsync(user1, "Alert 2", "Message 2");
        await service.CreateNotificationAsync(user2, "Other User Alert", "Other Message");

        // Act
        var user1Notifications = (await service.GetUserNotificationsAsync(user1)).ToList();

        // Assert
        Assert.Equal(2, user1Notifications.Count);
        Assert.Equal("Alert 2", user1Notifications[0].Title); // newest first
        Assert.Equal("Alert 1", user1Notifications[1].Title);
    }

    [Fact]
    public async Task MarkAsReadAsync_UpdatesIsReadAndReadAt_WhenAuthorized()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new NotificationService(context);
        var userId = Guid.NewGuid();

        var notification = await service.CreateNotificationAsync(userId, "Boarding Notice", "Bus arriving at Gate 3");

        // Act
        var success = await service.MarkAsReadAsync(notification.Id, userId);

        // Assert
        Assert.True(success);
        var updated = await context.Notifications.FindAsync(notification.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsRead);
        Assert.NotNull(updated.ReadAt);
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsFalse_WhenUnauthorizedUserAttempts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new NotificationService(context);
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var notification = await service.CreateNotificationAsync(ownerId, "Private Alert", "Private info");

        // Act
        var success = await service.MarkAsReadAsync(notification.Id, strangerId);

        // Assert
        Assert.False(success);
        var unchanged = await context.Notifications.FindAsync(notification.Id);
        Assert.NotNull(unchanged);
        Assert.False(unchanged.IsRead);
    }
}
