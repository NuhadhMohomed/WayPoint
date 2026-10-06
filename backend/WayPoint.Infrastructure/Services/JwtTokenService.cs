using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Domain.Entities.Identity;

namespace WayPoint.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, string roleName)
    {
        var secretEnv = Environment.GetEnvironmentVariable("JWT_SECRET");
        var secretConfig = _configuration["Jwt:Secret"];
        var secret = !string.IsNullOrWhiteSpace(secretEnv) ? secretEnv
            : !string.IsNullOrWhiteSpace(secretConfig) ? secretConfig
            : throw new InvalidOperationException(
                "JWT signing secret is not configured. Set JWT_SECRET environment variable or Jwt:Secret in appsettings.");
        var issuer = _configuration["Jwt:Issuer"] ?? "WayPoint";
        var audience = _configuration["Jwt:Audience"] ?? "WayPointClients";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 120;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, roleName),
            new("role", roleName),
            new("PassengerId", user.PassengerProfile?.Id.ToString() ?? user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
