# Sri Lanka Transit Data Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Seed and migrate comprehensive, realistic, and non-duplicate transit data for the WayPoint system covering all Sri Lankan provinces, major expressways, intercity highway corridors, intermediate stops, boarding points with GPS coordinates, tourist attractions, operators, buses, drivers, scheduled services, fare rules, disruptions, and reviews.

**Architecture:** 
1. Refactor `DbSeeder.cs` from monolithic `if (!await context.Table.AnyAsync())` gates to granular, idempotent `AnyAsync(x => x.Key == ...)` guards that safely add new Sri Lankan records without modifying or duplicating existing records.
2. Expand `database/seed/reference_data.sql` with comprehensive baseline SQL scripts utilizing `ON CONFLICT DO NOTHING` for all entities.
3. Execute the migration against the live PostgreSQL database via `dotnet run --project backend/WayPoint.API -- --seed` and verify zero duplicates and complete coverage.

**Tech Stack:** ASP.NET Core 8, Entity Framework Core 8, Npgsql PostgreSQL, C# 12.

**Spec:** User request: "Migrate appropriate new data to db covering everything based on Sri lanka.Do not enter repeat data".

## Global Constraints

- Never duplicate existing records (RT-01, EX-02, EX-08, ND-5421, NC-8890, admin/manager/operator/passenger users).
- Every new insert must be strictly idempotent (guarded by unique keys or `ON CONFLICT DO NOTHING`).
- All distances, durations, GPS coordinates, bus plate registrations, NIC numbers, and route codes must reflect authentic Sri Lankan geography and transport naming conventions.
- All fares must be in realistic Sri Lankan Rupees (LKR).
- Database migrations must remain consistent and all 87 existing unit/integration tests must pass.

## Review Focus

1. **Idempotent Seeding:** Running `--seed` twice consecutively must not throw primary key/unique constraint violations and must not duplicate rows.
2. **Entity Foreign Keys:** All new `Service`, `RouteStop`, `BoardingPoint`, `TouristDestination`, `ServiceAmenity`, and `FareRule` records must cleanly bind to valid parent entities without null reference or foreign key constraint exceptions.
3. **Seat Generation:** Adding new `SeatLayout` records must generate matching `Seat` items with valid Row/Column indices and `SeatClass` without duplicating seats for existing layouts.
4. **Time & Date Offsets:** Scheduled service departure/arrival timestamps must be computed relative to `DateTime.UtcNow.Date.AddDays(1)` so search queries for upcoming trips always return valid active services.
5. **Database Sync:** The live PostgreSQL database must be verified with record counts confirming all 9 provinces and 15 key corridors are populated.

---

### Task 1: Refactor and Expand `DbSeeder.cs` with Granular Idempotency

**Files:**
- Modify: `backend/WayPoint.Infrastructure/Data/DbSeeder.cs`

**Interfaces:**
- Consumes: `WayPointDbContext`, `IPasswordHasher`, Domain Entities (`Route`, `Bus`, `Driver`, `Service`, `User`, `SeatLayout`, etc.)
- Produces: `DbSeeder.SeedAsync(WayPointDbContext context, IPasswordHasher passwordHasher)`

- [ ] **Step 1: Inspect and update Roles, Users, and Regional Operator Profiles**
  - Check existing roles, add if missing with `RoleName` check.
  - Check existing users by `Email`. Add new regional operators (`operator.central@waypoint.lk`, `operator.southern@waypoint.lk`, `operator.northern@waypoint.lk`, `operator.eastern@waypoint.lk`, `operator.superline@waypoint.lk`, `operator.ransilu@waypoint.lk`, `operator.ncg@waypoint.lk`) and new passengers across provinces.

- [ ] **Step 2: Update SeatLayouts and Seat Generation**
  - Check existing layouts by `Name`.
  - Add `Super Luxury VIP Semi-Sleeper (30 Seats)` (10 rows, 3 cols: 2x1), `High-Capacity Intercity (48 Seats)` (12 rows, 4 cols: 2x2), and `Executive Airport Shuttle (28 Seats)` (7 rows, 4 cols: 2x2) with corresponding `Seat` records if not already existing.

- [ ] **Step 3: Update Transit Amenities**
  - Check each amenity by `Name` before adding.
  - Add 8 new amenities: Overhead Luggage Compartment, Reading Lights, Live GPS Tracking, Bottled Water, Onboard Entertainment, First Aid Kit, Blanket & Pillow, Wheelchair Ramp.

- [ ] **Step 4: Update Fleet of Buses and Drivers**
  - Check each bus by `RegistrationNumber` before adding.
  - Add 14 provincial buses (`WP ND-6012`, `WP NC-3314`, `WP NB-7721`, `CP ND-1185`, `CP NC-9204`, `SP ND-4402`, `SP NC-5581`, `NP NB-8840`, `NP ND-2230`, `EP NC-7119`, `NW ND-5533`, `WP NA-1090`, `SG ND-3918`, `NC ND-8022`).
  - Check each driver by `LicenseNumber` before adding.
  - Add 10 regional drivers across Central, Southern, Northern, Eastern, and Western provinces.

- [ ] **Step 5: Update Routes, RouteStops, BoardingPoints, and TouristDestinations**
  - Check each route by `RouteCode` before adding.
  - Add 12 new routes covering Sri Lanka's expressway network and national highways (EX-01, EX-03, EX-04, EX-05, RT-04, RT-15, RT-17, RT-42, RT-48, RT-49, RT-57, RT-79) with their complete stops, GPS-located boarding points, and regional tourist attractions.

- [ ] **Step 6: Update Scheduled Services, FareRules, and ServiceAmenities**
  - Check each service by `ServiceCode` before adding.
  - Create services covering morning, afternoon, evening, and night trips for the expanded routes.
  - Attach `FareRule` records with realistic LKR distance rates and bus class multipliers.
  - Attach `ServiceAmenity` links for each service.

- [ ] **Step 7: Update DisruptionCases, ServiceAlerts, and Reviews**
  - Check disruptions and alerts to ensure no duplicates.
  - Add realistic Sri Lankan highway and mountain weather disruption cases and alerts.
  - Add authentic passenger bus and driver reviews for completed services.

---

### Task 2: Update Reference SQL Seed File `database/seed/reference_data.sql`

**Files:**
- Modify: `database/seed/reference_data.sql`

- [ ] **Step 1: Write idempotent SQL for all expanded reference entities**
  - Add SQL inserts for all 15 routes (`ON CONFLICT ("RouteCode") DO NOTHING`).
  - Add SQL inserts for all route stops (`ON CONFLICT DO NOTHING`).
  - Add SQL inserts for all amenities (`ON CONFLICT DO NOTHING`).
  - Add SQL inserts for all seat layouts (`ON CONFLICT DO NOTHING`).

---

### Task 3: Execute Database Migration & Seeding Against Live Database

- [ ] **Step 1: Build the backend solution to verify code compilation**
  - Run: `dotnet build backend/WayPoint.sln`
  - Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Run CLI Seed against the live PostgreSQL database**
  - Run: `dotnet run --project backend/WayPoint.API --no-build -- --seed`
  - Expected: "Applying migrations and seeding database from CLI... Database seeded successfully. Exiting CLI mode."

- [ ] **Step 3: Run CLI Seed a second time to verify idempotency (zero repeat data / zero conflicts)**
  - Run: `dotnet run --project backend/WayPoint.API --no-build -- --seed`
  - Expected: Succeeds cleanly without primary key or unique constraint errors.

---

### Task 4: Verification and Automated Testing

- [ ] **Step 1: Run full test suite**
  - Run: `dotnet test backend/WayPoint.Tests/WayPoint.Tests.csproj`
  - Expected: 87/87 tests PASS.

- [ ] **Step 2: Verify database record counts and integrity via DevController / diagnostic check**
  - Verify that routes count >= 15, buses >= 16, drivers >= 12, services >= 18, amenities >= 12, covering all major Sri Lankan transit hubs.
