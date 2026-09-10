# Student 2 Implementation Guide: Fleet, Seat & Resource Feasibility

- **Assigned Student**: **Nuhadh** (Student 2)
- **Component**: **Component 2 — Fleet, Seat & Resource Feasibility**
- **Core Domain Focus**: Bus fleet inventory, custom visual seat layouts, driver assignments, vehicle maintenance, and real-time seat availability calculation.
- **Assigned Feature Branch**: `feature/fleet-feasibility`

---

## 1. Executive Component Overview

As the owner of **Component 2**, you manage physical transport assets and operational capacity. Your work allows operators to configure bus seat maps, track vehicle availability, assign drivers without schedule conflicts, and compute real-time seat matrices across departures for both the web workspace and the mobile passenger seat picker.

### Assigned User Stories
- `US-PASS-003` (Interactive Seat Selection)
- `US-OP-002` (Fleet & Driver Resource Management)
- `US-OP-003` (Disruption Logging & Resource Feasibility Check)

---

## 2. Local Setup & Environment Checklist

1. **Environment Configuration**:
   ```bash
   cp .env.example .env
   ```
   - Ensure `DATABASE_URL` points to your active PostgreSQL instance.
   - Authoritative API base URL: `http://localhost:5010/api/v1` (`ASPNETCORE_URLS=http://localhost:5010`).
   - Web development server connects via `VITE_API_URL=http://localhost:5010/api/v1`.
   - Mobile app connects via `FLUTTER_API_URL=http://localhost:5010/api/v1`.
2. **Restore & Seed Database**:
   ```bash
   dotnet restore backend/WayPoint.sln
   dotnet run --project backend/WayPoint.API -- --seed
   ```
3. **Branch Workflow**:
   ```bash
   git checkout -b feature/fleet-feasibility
   ```

---

## 3. Design System & UI Contract (`docs/design/DESIGN.md`)

All UI screens must strictly comply with [`docs/design/DESIGN.md`](docs/design/DESIGN.md):
- **Brand Tokens**: Lanka Blue (`#0056D2`), Sunset Amber (`#FEB300`), Jungle Green (`#005312`), Surface (`#F8F9FA` / `#FFFFFF`).
- **Typography Pairing**: **Plus Jakarta Sans** (headings) and **Inter** (body, tabular rosters, and seat grids).
- **Reusable Primitives**:
  - Web: Use `Button`, `Card`, and `TransitBadge` in `web/src/components/ui/`.
  - Mobile: Use `WayPointButton`, `WayPointCard`, and `TransitBadge` in `mobile/lib/core/widgets/`.

---

## 4. Google Stitch UI Screen Specifications

Reference your assigned pre-designed screens in [`docs/design/stitch-screens-index.md`](docs/design/stitch-screens-index.md):

| Screen Code | Screen Title | Stitch Screen ID | Platform | Target File |
| :--- | :--- | :--- | :--- | :--- |
| **MOB-05** | Interactive Seat Picker & 10m Hold | `f823b1bf16b54795972e08b4d0813d5d` | Mobile | `mobile/lib/features/fleet/screens/seat_picker_screen.dart` |
| **MOB-10** | Conductor QR Boarding Scanner | `4e6e32a8846441249f406afad5c51eef` | Mobile | `mobile/lib/features/fleet/screens/conductor_scanner_screen.dart` |
| **MOB-11** | Conductor Passenger Manifest Roster | `c38c34595fa64b978deeb6f2db6c035d` | Mobile | `mobile/lib/features/fleet/screens/conductor_manifest_screen.dart` |
| **WEB-05** | Bus Fleet & Visual Seat Layout Matrix | `85fdc0e3b77c40e6877f1a994776abdd` | Web | `web/src/features/fleet/FleetMatrixBuilderPage.jsx` |
| **WEB-06** | Driver Roster & Rest Compliance Gantt | `4110147a3a9a47dfb81c2d710e450b70` | Web | `web/src/features/fleet/DriverRosteringPage.jsx` |

---

## 5. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Fleet` (`backend/WayPoint.Domain/Entities/Fleet/FleetEntities.cs`):

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`Bus`** | `Id`, `RegistrationNumber`, `BusClass`, `TotalSeatCapacity`, `SeatLayoutId`, `IsUnderMaintenance` | e.g., ND-5421 Luxury 40-seat bus |
| **`SeatLayout`** | `Id`, `Name`, `TotalRows`, `TotalColumns` | Grid template (e.g., Standard 2x2 Express 40 Seats) |
| **`Seat`** | `Id`, `SeatLayoutId`, `SeatNumber`, `RowIndex`, `ColumnIndex`, `SeatClass`, `RowVersion` | Individual seats; concurrency token enabled |
| **`Driver`** | `Id`, `FullName`, `LicenseNumber`, `PhoneNumber`, `Status` | Driver roster directory |
| **`DriverAssignment`** | `Id`, `DriverId`, `ServiceId`, `AssignedAt` | Maps driver to scheduled departure |
| **`MaintenanceRecord`** | `Id`, `BusId`, `Description`, `StartedAt`, `CompletedAt`, `Cost` | Vehicle maintenance log |
| **`Amenity`** & **`ServiceAmenity`** | `Id`, `Name`, `IconCode` | Bus amenities (AC, Wi-Fi, USB, Reclining) |

---

## 6. Backend Implementation Blueprint (`backend/`)

### 6.1 Controllers to Implement
Create these controllers under `backend/WayPoint.API/Controllers/`:
1. `BusController.cs` (`/api/v1/buses`):
   - `GET /api/v1/buses` (List bus fleet with maintenance status and layout details)
   - `POST /api/v1/buses` (`[Authorize(Roles = "Admin,TransportManager")]` - Register new bus)
   - `PUT /api/v1/buses/{id}/maintenance` (Toggle maintenance flag & log record)
2. `SeatLayoutController.cs` (`/api/v1/seats/layouts`):
   - `GET /api/v1/seats/layouts` (List seat layout templates)
   - `POST /api/v1/seats/layouts` (Define visual seat layout grid with row/column coordinates)
3. `DriverController.cs` (`/api/v1/drivers`):
   - `GET /api/v1/drivers` (List active drivers and upcoming schedule assignments)
   - `POST /api/v1/drivers` (Add driver with license validation)
   - `POST /api/v1/drivers/assign` (Assign driver to departure with overlap check)
4. `SeatAvailabilityController.cs` (`/api/v1/services/{id}/seats`):
   - `GET /api/v1/services/{id}/seats` (Calculate real-time seat availability map for service)

### 6.2 Complex Business Operation (Beyond CRUD)
- **Operation**: *Real-Time Seat Availability Matrix Aggregator & Resource Feasibility Solver*.
- **Logic**:
  1. Retrieve the bus's `SeatLayout` and complete list of `Seats` for the service.
  2. Query active `SeatHolds` where `ServiceId == serviceId` AND `HeldUntil > DateTime.UtcNow` AND `Status == Held`.
  3. Query confirmed `Bookings` where `ServiceId == serviceId` AND `Status != Cancelled`.
  4. Merge into a real-time matrix:
     - If Seat in active `SeatHolds` $\rightarrow$ Status = `Held`.
     - If Seat in confirmed `Bookings` $\rightarrow$ Status = `Booked`.
     - Otherwise $\rightarrow$ Status = `Available`.
  5. **Replacement Resource Feasibility**: In disruption scenarios, find all active buses not assigned to conflicting schedules with `SeatCapacity >= RequiredCapacity`.

---

## 7. Agentic AI Responsibilities (Student 2)

- **Assigned Agent**: **Resource Feasibility Agent** (`ADR-003`).
- **Domain Purpose**: Evaluates replacement bus inventory, driver availability schedules, seat capacity constraints, and resource conflict limits under operational disruptions.
- **Allow-Listed Tools**:
  - `CheckSeatAvailability` / `CheckReplacementResources`
  - `CheckTransferFeasibility`
- **Safety Rule**: AI cannot directly modify vehicle maintenance status or driver rosters without validated backend execution.

---

## 8. Testing Requirements

The comprehensive Testing Suite (Phase 4) is fully implemented across the stack:

1. **Backend Unit Tests (xUnit + Moq)**:
   - **Coverage**: Driver schedule overlap detection (`DriverOverlapDetectionTests.cs`), seat layout matrix and coordinate bounds validation (`SeatLayoutValidationTests.cs`), real-time seat availability aggregation (`SeatMatrixGeneratorTests.cs`), and resource feasibility constraints (`ResourceFeasibilityTests.cs`).
   - **How to run**: `dotnet test backend/WayPoint.Tests/WayPoint.Tests.csproj`

2. **Frontend Web Tests (React + Vitest)**:
   - **Coverage**: Seat layout designer grid dynamic scaling, seat type cycling, and layout saving validation (`SeatLayoutDesigner.test.jsx`).
   - **How to run**: Navigate to `web/` and run `npm run test`

3. **Mobile Tests (Flutter + bloc_test)**:
   - **Coverage**: Seat picker state machine transitions, interactive selection limits, and 10-minute hold initiation countdowns (`seat_picker_bloc_test.dart`).
   - **How to run**: Navigate to `mobile/` and run `flutter test test/features/fleet/seat_picker_bloc_test.dart`

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Concurrency & Double-Booking Protection**: How the `RowVersion` bytea token on `Seats` prevents race conditions.
- **Driver Rest-Hour Validation (`BR-RESOURCE-002`)**: Explain how your code verifies that a driver has at least 8 hours off-duty between consecutive service arrivals and departures.
- **Dynamic Seat Map Construction**: Walk through how your backend takes 1D seat entities and maps them into a 2D matrix for Flutter and React.
