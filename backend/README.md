# WayPoint Backend (ASP.NET Core 8 Web API)

The authoritative backend application layer for **WayPoint** (**SE3090 Assignment 1**). Built using **Clean Architecture**, **ASP.NET Core 8**, **Entity Framework Core 9**, and **Npgsql 9**.

---

## 1. Architectural Layers

The solution is divided into four distinct projects following the Clean Architecture pattern:

```text
backend/
├── WayPoint.Domain/             # Enterprise business entities, enums, domain rules (no external deps)
├── WayPoint.Application/        # Use cases, interfaces, DTOs, business validations
├── WayPoint.Infrastructure/     # EF Core 9 DbContext, Npgsql 9 data access, external services, migrations
├── WayPoint.API/                # ASP.NET Core REST API controllers, filters, middleware, Program.cs
└── WayPoint.sln                 # .NET solution file
```

---

## 2. Key Technology Stack

- **Framework**: .NET 8.0 SDK (C# 12)
- **Database Driver**: `Npgsql.EntityFrameworkCore.PostgreSQL` (v9.0.4)
- **ORM**: Microsoft Entity Framework Core (v9.0.1)
- **Direct SSL & ALPN**: Native support for PostgreSQL 18 direct SSL (`SslNegotiation = SslNegotiation.Direct`)
- **Authentication**: JWT Bearer Authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **Password Security**: BCrypt (`BCrypt.Net-Next`)
- **API Documentation**: OpenAPI / Swagger (`Swashbuckle.AspNetCore`)

---

## 3. Environment & Configuration

Environment variables are loaded dynamically on startup from the root `.env` file via `Program.cs`.

### Required Variables
Ensure the following variables are defined in the repository root `.env` (copied from `.env.example`):

```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5010
API_BASE_URL=http://localhost:5010/api/v1
DATABASE_URL=postgresql://<username>:<password>@<host>:<port>/railway
JWT_SECRET=WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090
SESSION_SECRET=waypoint_secure_session_secret_2026
```

> **Security Rule**: `appsettings.json` must **never** contain live credentials or connection strings. All secrets reside exclusively in `.env` (which is excluded via `.gitignore`).

---

## 4. Setup & Running Locally

### Step 1: Restore Dependencies
```bash
dotnet restore backend/WayPoint.sln
```

### Step 2: Apply Migrations & Seed Database
Execute the built-in CLI seeder flag to apply all EF Core migrations and populate initial transit and user data:
```bash
dotnet run --project backend/WayPoint.API -- --seed
```

### Step 3: Run the Web API
```bash
dotnet run --project backend/WayPoint.API
```
The API starts on `http://localhost:5010`.

### Step 4: Access Swagger UI
Open your browser and navigate to:
```text
http://localhost:5010/swagger
```

---

## 5. Seeded Credentials for Testing

| Role | Email | Password | Assigned Persona |
| :--- | :--- | :--- | :--- |
| **System Administrator** | `admin@waypoint.lk` | `Admin@123` | System oversight & configurations |
| **Fleet Operator** | `operator@waypoint.lk` | `Operator@123` | Student 2 (Fleet & Bus Management) |
| **Ticketing Staff** | `staff@waypoint.lk` | `Staff@123` | Student 3 (Bookings & Check-ins) |
| **Passenger** | `passenger@waypoint.lk` | `Passenger@123` | Student 1 (Search & Journey Planning) |

---

## 6. Running Automated Tests

```bash
dotnet test
```
All unit and integration test suites are executed.
