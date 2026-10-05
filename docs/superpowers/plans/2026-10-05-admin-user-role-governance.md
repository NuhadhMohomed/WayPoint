# Admin User & Role Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full administrative user and role governance capabilities for the `Admin` role in WayPoint, including backend RBAC endpoints in `UserController.cs` and a dedicated Admin Hub in the React web application, with zero regression on existing backend and frontend tests.

**Architecture:** Extend the existing ASP.NET Core API with `RequireAdmin` policy-gated endpoints in `UserController.cs` that enforce business rules (`BR-ADMIN-001`, `BR-ADMIN-002`, `BR-ADMIN-003`, `BR-AUTH-002`) and write immutable entries via `IAuditService`. In the React frontend, add a top-level `/admin` layout with User Directory and Audit Ledger views adhering to `DESIGN.md` and WCAG AA guidelines.

**Tech Stack:** C# 12 / .NET 8, EF Core, xUnit, React 18, React Router v6, Tailwind CSS, Vitest, Testing Library.

**Spec:** [`docs/superpowers/specs/2026-10-05-admin-user-role-governance-design.md`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/superpowers/specs/2026-10-05-admin-user-role-governance-design.md)

---

## Global Constraints

- Endpoints must enforce `[Authorize(Policy = "RequireAdmin")]` matching canonical `UserRoleType.Admin` (`BR-AUTHZ-001`).
- Self-demotion from `Admin` to a lower role is prohibited and must return HTTP 400 Bad Request (`BR-ADMIN-002`).
- Deactivation or demotion of the last remaining active administrator is prohibited and must return HTTP 400 Bad Request (`BR-ADMIN-001`).
- Account unlock resets `FailedLoginAttempts = 0` and `LockedUntil = null` (`BR-AUTH-002`).
- Every administrative state change creates an immutable audit record in `AuditLogs` (`BR-AUDIT-001`).
- Existing self-service `/api/v1/users/me` and `/disruptions/admin` routes must remain 100% operational without regressions.
- All existing 71 backend tests and 36 frontend tests must continue passing.

## Review Focus

1. **Unauthenticated or Non-Admin access to admin endpoints**: Must return HTTP 401 Unauthorized or HTTP 403 Forbidden.
2. **Self-demotion lockout attempt**: Authenticated admin trying to demote their own account must receive HTTP 400 Bad Request.
3. **Sole admin deactivation**: Attempt to deactivate the only active admin must receive HTTP 400 Bad Request.
4. **Promotion to Operator without profile**: Promoting user to `Operator` must ensure `OperatorProfile` exists in database without throwing null exceptions.
5. **Route backward compatibility**: Existing route `/disruptions/admin` must continue rendering the Audit Ledger without breaking existing test suites.

---

## Task Decomposition

### Task 1: Administrative DTOs in Application Layer

**Files:**
- Create: `backend/WayPoint.Application/DTOs/Admin/AdminUserDtos.cs`

**Interfaces:**
- Produces: `AdminUserListItemDto`, `UpdateUserRoleRequestDto`, `UpdateUserStatusRequestDto`, `AdminCreateUserDto`, `RoleSummaryDto` in namespace `WayPoint.Application.DTOs.Admin`.

- [ ] **Step 1: Create `AdminUserDtos.cs`**

Write `backend/WayPoint.Application/DTOs/Admin/AdminUserDtos.cs` defining:
```csharp
namespace WayPoint.Application.DTOs.Admin;

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

public class UpdateUserRoleRequestDto
{
    public string Role { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? OperatorCode { get; set; }
    public string? CompanyName { get; set; }
    public string? AssignedRegion { get; set; }
}

public class UpdateUserStatusRequestDto
{
    public bool? IsActive { get; set; }
    public bool UnlockAccount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

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

public class RoleSummaryDto
{
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UserCount { get; set; }
}
```

- [ ] **Step 2: Verify project builds**

Run: `dotnet build backend/WayPoint.Application/WayPoint.Application.csproj`
Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit DTOs**

```bash
git add backend/WayPoint.Application/DTOs/Admin/AdminUserDtos.cs
git commit -m "feat(application): add admin user and role governance DTOs"
```

---

### Task 2: Backend Automated Tests for Admin Governance

**Files:**
- Create: `backend/WayPoint.Tests/AdminUserGovernanceTests.cs`

**Interfaces:**
- Consumes: `UserController`, `IWayPointDbContext`, `IAuditService`, `IPasswordHasher`, DTOs.
- Produces: Comprehensive test coverage for `BR-ADMIN-001`, `BR-ADMIN-002`, `BR-ADMIN-003`, `BR-AUTH-002`, and `BR-AUDIT-001`.

- [ ] **Step 1: Write failing tests in `AdminUserGovernanceTests.cs`**

Write test methods verifying:
1. `GetUsers_ReturnsPaginatedUsers_WhenAdmin`
2. `UpdateUserRole_PromotesToOperator_CreatesOperatorProfile`
3. `UpdateUserRole_SelfDemotion_ReturnsBadRequest` (`BR-ADMIN-002`)
4. `UpdateUserRole_SoleAdminDemotion_ReturnsBadRequest` (`BR-ADMIN-001`)
5. `UpdateUserStatus_UnlockAccount_ResetsFailedAttemptsAndLockout` (`BR-AUTH-002`)
6. `UpdateUserRole_LogsImmutableAuditRecord` (`BR-AUDIT-001`)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/WayPoint.Tests/WayPoint.Tests.csproj --filter "FullyQualifiedName~AdminUserGovernanceTests"`
Expected: FAIL (endpoints / logic not yet implemented in `UserController`).

- [ ] **Step 3: Commit failing tests**

```bash
git add backend/WayPoint.Tests/AdminUserGovernanceTests.cs
git commit -m "test(backend): add test suite for admin user and role governance"
```

---

### Task 3: Implement Administrative Endpoints in `UserController.cs`

**Files:**
- Modify: `backend/WayPoint.API/Controllers/UserController.cs`

**Interfaces:**
- Consumes: `IWayPointDbContext`, `IAuditService`, `IPasswordHasher`, `ILogger<UserController>`, `AdminUserDtos`.
- Produces: `GET /api/v1/users`, `GET /api/v1/users/{id}`, `PUT /api/v1/users/{id}/role`, `PATCH /api/v1/users/{id}/status`, `POST /api/v1/users`, `GET /api/v1/users/roles`.

- [ ] **Step 1: Update `UserController.cs` constructor to inject dependencies**

Inject `IAuditService`, `IPasswordHasher`, and `ILogger<UserController>` into `UserController`.

- [ ] **Step 2: Implement `GetUsers` and `GetUserById`**
- Implement paginated querying with `.AsNoTracking()`, filtering by search term (case-insensitive email or full name), role name, active status, and locked status.
- Map associated profile metadata (e.g. `OperatorCode` or `NicOrPassport`).

- [ ] **Step 3: Implement `UpdateUserRole` with `BR-ADMIN-001`, `BR-ADMIN-002`, `BR-ADMIN-003`, and Audit Logging**
- Validate target role exists in `UserRoleType` enum.
- Check self-demotion: if target user ID equals caller name identifier and new role != `Admin`, return `BadRequest("Administrators cannot demote their own account.")`.
- Check sole admin: if target user is currently `Admin` and new role != `Admin`, verify `CountAsync(u => u.Role.RoleName == "Admin" && u.IsActive && u.Id != id) > 0`. If not, return `BadRequest("Cannot demote the last remaining active Administrator.")`.
- If new role is `Operator` and user has no `OperatorProfile`, create `new OperatorProfile { UserId = user.Id, OperatorCode = dto.OperatorCode ?? $"OP-{Guid.NewGuid().ToString()[..6].ToUpper()}", CompanyName = dto.CompanyName, AssignedRegion = dto.AssignedRegion ?? "Western Province" }`.
- Save changes and call `_auditService.LogActionAsync(callerId, "User", user.Id, "ROLE_CHANGE", ...)` with reason and details.

- [ ] **Step 4: Implement `UpdateUserStatus` with Sole Admin and Unlock Guards**
- If `dto.IsActive == false` and target is `Admin`, ensure `activeAdminCount > 1` (`BR-ADMIN-001`). Also block deactivating self (`BR-ADMIN-002`).
- If `dto.UnlockAccount == true`, reset `user.FailedLoginAttempts = 0; user.LockedUntil = null;`.
- If `dto.IsActive.HasValue`, update `user.IsActive = dto.IsActive.Value;`.
- Log audit record and save changes.

- [ ] **Step 5: Implement `CreateUser` and `GetRoles`**
- In `CreateUser`: validate email uniqueness, hash password via `_passwordHasher.HashPassword`, assign role, create `OperatorProfile` or `PassengerProfile` according to role, log audit action.
- In `GetRoles`: return all roles with user counts and descriptions.

- [ ] **Step 6: Run tests to verify all tests pass**

Run: `dotnet test backend/WayPoint.Tests/WayPoint.Tests.csproj --filter "FullyQualifiedName~AdminUserGovernanceTests"`
Expected: PASS (All tests pass).

- [ ] **Step 7: Run entire backend test suite to guarantee zero regression**

Run: `dotnet test backend/WayPoint.sln`
Expected: 100% PASS (All 71+ tests pass).

- [ ] **Step 8: Commit `UserController.cs`**

```bash
git add backend/WayPoint.API/Controllers/UserController.cs
git commit -m "feat(api): implement admin user and role governance endpoints"
```

---

### Task 4: Frontend Admin API Client

**Files:**
- Create: `web/src/features/admin/adminApi.js`

**Interfaces:**
- Consumes: `web/src/lib/axios.js` (or existing apiClient).
- Produces: `adminApi.getUsers`, `adminApi.getUserById`, `adminApi.updateUserRole`, `adminApi.updateUserStatus`, `adminApi.createUser`, `adminApi.getRoles`.

- [ ] **Step 1: Write `adminApi.js`**

Implement methods for all `/api/v1/users` admin endpoints with appropriate error handling and query serialization.

- [ ] **Step 2: Commit `adminApi.js`**

```bash
git add web/src/features/admin/adminApi.js
git commit -m "feat(web): add admin governance API service client"
```

---

### Task 5: Frontend Admin Hub Layout & User Directory Page

**Files:**
- Create: `web/src/features/admin/AdminHubLayout.jsx`
- Create: `web/src/features/admin/AdminUsersPage.jsx`
- Create: `web/src/features/admin/components/ChangeRoleModal.jsx`
- Create: `web/src/features/admin/components/ProvisionUserModal.jsx`

**Interfaces:**
- Consumes: `adminApi`, `DataTable`, `Modal`, `Button`, `TransitBadge`, `Input`, `useAuthStore`.
- Produces: Interactive User & Role Directory with metrics, search/filtering, role reassignment, and account unlocking.

- [ ] **Step 1: Implement `AdminHubLayout.jsx`**
- Render page header with title "System Administration & Governance Hub".
- Tab navigation between `/admin/users` ("User Directory & RBAC") and `/admin/audit` ("Audit & Security Vault").

- [ ] **Step 2: Implement `ChangeRoleModal.jsx`**
- Accessible dialog powered by [`Modal.jsx`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/components/ui/Modal.jsx).
- Shows current user and role.
- Role select radio options with permission summaries.
- Mandatory "Reason for Role Change" input.
- Displays self-demotion warning when editing current user account.

- [ ] **Step 3: Implement `ProvisionUserModal.jsx`**
- Fields: Full Name, Email, Temporary Password, Phone Number, Role selector, and conditional Operator details (Operator Code, Company).

- [ ] **Step 4: Implement `AdminUsersPage.jsx`**
- 4 telemetry cards: Total Users, Active Admins, Operators/Managers, Locked/Restricted.
- Filter bar: Search input (name/email), Role dropdown, Status dropdown, and "Provision User" button.
- [`DataTable`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/components/ui/DataTable.jsx) mapping columns: User (avatar, name, email), Role (TransitBadge), Status (Active/Locked), Profile info, Created Date, Actions ("Change Role", "Unlock", "Deactivate/Activate").

- [ ] **Step 5: Commit UI components**

```bash
git add web/src/features/admin/
git commit -m "feat(web): create admin user and role governance views"
```

---

### Task 6: Routing, Dashboard Navigation & Backward Compatibility

**Files:**
- Modify: `web/src/App.jsx`
- Modify: `web/src/layouts/DashboardLayout.jsx`

**Interfaces:**
- Consumes: `AdminHubLayout`, `AdminUsersPage`, `AdminConsolePage`.
- Produces: Working `/admin` navigation for Admin role, backward-compatible `/disruptions/admin`.

- [ ] **Step 1: Update `DashboardLayout.jsx`**
- Add navigation item for `Admin` role in `navItems`:
  ```javascript
  {
    to: '/admin',
    label: 'Admin Console & Users',
    subtext: 'User Governance & Audit Ledger',
    icon: ShieldCheck,
    roles: ['Admin']
  }
  ```

- [ ] **Step 2: Update `App.jsx`**
- Add `/admin` routes:
  ```jsx
  <Route path="/admin" element={<AdminHubLayout />}>
    <Route index element={<Navigate to="users" replace />} />
    <Route path="users" element={<AdminUsersPage />} />
    <Route path="audit" element={<AdminConsolePage />} />
  </Route>
  ```
- Ensure `/disruptions/admin` remains intact.

- [ ] **Step 3: Commit routing changes**

```bash
git add web/src/App.jsx web/src/layouts/DashboardLayout.jsx
git commit -m "feat(web): register admin governance routes and sidebar navigation"
```

---

### Task 7: Frontend Automated Tests & Full System Verification

**Files:**
- Create: `web/src/features/admin/__tests__/AdminUsersPage.test.jsx`

**Interfaces:**
- Tests `AdminUsersPage` rendering, telemetry calculation, search filtering, and role modal interaction.

- [ ] **Step 1: Write `AdminUsersPage.test.jsx`**
- Test renders telemetry metrics and user table rows.
- Test filtering by search term and role dropdown.
- Test self-demotion warning when editing current user.
- Test unlock account action triggers API update.

- [ ] **Step 2: Run frontend tests**

Run: `npm test -- --run` in `web/`
Expected: 100% PASS (All tests pass).

- [ ] **Step 3: Run full backend test suite**

Run: `dotnet test backend/WayPoint.sln`
Expected: 100% PASS (All tests pass).

- [ ] **Step 4: Commit frontend tests**

```bash
git add web/src/features/admin/__tests__/AdminUsersPage.test.jsx
git commit -m "test(web): add automated test suite for admin user governance"
```
