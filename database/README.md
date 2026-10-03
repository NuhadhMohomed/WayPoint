# WayPoint Database Module

This directory is designated for PostgreSQL database schemas, baseline reference scripts, and SQL seed exports.

## Authoritative Migrations Notice
As defined in `AGENTS.md` and `system-architecture.md`, **Entity Framework Core migrations** are the authoritative mechanism for relational database schema versioning:
- **Migration Location**: `backend/WayPoint.Infrastructure/Data/Migrations/`
- **DbContext Definition**: `backend/WayPoint.Infrastructure/Data/WayPointDbContext.cs`
- **Data Seeder**: `backend/WayPoint.Infrastructure/Data/DbSeeder.cs`

## Contents
- **`schema_baseline.sql`**: Full idempotent DDL script representing the PostgreSQL relational model generated from authoritative EF Core migrations.
- **`seed/reference_data.sql`**: Baseline SQL seed data containing core system roles, standard transit amenities, primary scenic corridors, and express route stops.

## Generating SQL Scripts from Migrations
To regenerate the idempotent raw SQL baseline script from migrations:
```bash
dotnet ef migrations script --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API --output database/schema_baseline.sql --idempotent
```

## Running Migrations Locally
```bash
dotnet ef database update --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API
```
Or use the built-in CLI seed argument on the Web API:
```bash
dotnet run --project backend/WayPoint.API -- --seed
```
