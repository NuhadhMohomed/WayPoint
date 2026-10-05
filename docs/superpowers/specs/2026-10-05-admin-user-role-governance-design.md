# WayPoint Admin User & Role Governance Specification

**Document Identifier:** `SPEC-ADMIN-2026-10-05-01`  
**Status:** In Review (Brainstorming Phase)  
**Target:** `backend/` (ASP.NET Core Web API) & `web/` (React SPA)  
**Authority:** Aligned with `AGENTS.md`, `docs/design/DESIGN.md`, and `docs/requirements/business-rules.md`  

---

## 1. Executive Summary & Design Mandate

WayPoint currently implements a four-tier role hierarchy defined by [`UserRoleType`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/backend/WayPoint.Domain/Enums/DomainEnums.cs): `Passenger`, `Operator`, `TransportManager`, and `Admin`. While authorization policies (`RequirePassenger`, `RequireOperator`, `RequireManager`, `RequireAdmin`) are configured in ASP.NET Core, the platform currently lacks an administrative capability for user and role lifecycle management.

This specification formalizes the **Admin User & Role Governance** subsystem. It empowers platform Administrators to:
1. Retain comprehensive omni-access across all transit operational surfaces (Routes, Timetables, Fleet, Manifests, Incidents, Approvals).
2. Inspect, search, filter, and page all registered users across the transit network.
3. Securely reassign user roles (`Passenger` $\leftrightarrow$ `Operator` $\leftrightarrow$ `TransportManager` $\leftrightarrow$ `Admin`) with deterministic safety guards.
4. Manage account availability: activate, deactivate, or unlock accounts locked due to consecutive bad password attempts (`BR-AUTH-002`).
5. Provision enterprise personnel accounts directly without public self-registration.
6. Enforce immutable audit trails (`BR-AUDIT-001`) for every administrative intervention.
7. Deliver a dedicated, WCAG AA compliant **Admin & Governance Hub** in the React web application with zero regressions to existing tests or workflows.

---

## 2. Business Rules & Safety Invariants

### 2.1 Role-Based Access Control (`BR-AUTHZ-001`)
* Administrative endpoints require the canonical `Admin` role claim verified via policy `RequireAdmin`.
* Standard users (`Passenger`, `Operator`, `TransportManager`) attempting to access administrative endpoints receive immediate HTTP 403 Forbidden responses at the middleware layer.

### 2.2 Account Lockout & Recovery (`BR-AUTH-002`)
* Accounts with 5 consecutive failed logins are locked for 15 minutes (`FailedLoginAttempts >= 5` and `LockedUntil > UtcNow`).
* Administrators can execute an immediate administrative unlock via `PATCH /api/v1/users/{id}/status`, resetting `FailedLoginAttempts = 0` and `LockedUntil = null`.

### 2.3 Sole Administrator Protection (`BR-ADMIN-001`)
* The system must never be left in a state with zero active administrators.
* Any administrative request to demote or deactivate an `Admin` user must be rejected with HTTP 400 Bad Request if the target user is the sole remaining active administrator (`ActiveAdminCount <= 1`).

### 2.4 Self-Demotion Lockout Prevention (`BR-ADMIN-002`)
* An authenticated administrator cannot demote their own account from `Admin` to a lower role.
* Self-demotion attempts are rejected with HTTP 400 Bad Request to prevent accidental administrative lockout.

### 2.5 Profile Entity Lifecycle Integrity (`BR-ADMIN-003`)
* When an account is created or promoted to `Operator`, the backend automatically verifies and instantiates an associated [`OperatorProfile`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/backend/WayPoint.Domain/Entities/Identity/IdentityEntities.cs#L32-L41) with a default or specified operator code (`OP-{CODE}`) and province assignment.
* Passenger travel history and profiles are preserved across role promotions.

### 2.6 Tamper-Evident Audit Logging (`BR-AUDIT-001`)
* Every administrative role reassignment, user creation, deactivation, and account unlock must record an append-only entry in the database `AuditLogs` table using [`IAuditService`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/backend/WayPoint.Application/Common/Interfaces/IAuditService.cs).
* The log entry captures: `ActorId` (Admin GUID), `EntityName: "User"`, `EntityId: TargetUserGuid`, `ActionType: "ROLE_CHANGE" | "STATUS_CHANGE" | "USER_CREATED"`, and `Payload` containing old role, new role, timestamp, and mandatory administrative reason.

---

## 3. Backend Architecture & API Contracts

### 3.1 DTO Contracts (`WayPoint.Application.DTOs.Admin`)

```csharp
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
    public string? ProfileMetadata { get; set; } // Operator code, Region, or NIC
}

/// <summary>
/// Request payload to change a user's role.
/// </summary>
public class UpdateUserRoleRequestDto
{
    public string Role { get; set; } = string.Empty; // Validated against UserRoleType
    public string Reason { get; set; } = string.Empty; // Mandatory audit justification
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
```

### 3.2 Endpoint Specifications in [`UserController.cs`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/backend/WayPoint.API/Controllers/UserController.cs)

Existing endpoints `GET /api/v1/users/me` and `PUT /api/v1/users/me` remain unchanged for user self-service.

```csharp
[ApiController]
[Route("api/v1/users")]
public class UserController : ControllerBase
{
    // ---------------------------------------------------------
    // Existing Self-Service Endpoints (Unchanged)
    // ---------------------------------------------------------
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetProfile(...)

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile(...)

    // ---------------------------------------------------------
    // New Administrative Governance Endpoints
    // ---------------------------------------------------------

    /// <summary>
    /// Searches and pages through registered users with optional role and status filters.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<PagedResult<AdminUserListItemDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isLocked,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches full record and profile telemetry for a specific user.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> GetUserById(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reassigns a user's role claim with safety checks (BR-ADMIN-001, BR-ADMIN-002, BR-AUDIT-001).
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> UpdateUserRole(
        Guid id,
        [FromBody] UpdateUserRoleRequestDto dto,
        CancellationToken cancellationToken);

    /// <summary>
    /// Toggles active status or resets failed login lockout (BR-AUTH-002, BR-AUDIT-001).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequestDto dto,
        CancellationToken cancellationToken);

    /// <summary>
    /// Provisions a new user account with pre-assigned role and profile attributes.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<AdminUserListItemDto>> CreateUser(
        [FromBody] AdminCreateUserDto dto,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns available system roles with counts and descriptions.
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<List<RoleSummaryDto>>> GetRoles(
        CancellationToken cancellationToken);
}
```

---

## 4. Frontend Architecture & UI/UX Specifications

### 4.1 Routing & Navigation Integration

1. **Top-Level Navigation Item** in [`DashboardLayout.jsx`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/layouts/DashboardLayout.jsx):
   ```javascript
   {
     to: '/admin',
     label: 'Admin & Governance Hub',
     subtext: 'User Directory, Role RBAC & Audit Ledger',
     icon: ShieldCheck,
     roles: ['Admin']
   }
   ```
2. **Route Structure** in [`App.jsx`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/App.jsx):
   ```jsx
   <Route path="/admin" element={<AdminHubLayout />}>
     <Route index element={<Navigate to="users" replace />} />
     <Route path="users" element={<AdminUsersPage />} />
     <Route path="audit" element={<AdminConsolePage />} />
   </Route>
   ```
3. **Backward Compatibility**:
   - `/disruptions/admin` remains functional as an alias or redirect to `/admin/audit`, preserving 100% of existing tests in `DisruptionHub.test.jsx`.

### 4.2 UI Components & Views

#### A. Telemetry Strip (`AdminUsersPage.jsx`)
Four grid-cards displaying real-time system metrics:
* **Total Users**: Total registered accounts.
* **Active Administrators**: Displays count with warning indicator if $=1$ (Sole Admin risk).
* **Transport Personnel**: Combined count of `Operator` + `TransportManager` users.
* **Locked / Inactive Accounts**: Count of accounts currently restricted.

#### B. Filter & Search Controls
* Debounced live search across `FullName`, `Email`, and `PhoneNumber`.
* Role dropdown filter (`All Roles`, `Admin`, `TransportManager`, `Operator`, `Passenger`).
* Status toggle filter (`All`, `Active`, `Locked/Inactive`).
* Action button: **"Provision User"** (opens user creation modal).

#### C. User Directory Table ([`DataTable.jsx`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/components/ui/DataTable.jsx))
* Columns:
  1. **User Identity**: Full Name, Email, Avatar with initials.
  2. **Role**: Visual [`TransitBadge`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/components/ui/TransitBadge.jsx) with role-specific iconography (Shield for Admin, Scale for TransportManager, Bus for Operator, User for Passenger).
  3. **Status**: Green pulse dot for Active, Red/Amber chip for Locked, Slate chip for Inactive.
  4. **Profile**: Assigned Operator Code or Passenger NIC.
  5. **Joined Date**: Formatted date string (`YYYY-MM-DD`).
  6. **Actions**:
     * **"Edit Role"** button $\rightarrow$ launches Role Modal.
     * **"Unlock"** button (visible only if account is locked).
     * **"Deactivate / Activate"** toggle with confirmation.

#### D. Interactive Modals ([`Modal.jsx`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/components/ui/Modal.jsx))
1. **"Change User Role" Modal**:
   * Target user banner.
   * Radio group with descriptions of each role's permissions.
   * Mandatory Reason text area.
   * Client-side guards:
     * Disables selection if target user is self (`BR-ADMIN-002`).
     * Alerts if target user is sole active admin (`BR-ADMIN-001`).
2. **"Provision User" Modal**:
   * Email, Full Name, Phone Number, Initial Password.
   * Role selector dropdown.
   * Dynamic profile fields (e.g. Operator Code and Region if role is `Operator`).
3. **"Unlock Account" Confirmation Modal**:
   * Summary of failed attempts and lockout expiration.
   * Immediate reset button.

---

## 5. Security & Verification Plan

### 5.1 Backend Automated Tests (`backend/WayPoint.Tests/AdminUserGovernanceTests.cs`)
1. `GetUsers_AsAdmin_ReturnsPaginatedUsers`: Asserts 200 OK and valid pagination.
2. `GetUsers_AsOperator_Returns403Forbidden`: Validates BFLA security gate.
3. `GetUsers_AsPassenger_Returns403Forbidden`: Validates non-admin rejection.
4. `UpdateUserRole_PromotesPassengerToOperator_CreatesOperatorProfile`: Asserts role update and profile instantiation.
5. `UpdateUserRole_SelfDemotion_Returns400BadRequest`: Enforces `BR-ADMIN-002`.
6. `UpdateUserRole_SoleAdminDemotion_Returns400BadRequest`: Enforces `BR-ADMIN-001`.
7. `UpdateUserStatus_UnlockLockedAccount_ClearsFailedAttempts`: Enforces `BR-AUTH-002`.
8. `UpdateUserRole_LogsImmutableAuditRecord`: Verifies entry in `AuditLogs` table (`BR-AUDIT-001`).

### 5.2 Frontend Automated Tests (`web/src/features/admin/__tests__/AdminUsersPage.test.jsx`)
1. `AdminUsersPage_RendersTelemetryAndTable`: Verifies metric cards and user data rows render.
2. `AdminUsersPage_FiltersByRoleAndSearch`: Validates live client-side or API query filtering.
3. `AdminUsersPage_SelfDemotionDisabled`: Asserts current logged-in admin cannot select lower roles for their own account.
4. `AdminUsersPage_UnlocksAccount_CallsApiAndUpdatesBadge`: Validates status update trigger.

### 5.3 Regression Assurance
* Execute full test suite `dotnet test backend/WayPoint.sln`.
* Execute frontend test suite `npm test -- --run` in `web/`.
* Verify existing 71 backend tests and 36 frontend tests continue passing with zero regressions.

---

## 6. Acceptance Criteria

| Criteria ID | Requirement | Validation Method |
| :--- | :--- | :--- |
| **AC-01** | Admin can list and search users with role and status filtering. | Automated integration test & manual UI verification. |
| **AC-02** | Admin can reassign user roles with mandatory reason logged to audit ledger. | Database query verifying `AuditLogs` entry and role claim. |
| **AC-03** | Sole admin and self-demotion lockout protections are strictly enforced. | Unit tests asserting HTTP 400 with descriptive problem details. |
| **AC-04** | Locked accounts can be unlocked by Admin resetting lockout counters. | Login verification of previously locked user account. |
| **AC-05** | Dedicated `/admin` cockpit renders seamlessly in React web app with dark theme. | Component render test & visual review. |
| **AC-06** | All existing 71 backend tests and 36 frontend tests remain 100% passing. | Automated test run execution. |
