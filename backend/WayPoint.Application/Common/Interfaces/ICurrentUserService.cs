using System.Security.Claims;

namespace WayPoint.Application.Common.Interfaces;

/// <summary>
/// Provides access to the authenticated user's identity and roles derived from trusted JWT claims.
/// Used across application and service boundaries to enforce Object Level Authorization.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsStaff { get; }
    bool IsAdmin { get; }
    bool IsPassenger { get; }
    bool IsInRole(string role);
}
