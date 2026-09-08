# Railway PostgreSQL Database Guide & Deployment Specification

This document details the configuration, deployment topology, connection handling, and migration procedures for the managed **PostgreSQL** database on **Railway** powering **WayPoint** (**SE3090 Assignment 1**).

---

## 1. Cloud Database Architecture

WayPoint utilizes a managed PostgreSQL database hosted in Railway:

- **Deployment Platform**: Railway Managed Database Service
- **Engine**: PostgreSQL 18.6 (Debian 64-bit)
- **Database Name**: `railway` (or `waypoint`)
- **Hosting Region**: `EU West` (Amsterdam / Europe)
- **Container Port**: `5432`
- **External Access**: Railway TCP Proxy (`proxy.rlwy.net`)

```mermaid
graph LR
    subgraph LocalDev["Local Development Environment"]
        DotNetBackend[".NET 8 Web API (WayPoint.API)"]
        CLISeeder["EF Core CLI Migrations / Seeder"]
    end

    subgraph RailwayCloud["Railway Cloud Infrastructure"]
        TCPProxy["Railway TCP Edge Proxy (*.proxy.rlwy.net:PORT)"]
        PostgresContainer["PostgreSQL 18.6 Container (0.0.0.0:5432)"]
    end

    DotNetBackend -->|Direct SSL + ALPN 'postgresql'| TCPProxy
    CLISeeder -->|Direct SSL + ALPN 'postgresql'| TCPProxy
    TCPProxy -->|Internal Network| PostgresContainer
```

---

## 2. PostgreSQL 18 Direct SSL & ALPN Requirement

Railway provisions PostgreSQL 18.6 containers. PostgreSQL 17 and 18 introduce **Direct SSL Negotiation** with mandatory **Application-Layer Protocol Negotiation (ALPN)**.

### Why Classic STARTTLS Fails on Railway TCP Proxies
- Classic PostgreSQL drivers (e.g. Npgsql 8.x) initiate connections with an 8-byte plaintext `SSLRequest` packet and wait for a single-byte response (`'S'` or `'N'`).
- Railway's edge TCP proxy forwards TLS handshakes directly to PostgreSQL's direct SSL port. If cleartext packets are sent, the connection hangs waiting for a response, causing `System.TimeoutException: Timeout during reading attempt`.
- When direct TLS handshakes are initiated, PostgreSQL 18 requires the TLS ClientHello to include the **ALPN extension** set to `"postgresql"`. If ALPN is missing, PostgreSQL rejects the connection with:
  ```
  LOG: received direct SSL connection request without ALPN protocol negotiation extension
  ```

### The Solution in WayPoint (.NET 8 + Npgsql 9)
WayPoint uses **Npgsql 9.0.4** and **Entity Framework Core 9.0.1** with `SslNegotiation = SslNegotiation.Direct`:

```csharp
// backend/WayPoint.Infrastructure/DependencyInjection.cs
var builder = new NpgsqlConnectionStringBuilder
{
    Host = uri.Host,
    Port = uri.Port > 0 ? uri.Port : 5432,
    Username = userInfo.Length > 0 ? userInfo[0] : "",
    Password = userInfo.Length > 1 ? userInfo[1] : "",
    Database = uri.AbsolutePath.TrimStart('/'),
    SslMode = SslMode.Require,
    SslNegotiation = SslNegotiation.Direct,
    Timeout = 15,
    CommandTimeout = 60
};
```

---

## 3. Environment Variable Configuration

All database connection parameters are loaded dynamically at runtime from the root `.env` file or environment variables. **Credentials must never be hardcoded into `appsettings.json` or committed to Git.**

### `.env` Setup (Local Development)

```env
# Database Configuration (Railway PostgreSQL Public Proxy)
DATABASE_URL=postgresql://<user>:<password>@<host>.proxy.rlwy.net:<port>/railway
```

### Railway Service Configuration (Production API)
When deploying the ASP.NET Core API container on Railway:
- Set `DATABASE_URL` in the API service's **Variables** tab to Railway's private internal network URL:
  ```
  postgresql://postgres:${{Postgres.PGPASSWORD}}@postgres.railway.internal:5432/railway
  ```
- This avoids public internet round-trips and provides sub-millisecond database queries.

---

## 4. Applying Migrations & Seeding Data

Entity Framework Core migrations in `backend/WayPoint.Infrastructure/Data/Migrations/` are the authoritative schema definition for WayPoint.

### Method 1: Automatic CLI Seeder (Recommended)
Run the backend with the `--seed` flag:
```bash
dotnet run --project backend/WayPoint.API -- --seed
```
This command:
1. Connects to PostgreSQL using `DATABASE_URL` from `.env`.
2. Automatically applies all pending EF Core migrations (`__EFMigrationsHistory`).
3. Seeds baseline data:
   - 4 System Users (Admin, Fleet Operator, Ticketing Staff, Passenger)
   - 3 Intercity Routes (Colombo–Kandy, Colombo–Galle, Colombo–Ella)
   - 16 Route Stops & 2 Boarding Points
   - 2 Luxury Buses & 2 Drivers
   - 3 Scheduled Services
   - 40 Bus Seats (configured via `SeatLayouts`)

### Method 2: EF Core CLI
```bash
dotnet ef database update --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API
```

### Method 3: Idempotent SQL Script Export
To export the complete PostgreSQL DDL without running .NET:
```bash
dotnet ef migrations script --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API --output database/schema.sql --idempotent
```

---

## 5. Seeded Schema Reference

The database includes **35 public tables** categorized across the four student functional domains:

| Category | Tables | Assigned Student |
| :--- | :--- | :--- |
| **Identity & Security** | `Users`, `Roles`, `AuditLogs`, `PassengerProfiles`, `OperatorProfiles` | Shared Foundation |
| **Journey & Catalogue** | `Routes`, `RouteStops`, `BoardingPoints`, `TouristDestinations`, `JourneySearches`, `JourneyCandidates` | Student 1 (Sethum) |
| **Fleet & Operations** | `Buses`, `Drivers`, `DriverAssignments`, `SeatLayouts`, `Seats`, `Amenities`, `ServiceAmenities`, `MaintenanceRecords` | Student 2 (Nuhadh) |
| **Booking & Payments** | `Bookings`, `Tickets`, `SeatHolds`, `PaymentAttempts`, `Refunds`, `FareRules` | Student 3 (Mithila) |
| **Disruptions & AI** | `DisruptionCases`, `RebookingProposals`, `ApprovalDecisions`, `ServiceAlerts`, `AiWorkflows`, `AiWorkflowSteps`, `AiToolCalls`, `AiValidationResults` | Student 4 (Dineth) |

---

## 6. Troubleshooting Railway Database Connections

| Symptom | Cause | Resolution |
| :--- | :--- | :--- |
| `Timeout during reading attempt at NpgsqlConnector.Authenticate` | Old driver using classic STARTTLS instead of Direct SSL | Upgrade to Npgsql 9.0+ and specify `SslNegotiation=Direct;` |
| `received direct SSL connection request without ALPN extension` | SSL handshake initiated without ALPN `postgresql` | Ensure `SslNegotiation.Direct` is set (which sends ALPN automatically) |
| `Connection timed out` (TCP SYN drops) | Local ISP or firewall filtering high ports (e.g., 30000+) | Test via mobile hotspot to verify if ISP is filtering non-standard ports |
| `Host does not exist` or connection refused | Railway regenerated proxy port upon restart | Check Railway **Postgres** → **Variables** → `DATABASE_PUBLIC_URL` and update `.env` |
| TCP Proxy connects but no database response | TCP Proxy target port in Railway is not `5432` | In Railway **Postgres** → **Settings** → **Networking**, set TCP Proxy Target Port to `5432` |
