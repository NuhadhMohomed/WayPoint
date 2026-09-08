using WayPoint.Domain.Entities.Identity;

namespace WayPoint.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user, string roleName);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
