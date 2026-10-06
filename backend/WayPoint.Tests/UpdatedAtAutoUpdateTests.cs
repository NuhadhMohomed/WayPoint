using Microsoft.EntityFrameworkCore;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Infrastructure.Data;
using Xunit;

namespace WayPoint.Tests;

public class UpdatedAtAutoUpdateTests
{
    private WayPointDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: "UpdatedAtDb_" + Guid.NewGuid())
            .Options;

        return new WayPointDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_OnModifiedEntity_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var initialTime = DateTime.UtcNow.AddMinutes(-10);

        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = "Operator",
            Description = "Operator role",
            CreatedAt = initialTime,
            UpdatedAt = initialTime
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        // Act - Modify entity
        role.RoleName = "SeniorOperator";
        await Task.Delay(10); // Small tick difference
        await context.SaveChangesAsync();

        // Assert
        var savedRole = await context.Roles.FindAsync(role.Id);
        Assert.NotNull(savedRole);
        Assert.Equal("SeniorOperator", savedRole.RoleName);
        Assert.Equal(initialTime, savedRole.CreatedAt); // CreatedAt unchanged
        Assert.True(savedRole.UpdatedAt > initialTime, $"Expected UpdatedAt ({savedRole.UpdatedAt}) > initialTime ({initialTime})");
    }
}
