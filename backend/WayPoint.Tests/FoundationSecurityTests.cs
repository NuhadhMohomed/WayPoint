using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Infrastructure;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class FoundationSecurityTests
{
    [Fact]
    public void PasswordHasher_ShouldHashAndVerifyPasswordCorrectly()
    {
        // Arrange
        var hasher = new PasswordHasher();
        const string rawPassword = "SecurePassword2026!";

        // Act
        var hash = hasher.HashPassword(rawPassword);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEqual(rawPassword, hash);
        Assert.True(hasher.VerifyPassword(rawPassword, hash));
        Assert.False(hasher.VerifyPassword("WrongPassword", hash));
    }

    [Fact]
    public void JwtTokenService_ShouldGenerateValidTokenWithClaims()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090"},
            {"Jwt:Issuer", "WayPoint"},
            {"Jwt:Audience", "WayPointClients"},
            {"Jwt:ExpiryMinutes", "60"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var jwtService = new JwtTokenService(configuration);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "manager@waypoint.lk",
            FullName = "Test Transport Manager"
        };

        // Act
        var tokenString = jwtService.GenerateToken(user, "TransportManager");

        // Assert
        Assert.NotNull(tokenString);
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(tokenString));

        var jwt = handler.ReadJwtToken(tokenString);
        Assert.Equal("WayPoint", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == "manager@waypoint.lk");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "TransportManager");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
    }

    [Fact]
    public void ConnectionStringResolver_ShouldParseRailwayPostgresUrl()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DATABASE_URL", "postgresql://railwayuser:railwaypass@viaduct.proxy.rlwy.net:45678/railway"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var resolved = DependencyInjection.ResolveConnectionString(configuration);

        // Assert
        Assert.Contains("Host=viaduct.proxy.rlwy.net", resolved);
        Assert.Contains("Port=45678", resolved);
        Assert.Contains("Username=railwayuser", resolved);
        Assert.Contains("Database=railway", resolved);
    }
}
