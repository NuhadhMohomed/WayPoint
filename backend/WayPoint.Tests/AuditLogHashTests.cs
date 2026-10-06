using Microsoft.EntityFrameworkCore;
using WayPoint.Domain.Entities.Audit;
using WayPoint.Infrastructure.Data;
using Xunit;

namespace WayPoint.Tests;

public class AuditLogHashTests
{
    [Fact]
    public void ComputeHash_ProducesDeterministic64CharHexSha256()
    {
        // Arrange
        var timestamp = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var log1 = new AuditLog
        {
            Timestamp = timestamp,
            ActorId = "usr-123",
            ActionType = "DISRUPTION_TRIGGERED",
            EntityName = "Service",
            EntityId = "srv-456",
            AfterStateJson = "{\"status\":\"Disrupted\"}"
        };

        var log2 = new AuditLog
        {
            Timestamp = timestamp,
            ActorId = "usr-123",
            ActionType = "DISRUPTION_TRIGGERED",
            EntityName = "Service",
            EntityId = "srv-456",
            AfterStateJson = "{\"status\":\"Disrupted\"}"
        };

        // Act
        log1.ComputeHash();
        log2.ComputeHash();

        // Assert
        Assert.NotNull(log1.HashSha256);
        Assert.Equal(64, log1.HashSha256.Length);
        Assert.Equal(log1.HashSha256, log2.HashSha256);
    }

    [Fact]
    public void ComputeHash_DetectsTamperingInPayload()
    {
        // Arrange
        var timestamp = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var originalLog = new AuditLog
        {
            Timestamp = timestamp,
            ActorId = "usr-123",
            ActionType = "SERVICE_CANCELLED",
            EntityName = "Service",
            EntityId = "srv-789",
            AfterStateJson = "{\"status\":\"Cancelled\"}"
        };
        originalLog.ComputeHash();

        var tamperedLog = new AuditLog
        {
            Timestamp = timestamp,
            ActorId = "usr-999", // Tampered actor
            ActionType = "SERVICE_CANCELLED",
            EntityName = "Service",
            EntityId = "srv-789",
            AfterStateJson = "{\"status\":\"Cancelled\"}"
        };
        tamperedLog.ComputeHash();

        // Assert
        Assert.NotEqual(originalLog.HashSha256, tamperedLog.HashSha256);
    }

    [Fact]
    public async Task SaveChangesAsync_AutomaticallyComputesHashOnAddedAuditLog()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: "AuditLogDb_" + Guid.NewGuid())
            .Options;

        using var context = new WayPointDbContext(options);

        var log = new AuditLog
        {
            ActorId = "admin-1",
            ActionType = "ROUTE_CREATED",
            EntityName = "Route",
            EntityId = "rt-colombo-kandy",
            AfterStateJson = "{\"code\":\"EX-02\"}"
        };

        Assert.Empty(log.HashSha256);

        // Act
        await context.AuditLogs.AddAsync(log);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEmpty(log.HashSha256);
        Assert.Equal(64, log.HashSha256.Length);
    }
}
