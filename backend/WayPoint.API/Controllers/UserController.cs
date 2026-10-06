using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Admin;
using WayPoint.Application.DTOs.Auth;
using WayPoint.Application.DTOs.Common;
using WayPoint.Domain.Entities.Audit;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Services;

namespace WayPoint.API.Controllers;

/// <summary>
/// User profile and administrative user governance endpoints (API §4.2 & §4.3).
/// Provides authenticated user profile self-service and comprehensive administrator governance.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IWayPointDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserController>? _logger;

    public UserController(
        IWayPointDbContext context,
        IPasswordHasher? passwordHasher = null,
        ILogger<UserController>? logger = null)
    {
        _context = context;
        _passwordHasher = passwordHasher ?? new PasswordHasher();
        _logger = logger;
    }

    // =========================================================================
    // 1. Self-Service Endpoints (Passenger, Operator, Manager, Admin)
    // =========================================================================

    /// <summary>
    /// Retrieves the authenticated user's profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User Not Found",
                Detail = "The authenticated user profile could not be found."
            });
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.RoleName
        });
    }

    /// <summary>
    /// Updates the authenticated user's profile fields.
    /// Only non-null fields in the request body are applied.
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        [FromBody] UpdateProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User Not Found",
                Detail = "The authenticated user profile could not be found."
            });
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            user.FullName = dto.FullName.Trim();
        }

        if (dto.PhoneNumber != null)
        {
            user.PhoneNumber = dto.PhoneNumber.Trim();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.RoleName
        });
    }

    // =========================================================================
    // 2. Administrative User & Role Governance Endpoints (Admin Only)
    // =========================================================================

    /// <summary>
    /// Retrieves a paginated list of system users with optional filtering.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<PaginatedResponseDto<AdminUserListItemDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isLocked,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.OperatorProfile)
            .Include(u => u.PassengerProfile)
            .AsQueryable();

        // 1. Text search (Email, FullName, or PhoneNumber)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                u.FullName.ToLower().Contains(searchLower) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchLower)));
        }

        // 2. Role filter
        if (!string.IsNullOrWhiteSpace(role) && !role.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(u => u.Role.RoleName.ToLower() == role.Trim().ToLower());
        }

        // 3. Active status filter
        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // 4. Locked status filter (BR-AUTH-002)
        if (isLocked.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isLocked.Value)
            {
                query = query.Where(u => u.LockedUntil.HasValue && u.LockedUntil.Value > now);
            }
            else
            {
                query = query.Where(u => !u.LockedUntil.HasValue || u.LockedUntil.Value <= now);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var safePageNumber = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var items = users.Select(MapToAdminUserDto).ToList();

        return Ok(new PaginatedResponseDto<AdminUserListItemDto>(items, totalCount, safePageNumber, safePageSize));
    }

    /// <summary>
    /// Retrieves full details and operational metadata for a single user.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.OperatorProfile)
            .Include(u => u.PassengerProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User Not Found",
                Detail = $"User with ID '{id}' was not found in the directory."
            });
        }

        return Ok(MapToAdminUserDto(user));
    }

    /// <summary>
    /// Reassigns a user's role with safety invariant checks (BR-ADMIN-001, BR-ADMIN-002, BR-AUDIT-001).
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> UpdateUserRole(
        Guid id,
        [FromBody] UpdateUserRoleRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRoleType>(dto.Role, ignoreCase: true, out var targetRoleType))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Role",
                Detail = $"Role '{dto.Role}' is not a valid canonical system role."
            });
        }

        var targetRoleName = targetRoleType.ToString();

        var callerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(callerIdClaim, out var callerId);

        // Safety Invariant: BR-ADMIN-002 Self-demotion lockout prevention
        if (callerId == id && targetRoleName != UserRoleType.Admin.ToString())
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Self-Demotion Prohibited",
                Detail = "Administrators cannot demote their own account. Another administrator must perform this action."
            });
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.OperatorProfile)
            .Include(u => u.PassengerProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User Not Found",
                Detail = $"User with ID '{id}' was not found."
            });
        }

        // Safety Invariant: BR-ADMIN-001 Sole Administrator Protection
        if (user.Role.RoleName == UserRoleType.Admin.ToString() && targetRoleName != UserRoleType.Admin.ToString())
        {
            var otherActiveAdminsCount = await _context.Users
                .CountAsync(u => u.Role.RoleName == UserRoleType.Admin.ToString() && u.IsActive && u.Id != id, cancellationToken);

            if (otherActiveAdminsCount == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Sole Administrator Protection",
                    Detail = "Cannot demote the last remaining active Administrator in the system."
                });
            }
        }

        var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == targetRoleName, cancellationToken);
        if (newRole == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Role Definition Missing",
                Detail = $"Role entity for '{targetRoleName}' does not exist in the database."
            });
        }

        var oldRoleName = user.Role.RoleName;
        user.RoleId = newRole.Id;
        user.Role = newRole;

        // BR-ADMIN-003: Profile Entity Lifecycle Integrity
        if (targetRoleName == UserRoleType.Operator.ToString() && user.OperatorProfile == null)
        {
            var opProfile = new OperatorProfile
            {
                UserId = user.Id,
                OperatorCode = !string.IsNullOrWhiteSpace(dto.OperatorCode)
                    ? dto.OperatorCode.Trim()
                    : $"OP-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                CompanyName = dto.CompanyName?.Trim() ?? "Transit Provider",
                AssignedRegion = dto.AssignedRegion?.Trim() ?? "Western Province"
            };
            await _context.OperatorProfiles.AddAsync(opProfile, cancellationToken);
            user.OperatorProfile = opProfile;
        }

        user.UpdatedAt = DateTime.UtcNow;

        // BR-AUDIT-001: Tamper-evident Audit Logging
        var auditLog = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActorId = callerId.ToString(),
            ActionType = "ROLE_CHANGE",
            EntityName = "User",
            EntityId = user.Id.ToString(),
            BeforeStateJson = JsonSerializer.Serialize(new { Role = oldRoleName }),
            AfterStateJson = JsonSerializer.Serialize(new
            {
                Role = targetRoleName,
                Reason = dto.Reason,
                OperatorCode = user.OperatorProfile?.OperatorCode
            })
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Admin {AdminId} changed role of User {UserId} from {OldRole} to {NewRole}. Reason: {Reason}",
            callerId, id, oldRoleName, targetRoleName, dto.Reason);

        return Ok(MapToAdminUserDto(user));
    }

    /// <summary>
    /// Toggles active status or clears account lockout (BR-AUTH-002, BR-ADMIN-001, BR-AUDIT-001).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequestDto dto,
        CancellationToken cancellationToken)
    {
        var callerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(callerIdClaim, out var callerId);

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.OperatorProfile)
            .Include(u => u.PassengerProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User Not Found",
                Detail = $"User with ID '{id}' was not found."
            });
        }

        // BR-ADMIN-002: Cannot deactivate self
        if (dto.IsActive == false && callerId == id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Self-Deactivation Prohibited",
                Detail = "Administrators cannot deactivate their own account."
            });
        }

        // BR-ADMIN-001: Sole Administrator Protection on deactivation
        if (dto.IsActive == false && user.Role.RoleName == UserRoleType.Admin.ToString())
        {
            var otherActiveAdminsCount = await _context.Users
                .CountAsync(u => u.Role.RoleName == UserRoleType.Admin.ToString() && u.IsActive && u.Id != id, cancellationToken);

            if (otherActiveAdminsCount == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Sole Administrator Protection",
                    Detail = "Cannot deactivate the last remaining active Administrator in the system."
                });
            }
        }

        var beforeState = new
        {
            IsActive = user.IsActive,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockedUntil = user.LockedUntil
        };

        if (dto.IsActive.HasValue)
        {
            user.IsActive = dto.IsActive.Value;
        }

        // BR-AUTH-002: Reset lockout
        if (dto.UnlockAccount)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
        }

        user.UpdatedAt = DateTime.UtcNow;

        // BR-AUDIT-001: Tamper-evident Audit Logging
        var auditLog = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActorId = callerId.ToString(),
            ActionType = "STATUS_CHANGE",
            EntityName = "User",
            EntityId = user.Id.ToString(),
            BeforeStateJson = JsonSerializer.Serialize(beforeState),
            AfterStateJson = JsonSerializer.Serialize(new
            {
                IsActive = user.IsActive,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LockedUntil = user.LockedUntil,
                Reason = dto.Reason
            })
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapToAdminUserDto(user));
    }

    /// <summary>
    /// Provisions a new user account with pre-assigned role and profile attributes.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> CreateUser(
        [FromBody] AdminCreateUserDto dto,
        CancellationToken cancellationToken)
    {
        var callerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(callerIdClaim, out var callerId);

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User Already Exists",
                Detail = $"A user account with email '{normalizedEmail}' already exists."
            });
        }

        if (!Enum.TryParse<UserRoleType>(dto.Role, ignoreCase: true, out var roleType))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Role",
                Detail = $"Role '{dto.Role}' is not a valid system role."
            });
        }

        var roleName = roleType.ToString();
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);
        if (role == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Role Definition Missing",
                Detail = $"Role '{roleName}' is not defined in the system."
            });
        }

        var user = new User
        {
            Email = normalizedEmail,
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };

        if (roleName == UserRoleType.Operator.ToString())
        {
            user.OperatorProfile = new OperatorProfile
            {
                UserId = user.Id,
                OperatorCode = !string.IsNullOrWhiteSpace(dto.OperatorCode)
                    ? dto.OperatorCode.Trim()
                    : $"OP-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                CompanyName = dto.CompanyName?.Trim() ?? "Transit Provider",
                AssignedRegion = dto.AssignedRegion?.Trim() ?? "Western Province"
            };
        }
        else if (roleName == UserRoleType.Passenger.ToString())
        {
            user.PassengerProfile = new PassengerProfile
            {
                UserId = user.Id,
                NicOrPassport = dto.NicOrPassport?.Trim()
            };
        }

        await _context.Users.AddAsync(user, cancellationToken);

        // BR-AUDIT-001: Tamper-evident Audit Logging
        var auditLog = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActorId = callerId.ToString(),
            ActionType = "USER_CREATED",
            EntityName = "User",
            EntityId = user.Id.ToString(),
            BeforeStateJson = null,
            AfterStateJson = JsonSerializer.Serialize(new
            {
                Email = user.Email,
                FullName = user.FullName,
                Role = roleName
            })
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, MapToAdminUserDto(user));
    }

    /// <summary>
    /// Returns available system roles with counts and descriptions.
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<List<RoleSummaryDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Select(r => new RoleSummaryDto
            {
                RoleName = r.RoleName,
                Description = r.Description ?? string.Empty,
                UserCount = r.Users.Count()
            })
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    // =========================================================================
    // Helper Mappings
    // =========================================================================

    private static AdminUserListItemDto MapToAdminUserDto(User user)
    {
        string? metadata = null;
        if (user.OperatorProfile != null)
        {
            metadata = $"{user.OperatorProfile.OperatorCode} • {user.OperatorProfile.AssignedRegion ?? "All Regions"}";
        }
        else if (user.PassengerProfile != null)
        {
            metadata = user.PassengerProfile.NicOrPassport != null
                ? $"NIC: {user.PassengerProfile.NicOrPassport}"
                : "Standard Passenger";
        }

        return new AdminUserListItemDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.RoleName,
            IsActive = user.IsActive,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockedUntil = user.LockedUntil,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            ProfileMetadata = metadata
        };
    }
}
