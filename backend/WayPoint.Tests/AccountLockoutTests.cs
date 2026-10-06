using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WayPoint.API.Controllers;
using WayPoint.Application.DTOs.Auth;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Infrastructure.Data;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class AccountLockoutTests
{
    private WayPointDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WayPointDbContext>()
            .UseInMemoryDatabase(databaseName: "LockoutDb_" + Guid.NewGuid())
            .Options;

        return new WayPointDbContext(options);
    }

    private (AuthController Controller, WayPointDbContext Context, User TestUser) SetupController(WayPointDbContext context)
    {
        var passwordHasher = new PasswordHasher();
        var jwtSettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090"},
            {"Jwt:Issuer", "WayPoint"},
            {"Jwt:Audience", "WayPointClients"},
            {"Jwt:ExpiryMinutes", "60"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(jwtSettings)
            .Build();
        var jwtService = new JwtTokenService(configuration);

        var role = new Role { Id = Guid.NewGuid(), RoleName = "Passenger", Description = "Passenger role" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "passenger@waypoint.lk",
            FullName = "Test Passenger",
            PasswordHash = passwordHasher.HashPassword("CorrectPassword123!"),
            RoleId = role.Id,
            Role = role,
            IsActive = true,
            FailedLoginAttempts = 0
        };

        context.Roles.Add(role);
        context.Users.Add(user);
        context.SaveChanges();

        var controller = new AuthController(context, passwordHasher, jwtService);
        return (controller, context, user);
    }

    [Fact]
    public async Task Login_WrongPassword_IncrementsFailedLoginAttempts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, user) = SetupController(context);

        // Act
        var result = await controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "WrongPassword!"
        });

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);

        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.FailedLoginAttempts);
        Assert.Null(updatedUser.LockedUntil);
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccountFor15Minutes()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, user) = SetupController(context);

        // Act — 5 consecutive failures
        for (int i = 0; i < 5; i++)
        {
            await controller.Login(new LoginRequestDto
            {
                Email = user.Email,
                Password = "WrongPassword!"
            });
        }

        // Assert
        var lockedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(lockedUser);
        Assert.NotNull(lockedUser.LockedUntil);
        Assert.True(lockedUser.LockedUntil.Value > DateTime.UtcNow.AddMinutes(14));
        Assert.Equal(0, lockedUser.FailedLoginAttempts); // Reset count after lock
    }

    [Fact]
    public async Task Login_WhenAccountIsLocked_ReturnsStatus423Locked()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, user) = SetupController(context);

        user.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "CorrectPassword123!" // Even with correct password
        });

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status423Locked, objectResult.StatusCode);
    }

    [Fact]
    public async Task Login_SuccessfulLogin_ResetsFailedLoginAttempts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (controller, _, user) = SetupController(context);

        user.FailedLoginAttempts = 3;
        await context.SaveChangesAsync();

        // Act
        var result = await controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "CorrectPassword123!"
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var refreshedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(refreshedUser);
        Assert.Equal(0, refreshedUser.FailedLoginAttempts);
        Assert.Null(refreshedUser.LockedUntil);
    }
}
