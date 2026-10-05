namespace WayPoint.Application.DTOs.Admin;

/// <summary>
/// Detailed user listing item returned to administrative clients.
/// </summary>
public class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ProfileMetadata { get; set; }
}

/// <summary>
/// Request payload to change a user's role.
/// </summary>
public class UpdateUserRoleRequestDto
{
    public string Role { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? OperatorCode { get; set; }
    public string? CompanyName { get; set; }
    public string? AssignedRegion { get; set; }
}

/// <summary>
/// Request payload to toggle user status or clear account lockout.
/// </summary>
public class UpdateUserStatusRequestDto
{
    public bool? IsActive { get; set; }
    public bool UnlockAccount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for direct administrative user provisioning.
/// </summary>
public class AdminCreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Passenger";
    public string? OperatorCode { get; set; }
    public string? CompanyName { get; set; }
    public string? AssignedRegion { get; set; }
    public string? NicOrPassport { get; set; }
}

/// <summary>
/// Summary of roles with associated metadata and active member counts.
/// </summary>
public class RoleSummaryDto
{
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UserCount { get; set; }
}
