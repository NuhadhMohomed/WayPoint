using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WayPoint.Application.Common.Interfaces;

namespace WayPoint.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService reading identity claims from the ambient HttpContext.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true && UserId.HasValue;

    public bool IsStaff => Role is "Admin" or "TransportManager" or "Operator" or "Conductor";

    public bool IsAdmin => Role == "Admin";

    public bool IsPassenger => Role == "Passenger";

    public bool IsInRole(string role) => User?.IsInRole(role) == true;
}
