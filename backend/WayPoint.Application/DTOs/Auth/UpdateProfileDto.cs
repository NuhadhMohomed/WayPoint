namespace WayPoint.Application.DTOs.Auth;

/// <summary>
/// Request DTO for updating the authenticated user's profile (API §4.2).
/// </summary>
public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}
