# WayPoint Database Module

This directory is designated for PostgreSQL database schemas, baseline reference scripts, and SQL seed exports.

## Authoritative Migrations Notice
As defined in `AGENTS.md` and `system-architecture.md`, **Entity Framework Core migrations** are the authoritative mechanism for relational database schema versioning:
- **Migration Location**: `backend/WayPoint.Infrastructure/Data/Migrations/`
- **DbContext Definition**: `backend/WayPoint.Infrastructure/Data/WayPointDbContext.cs`
- **Data Seeder**: `backend/WayPoint.Infrastructure/Data/DbSeeder.cs`

## Generating SQL Scripts from Migrations
To generate an idempotent raw SQL migration script for PostgreSQL database execution without running .NET CLI:
```bash
dotnet ef migrations script --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API --output database/schema.sql --idempotent
```

## Running Migrations Locally
```bash
dotnet ef database update --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API
```
Or use the built-in CLI seed argument on the Web API:
```bash
dotnet run --project backend/WayPoint.API -- --seed
```
