using WayPoint.Domain.Common;
using WayPoint.Domain.Enums;

namespace WayPoint.Domain.Entities.Identity;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Role Role { get; set; } = null!;
    public OperatorProfile? OperatorProfile { get; set; }
    public PassengerProfile? PassengerProfile { get; set; }
}

public class Role : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class OperatorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? AssignedRegion { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}

public class PassengerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string? NicOrPassport { get; set; }
    public string? EmergencyContact { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
