# WayPoint Database Module

> **Status: Active Development (WIP)**  
> Database schemas and seed scripts are actively evolving alongside new domain features.

This directory holds reference PostgreSQL DDL exports and baseline SQL seed scripts for **WayPoint**.

## Authoritative Schema Versioning

Per [AGENTS.md](../AGENTS.md), **Entity Framework Core 9 migrations** in the backend are the single source of truth for schema changes:
- **Migrations**: `backend/WayPoint.Infrastructure/Data/Migrations/`
  - `20260907093703_InitialCreate`
  - `20260911152247_AddReviewEntities`
  - `20261003094506_AddAuditLogHashAndNotifications`
  - `20261003101503_AddAccountLockoutAndJourneyLegs`
- **DbContext**: `backend/WayPoint.Infrastructure/Data/WayPointDbContext.cs`
- **Programmatic Seeder**: `backend/WayPoint.Infrastructure/Data/DbSeeder.cs`

## Contents

- `schema_baseline.sql` — Idempotent PostgreSQL DDL script exported from EF Core migrations.
- `seed/reference_data.sql` — Baseline SQL inserts for core roles, transit amenities, sample Sri Lankan express routes (`EX-01`, `EX-02`, `EX-03`), stops, and a `Standard 2x2 Express` seat layout.

## Common Commands

### Apply Migrations & Seed via Backend CLI
```bash
dotnet run --project backend/WayPoint.API -- --seed
```

### Apply Migrations with EF Core CLI
```bash
dotnet ef database update --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API
```

### Regenerate `schema_baseline.sql` After Schema Changes
```bash
dotnet ef migrations script --project backend/WayPoint.Infrastructure --startup-project backend/WayPoint.API --output database/schema_baseline.sql --idempotent
```
