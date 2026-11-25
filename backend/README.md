# WayPoint Backend (ASP.NET Core 8 Web API)

> **Status: Active Development (`v0.1.0` / WIP)**  
> This service is the authoritative backend API for **WayPoint**, currently under active development. Core domain entities, REST controllers, JWT authentication, and EF Core 9 PostgreSQL migrations are implemented, while production hardening, background workers, and live payment integration are still in progress.

---

## 1. Architecture & Project Structure

The backend follows **Clean Architecture** across four projects plus an xUnit test project:

```text
backend/
├── WayPoint.Domain/          # Domain entities, BaseEntity, and domain enums (no external deps)
├── WayPoint.Application/     # Service interfaces, feature DTOs, pagination & contracts
├── WayPoint.Infrastructure/  # WayPointDbContext (EF Core 9), Npgsql 9, migrations, DbSeeder & services
├── WayPoint.API/             # 22 REST controllers, JWT auth, ExceptionHandlingMiddleware & Swagger
├── WayPoint.Tests/           # xUnit unit & integration test suites
└── WayPoint.sln              # .NET 8 solution file
```

### Implemented REST Controllers (`WayPoint.API/Controllers/`)
- **Auth & Users**: `AuthController` (`/api/v1/auth`), `UserController` (`/api/v1/users`)
- **Journey Planning**: `RouteController` (`/api/v1/routes`), `ServiceController` (`/api/v1/services`), `JourneySearchController` (`/api/v1/journeys`)
- **Fleet & Resources**: `BusController` (`/api/v1/buses`), `SeatLayoutController` (`/api/v1/seats/layouts`), `SeatAvailabilityController` (`/api/v1/services`), `DriverController` (`/api/v1/drivers`), `ResourceFeasibilityController` (`/api/v1/resources`), `ReviewController` (`/api/v1/reviews`)
- **Booking & Payments**: `SeatHoldController` (`/api/v1/bookings/hold`), `BookingController` (`/api/v1/bookings`), `PaymentController` (`/api/v1/payments`), `TicketController` (`/api/v1/tickets`), `NotificationController` (`/api/v1/notifications`)
- **Disruptions & AI**: `DisruptionController` (`/api/v1/disruptions`), `RebookingController` (`/api/v1/rebooking`), `ApprovalController` (`/api/v1/approvals`), `ServiceAlertController` (`/api/v1/alerts`), `AiWorkflowController` (`/api/v1/ai/workflows`)
- **Development Helpers**: `DevController` (`/api/v1/dev`)

---

## 2. Technology Stack

- **Runtime**: .NET 8.0 (`net8.0`, C# 12)
- **ORM & PostgreSQL Provider**: Entity Framework Core `9.0.1` + `Npgsql.EntityFrameworkCore.PostgreSQL` `9.0.4`
- **Authentication & Security**: `Microsoft.AspNetCore.Authentication.JwtBearer` `8.0.11`, `BCrypt.Net-Next` `4.2.0`
- **API Docs**: `Swashbuckle.AspNetCore` `6.6.2` (OpenAPI / Swagger UI at `/swagger`)

---

## 3. Environment & Configuration

`Program.cs` automatically loads environment variables from the repository root `.env` (or `WayPoint.API/.env`). Copy `.env.example` from the repo root to `.env` and configure:

```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5010
DATABASE_URL=postgresql://user:password@localhost:5432/waypoint
JWT_SECRET=your_jwt_secret_key_here
AI_SERVICE_URL=http://localhost:8000
```

> **Note**: `Program.cs` enforces fail-fast startup if `JWT_SECRET` (or `Jwt:Secret` in config) is missing. Keep live secrets out of `appsettings.json` and commit history.

---

## 4. Setup & Running Locally

```bash
# 1. Restore packages
dotnet restore backend/WayPoint.sln

# 2. Apply EF Core migrations and seed baseline roles, routes, fleet & test users
dotnet run --project backend/WayPoint.API -- --seed

# 3. Run the API server
dotnet run --project backend/WayPoint.API
```

- **API Base URL**: `http://localhost:5010/api/v1`
- **Swagger UI**: `http://localhost:5010/swagger`
- **Health Check**: `http://localhost:5010/health`

---

## 5. Seeded Test Accounts (`DbSeeder.cs`)

The seeder creates the four `UserRoleType` roles (`Admin`, `TransportManager`, `Operator`, `Passenger`) and provisions four default users, all with password **`Password123!`**:

| Role | Email | Password |
| :--- | :--- | :--- |
| `Admin` | `admin@waypoint.lk` | `Password123!` |
| `TransportManager` | `manager@waypoint.lk` | `Password123!` |
| `Operator` | `operator@waypoint.lk` | `Password123!` |
| `Passenger` | `passenger@waypoint.lk` | `Password123!` |

---

## 6. Running Automated Tests

```bash
dotnet test backend/WayPoint.sln --configuration Release
```

---

## 7. In Progress / Upcoming Backend Work

- [ ] Replace sandbox payment processing (`PaymentController.ProcessSandboxCharge`) with a real payment provider integration.
- [ ] Add hosted background worker for automatic seat-hold expiry cleanup and scheduled alert dispatch.
- [ ] Align `Conductor` / `Driver` role claims across backend `UserRoleType` and mobile conductor workflows.
- [ ] Lock down `[AllowAnonymous]` development/AI preview endpoints (`DevController`, AI recommendation fallbacks) before production release.
