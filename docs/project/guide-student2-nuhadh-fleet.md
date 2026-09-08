# Student 2 Implementation Guide: Fleet, Seat & Resource Feasibility

- **Assigned Student**: **Nuhadh** (Student 2)
- **Component**: **Component 2 — Fleet, Seat & Resource Feasibility**
- **Core Domain Focus**: Bus fleet inventory, custom visual seat layouts, driver assignments, vehicle maintenance, and real-time seat availability calculation.

---

## 1. Executive Component Overview

As the owner of **Component 2**, you manage physical transport assets and operational capacity. Your work allows operators to configure bus seat maps, track vehicle availability, assign drivers without schedule conflicts, and compute real-time seat matrices across departures for both the web workspace and the mobile passenger seat picker.

### Assigned User Stories
- `US-PASS-003` (Interactive Seat Selection)
- `US-OP-002` (Fleet & Driver Resource Management)
- `US-OP-003` (Disruption Logging & Resource Feasibility Check)

---

## 2. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Fleet` (`backend/WayPoint.Domain/Entities/Fleet/FleetEntities.cs`):

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`Bus`** | `Id`, `RegistrationNumber`, `BusClass`, `TotalSeatCapacity`, `SeatLayoutId`, `IsUnderMaintenance` | e.g., ND-8821, Luxury class, 44 seats |
| **`SeatLayout`** | `Id`, `Name`, `TotalRows`, `TotalColumns` | Grid template (e.g., 2x2 standard, 2x1 luxury) |
| **`Seat`** | `Id`, `SeatLayoutId`, `SeatNumber`, `RowIndex`, `ColumnIndex`, `SeatClass`, `RowVersion` | Individual seats; concurrency token enabled |
| **`Driver`** | `Id`, `FullName`, `LicenseNumber`, `PhoneNumber`, `Status` | Driver directory |
| **`DriverAssignment`** | `Id`, `DriverId`, `ServiceId`, `AssignedAt` | Maps driver to scheduled departure |
| **`MaintenanceRecord`** | `Id`, `BusId`, `Description`, `StartedAt`, `CompletedAt`, `Cost` | Vehicle service log |
| **`Amenity`** & **`ServiceAmenity`** | `Id`, `Name`, `IconCode` | Bus amenities (AC, Wi-Fi, USB, Reclining) |

---

## 3. Backend Implementation Blueprint (`backend/`)

### 3.1 Controllers to Implement
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
   - `POST /api/v1/drivers/assign` (Assign driver to service departure with overlap check)
4. `SeatAvailabilityController.cs` (`/api/v1/services/{id}/seats`):
   - `GET /api/v1/services/{id}/seats` (Calculate real-time seat availability map for service)

### 3.2 Complex Business Operation (Beyond CRUD)
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

## 4. Frontend Web Implementation Blueprint (`web/`)

- **Folder Location**: `web/src/features/fleet/`
- **API Client**: `fleetApi.js` (wraps `/api/v1/buses`, `/api/v1/seats/layouts`, `/api/v1/drivers`)
- **Key Views to Build**:
  1. `FleetInventoryView.jsx`: Table of buses, bus classes (Standard, Luxury, SemiLuxury), and maintenance toggles.
  2. `SeatLayoutBuilderView.jsx`: Interactive visual grid builder allowing dispatchers to create 2x2 or 2x1 seat templates and tag VIP/Window/Aisle seats.
  3. `DriverRosterView.jsx`: Calendar/table of driver assignments with rest-hour validation badges.

---

## 5. Mobile Flutter Implementation Blueprint (`mobile/`)

- **Folder Location**: `mobile/lib/features/fleet/`
- **Key Widgets & Screens**:
  1. `SeatPickerScreen.dart`: Visual representation of the bus interior rendering driver cabin, rows, aisle gap, and individual seats.
  2. `SeatWidget.dart`: Dynamic seat button colored by state (Green: Available, Yellow: Held, Gray: Booked, Purple: Selected).
  3. `SeatLegendWidget.dart`: Clarifies seat types (Window, Aisle, VIP) and pricing surcharges.
- **BLoC State Management**:
  - Events: `LoadSeatMapEvent`, `SelectSeatEvent`, `DeselectSeatEvent`.
  - States: `SeatMapLoading`, `SeatMapLoaded`, `SeatMapSelectionUpdated`, `SeatMapError`.

---

## 6. Testing Requirements

1. **Unit Tests (`WayPoint.Tests/FleetTests.cs`)**:
   - Test driver schedule overlap detection algorithm (ensuring no double-booking of drivers within departure/arrival window).
   - Test seat layout matrix generator and seat coordinate validation.
2. **Integration Tests**:
   - Test real-time seat availability calculation against concurrently active `SeatHold` and `Booking` records in PostgreSQL.

---

## 7. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Concurrency & Double-Booking Protection**: How the `RowVersion` bytea token on `Seats` prevents race conditions.
- **Driver Rest-Hour Validation (`BR-RESOURCE-002`)**: Explain how your code verifies that a driver has at least 8 hours off-duty between consecutive service arrivals and departures.
- **Dynamic Seat Map Construction**: Walk through how your backend takes 1D seat entities and maps them into a 2D matrix for Flutter and React.
