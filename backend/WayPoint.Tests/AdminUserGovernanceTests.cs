using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WayPoint.API.Controllers;
using WayPoint.Application.DTOs.Admin;
using WayPoint.Domain.Entities.Audit;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class AdminUserGovernanceTests
{
    private WayPointDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: "AdminGovDb_" + Guid.NewGuid())
            .Options;

        return new WayPointDbContext(options);
    }

    private (UserController Controller, WayPointDbContext Context, User AdminUser, User SecondAdminUser, User PassengerUser) SetupController(WayPointDbContext context)
    {
        var passwordHasher = new PasswordHasher();
        var loggerMock = new Mock<ILogger<UserController>>();

        var adminRole = new Role { Id = Guid.NewGuid(), RoleName = "Admin", Description = "Admin role" };
        var operatorRole = new Role { Id = Guid.NewGuid(), RoleName = "Operator", Description = "Operator role" };
        var managerRole = new Role { Id = Guid.NewGuid(), RoleName = "TransportManager", Description = "Manager role" };
        var passengerRole = new Role { Id = Guid.NewGuid(), RoleName = "Passenger", Description = "Passenger role" };

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "leadadmin@waypoint.lk",
            FullName = "Lead Administrator",
            PasswordHash = passwordHasher.HashPassword("AdminPass123!"),
            RoleId = adminRole.Id,
            Role = adminRole,
            IsActive = true
        };

        var secondAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = "backupadmin@waypoint.lk",
            FullName = "Backup Administrator",
            PasswordHash = passwordHasher.HashPassword("AdminPass123!"),
            RoleId = adminRole.Id,
            Role = adminRole,
            IsActive = true
        };

        var passengerUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "nimal@waypoint.lk",
            FullName = "Nimal Passenger",
            PasswordHash = passwordHasher.HashPassword("Pass123!"),
            RoleId = passengerRole.Id,
            Role = passengerRole,
            IsActive = true,
            FailedLoginAttempts = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(15)
        };

        context.Roles.AddRange(adminRole, operatorRole, managerRole, passengerRole);
        context.Users.AddRange(adminUser, secondAdmin, passengerUser);
        context.SaveChanges();

        var controller = new UserController(context, passwordHasher, loggerMock.Object);

        // Mock HttpContext with Admin ClaimsPrincipal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
            new(ClaimTypes.Email, adminUser.Email),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return (controller, context, adminUser, secondAdmin, passengerUser);
    }

    [Fact]
    public async Task GetUsers_ReturnsPaginatedUsers_WhenAdmin()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, _, _, _) = SetupController(context);

        // Act
        var result = await controller.GetUsers(search: null, role: null, isActive: null, isLocked: null, page: 1, pageSize: 10, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsAssignableFrom<WayPoint.Application.DTOs.Common.PaginatedResponseDto<AdminUserListItemDto>>(okResult.Value);
        Assert.True(pagedResult.TotalCount >= 3);
        Assert.Contains(pagedResult.Items, u => u.Email == "leadadmin@waypoint.lk");
    }

    [Fact]
    public async Task UpdateUserRole_PromotesToOperator_CreatesOperatorProfile()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, _, _, passenger) = SetupController(context);

        var dto = new UpdateUserRoleRequestDto
        {
            Role = "Operator",
            Reason = "Assigned as Galle station dispatcher",
            OperatorCode = "OP-GALLE-01",
            CompanyName = "Southern Express",
            AssignedRegion = "Southern Province"
        };

        // Act
        var result = await controller.UpdateUserRole(passenger.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminUserListItemDto>(okResult.Value);
        Assert.Equal("Operator", updated.Role);

        // Verify OperatorProfile created in DB
        var profile = await context.OperatorProfiles.FirstOrDefaultAsync(p => p.UserId == passenger.Id);
        Assert.NotNull(profile);
        Assert.Equal("OP-GALLE-01", profile.OperatorCode);
    }

    [Fact]
    public async Task UpdateUserRole_SelfDemotion_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, admin, _, _) = SetupController(context);

        var dto = new UpdateUserRoleRequestDto
        {
            Role = "Passenger",
            Reason = "Accidental self-demotion attempt"
        };

        // Act
        var result = await controller.UpdateUserRole(admin.Id, dto, CancellationToken.None);

        // Assert: BR-ADMIN-002 blocks self-demotion
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("demote", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserRole_SoleAdminDemotion_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, admin, secondAdmin, _) = SetupController(context);

        // Remove the second admin so only one remains
        context.Users.Remove(secondAdmin);
        await context.SaveChangesAsync();

        // Switch caller to a different user context (e.g. system console) to test sole admin check specifically
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, "Admin")
        };
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var dto = new UpdateUserRoleRequestDto
        {
            Role = "TransportManager",
            Reason = "Attempt to demote last remaining admin"
        };

        // Act
        var result = await controller.UpdateUserRole(admin.Id, dto, CancellationToken.None);

        // Assert: BR-ADMIN-001 blocks demoting sole active admin
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("last remaining active Administrator", problem.Detail);
    }

    [Fact]
    public async Task UpdateUserStatus_UnlockAccount_ResetsFailedAttemptsAndLockout()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, _, _, passenger) = SetupController(context);
        Assert.True(passenger.FailedLoginAttempts > 0);
        Assert.NotNull(passenger.LockedUntil);

        var dto = new UpdateUserStatusRequestDto
        {
            UnlockAccount = true,
            Reason = "User called helpdesk and verified NIC"
        };

        // Act
        var result = await controller.UpdateUserStatus(passenger.Id, dto, CancellationToken.None);

        // Assert: BR-AUTH-002 unlock verification
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminUserListItemDto>(okResult.Value);
        Assert.False(updated.IsLocked);
        Assert.Equal(0, updated.FailedLoginAttempts);

        var dbUser = await context.Users.FindAsync(passenger.Id);
        Assert.NotNull(dbUser);
        Assert.Equal(0, dbUser.FailedLoginAttempts);
        Assert.Null(dbUser.LockedUntil);
    }

    [Fact]
    public async Task UpdateUserRole_LogsImmutableAuditRecord()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, admin, _, passenger) = SetupController(context);

        var dto = new UpdateUserRoleRequestDto
        {
            Role = "TransportManager",
            Reason = "Promotion approved by DG Transport"
        };

        // Act
        await controller.UpdateUserRole(passenger.Id, dto, CancellationToken.None);

        // Assert: BR-AUDIT-001 audit record written
        var audit = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == passenger.Id.ToString() && a.ActionType == "ROLE_CHANGE");

        Assert.NotNull(audit);
        Assert.Equal(admin.Id.ToString(), audit.ActorId);
        Assert.Contains("TransportManager", audit.AfterStateJson);
        Assert.Contains("Promotion approved by DG Transport", audit.AfterStateJson);
    }
}
