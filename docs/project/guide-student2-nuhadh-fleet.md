# Student 2 Implementation Guide: Fleet, Seat & Resource Feasibility

- **Assigned Student**: **Nuhadh** (Student 2)
- **Component**: **Component 2 — Fleet, Seat & Resource Feasibility**
- **Core Domain Focus**: Bus fleet inventory, seat map matrix templates, driver scheduling, maintenance status, and replacement resource feasibility.
- **Assigned Feature Branch**: `feature/fleet-feasibility`
- **Architecture**: **Headless API-First Architecture** ([ADR-006](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-006-headless-architecture.md))

---

## 1. Executive Component Overview

As the owner of **Component 2**, you manage physical transport assets across the transit network: bus fleet specifications, driver rosters and compliance, vehicle maintenance, and dynamic seat layout templates. Your component provides the real-time seat inventory engine that computes seat availability (`Available`, `Held`, `Booked`) and acts as the resource feasibility solver for AI disruption recovery.

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
   - Swagger Documentation: `http://localhost:5010/swagger`.
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

## 3. Headless API Contract & OpenAPI Specification

All endpoints must be documented in OpenAPI/Swagger (`/swagger`):
- **Seat Matrix Serialization**: Serializes 2D grid coordinates (`rowIndex`, `columnIndex`, `seatNumber`, `seatClass`, `status`).
- **Concurrency Protection**: Strong concurrency token handling on seat reservation attempts to prevent double-booking.
- **Driver Rest Invariants**: Rest period validations between scheduled departures (`BR-RESOURCE-002`).

---

## 4. Authoritative Request & Response DTO Specifications

### 4.1 Seat Layout Definition DTO (`CreateSeatLayoutDto`)
```json
{
  "name": "Standard 2x2 Express",
  "totalRows": 10,
  "totalColumns": 4,
  "seats": [
    { "seatNumber": "1A", "rowIndex": 1, "columnIndex": 1, "seatClass": "Standard" },
    { "seatNumber": "1B", "rowIndex": 1, "columnIndex": 2, "seatClass": "Standard" },
    { "seatNumber": "1C", "rowIndex": 1, "columnIndex": 3, "seatClass": "Standard" },
    { "seatNumber": "1D", "rowIndex": 1, "columnIndex": 4, "seatClass": "Standard" }
  ]
}
```

### 4.2 Real-Time Seat Availability Response DTO (`ServiceSeatAvailabilityDto`)
```json
{
  "serviceId": "e1a90c12-3456-789a-bcde-f0123456789a",
  "busRegistration": "ND-5421",
  "busClass": "Luxury",
  "totalCapacity": 40,
  "availableCount": 24,
  "heldCount": 4,
  "bookedCount": 12,
  "seatGrid": [
    {
      "seatNumber": "1A",
      "row": 1,
      "col": 1,
      "status": "Available",
      "price": 2400.0
    },
    {
      "seatNumber": "1B",
      "row": 1,
      "col": 2,
      "status": "Held",
      "heldUntil": "2026-10-15T08:10:00Z"
    },
    {
      "seatNumber": "2A",
      "row": 2,
      "col": 1,
      "status": "Booked"
    }
  ]
}
```

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
Controllers reside under `backend/WayPoint.API/Controllers/`:
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

1. **Backend Unit & Integration Tests (xUnit + Moq)**:
   - Driver schedule overlap detection (`DriverOverlapDetectionTests.cs`).
   - Seat layout matrix and coordinate bounds validation (`SeatLayoutValidationTests.cs`).
   - Real-time seat availability aggregation (`SeatMatrixGeneratorTests.cs`).
   - Resource feasibility constraints (`ResourceFeasibilityTests.cs`).
   - **How to run**: `dotnet test backend/WayPoint.Tests/WayPoint.Tests.csproj`

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Concurrency & Double-Booking Protection**: How the `RowVersion` bytea token on `Seats` prevents race conditions.
- **Driver Rest-Hour Validation (`BR-RESOURCE-002`)**: Explain how your code verifies that a driver has at least 8 hours off-duty between consecutive service arrivals and departures.
- **Dynamic Seat Map Construction**: Walk through how your backend aggregates 1D seat database records into a structured 2D grid matrix.

## 10. Reviews & Ratings Module Extension
You are also responsible for the Driver & Bus Reviews/Ratings feature (`FR-REVIEW-001` to `FR-REVIEW-006`):
- **7-Day Review Window**: The backend blocks updates after 7 days but permits deletion.
- **Anonymous Reviews**: `PassengerName` is automatically masked as "Anonymous Passenger" when `IsAnonymous` is true.
- **Profanity Filter**: `SanitizeComment` prevents inappropriate language before persisting to PostgreSQL.
