# Student 1 Implementation Guide: Journey Planning & Route Catalogue

- **Assigned Student**: **Sethum** (Student 1)
- **Component**: **Component 1 — Journey Planning & Route Catalogue**
- **Core Domain Focus**: Intercity route networks, intermediate stops, timetables, tourist destinations, and journey candidate generation.

---

## 1. Executive Component Overview

As the owner of **Component 1**, you are responsible for the foundational transit infrastructure of WayPoint. Your work enables operators to manage routes, stops, and scheduled services on the web, and enables passengers to search and compare feasible travel options across Sri Lankan corridors (e.g., Colombo–Ella, Colombo–Kandy, Colombo–Galle) on mobile.

### Assigned User Stories
- `US-PASS-002` (Intercity Journey Search & Preferences)
- `US-OP-001` (Route & Timetable Administration)

---

## 2. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Journey` (`backend/WayPoint.Domain/Entities/Journey/JourneyEntities.cs`):

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`Route`** | `Id`, `RouteNumber`, `OriginCity`, `DestinationCity`, `EstimatedDurationMinutes`, `IsActive` | e.g., Route EX-01 (Colombo to Galle via Southern Expressway) |
| **`RouteStop`** | `Id`, `RouteId`, `StopName`, `SequenceOrder`, `ArrivalOffsetMinutes`, `DistanceFromOriginKm` | Enforces sequence ordering (`OriginSeq < DestinationSeq`) |
| **`BoardingPoint`**| `Id`, `RouteId`, `PointName`, `Landmark`, `Latitude`, `Longitude` | Pickup locations with GPS coordinates |
| **`TouristDestination`** | `Id`, `RouteId`, `AttractionName`, `Description`, `ImageUrl` | Highlights (e.g., Nine Arch Bridge, Sigiriya) |
| **`Service`** | `Id`, `ServiceCode`, `RouteId`, `BusId`, `DriverId`, `DepartureTime`, `ArrivalTime`, `BaseFare`, `Status` | Scheduled bus departures |
| **`FareRule`** | `Id`, `ServiceId`, `BusClass`, `RatePerKm`, `ClassMultiplier` | Distance and class segment pricing |
| **`JourneySearch`** | `Id`, `PassengerId`, `OriginCity`, `DestinationCity`, `TravelDate`, `PassengerCount`, `SearchedAt` | Query history tracking |
| **`JourneyCandidate`** | `Id`, `JourneySearchId`, `CandidateType` (`Direct`/`Connecting`), `TotalFare`, `TotalDurationMinutes`, `MatchScore` | Calculated journey options |

---

## 3. Backend Implementation Blueprint (`backend/`)

### 3.1 Controllers to Implement
Create these controllers under `backend/WayPoint.API/Controllers/`:
1. `RouteController.cs` (`/api/v1/routes`):
   - `GET /api/v1/routes` (List & filter routes by city, active status)
   - `GET /api/v1/routes/{id}` (Get route details with ordered stops & boarding points)
   - `POST /api/v1/routes` (`[Authorize(Roles = "Admin,TransportManager")]` - Create route with stops)
   - `PUT /api/v1/routes/{id}` (`[Authorize(Roles = "Admin,TransportManager")]` - Update route)
2. `ServiceController.cs` (`/api/v1/services`):
   - `GET /api/v1/services` (Filter departures by route, date, status)
   - `GET /api/v1/services/{id}` (Retrieve service departure details, assigned bus, driver)
   - `POST /api/v1/services` (`[Authorize(Roles = "Admin,TransportManager,Operator")]` - Schedule new service departure)
3. `JourneySearchController.cs` (`/api/v1/journeys`):
   - `POST /api/v1/journeys/search` (Search & rank feasible direct & connecting journeys)

### 3.2 Complex Business Operation (Beyond CRUD)
- **Operation**: *Preference-Aware Candidate Journey & Transfer Window Generator*.
- **Logic**:
  1. Search for **Direct Services** matching `OriginCity` and `DestinationCity` on the requested date.
  2. Search for **Connecting Services** via intermediate transfer hubs (e.g., Colombo $\rightarrow$ Kandy and Kandy $\rightarrow$ Ella).
  3. **Strict Transfer Validation (`BR-TRANSFER-001`)**: `Leg2.DepartureTime - Leg1.ArrivalTime >= 20 minutes`. Reject any connection with $< 20$ minutes transfer window.
  4. **Scoring Engine**: Score candidate journeys (0.00 to 1.00) based on passenger preferences: early arrival bonus, AC bus multiplier, and lower total fare.

---

## 4. Frontend Web Implementation Blueprint (`web/`)

- **Folder Location**: `web/src/features/journey/`
- **API Client**: `journeyApi.js` (wraps `/api/v1/routes`, `/api/v1/services`)
- **Key Views to Build**:
  1. `RouteManagementView.jsx`: Table of active routes, stop sequence viewer, and route modal dialog.
  2. `ServiceSchedulerView.jsx`: Timetable scheduler for operators to assign departure/arrival times, buses, and drivers.
  3. `TouristCorridorView.jsx`: Showcase cards for tourist destinations linked to Sri Lankan transit corridors.

---

## 5. Mobile Flutter Implementation Blueprint (`mobile/`)

- **Folder Location**: `mobile/lib/features/journey/`
- **Key Widgets & Screens**:
  1. `JourneySearchScreen.dart`: Origin and destination city dropdowns, date picker, passenger count stepper.
  2. `PreferenceFilterSheet.dart`: Bottom sheet toggling AC required, Wi-Fi, direct bus only, arrival before noon.
  3. `JourneyCard.dart`: Displays departure time, arrival time, total duration, fare in LKR, transfer hub badge, and match score.
- **BLoC State Management**:
  - Events: `SearchJourneysEvent`, `ApplyFiltersEvent`, `SelectJourneyEvent`.
  - States: `JourneySearchInitial`, `JourneySearchLoading`, `JourneySearchLoaded`, `JourneySearchEmpty`, `JourneySearchError`.

---

## 6. Testing Requirements

1. **Unit Tests (`WayPoint.Tests/JourneyTests.cs`)**:
   - Test segment fare calculation formula (`BaseFare + (Distance * RatePerKm) * ClassMultiplier`).
   - Test candidate journey ranking algorithm under various passenger preference weightings.
2. **Integration Tests**:
   - Query test verifying connecting services with $<20$ min buffer are rejected.
   - Database test validating ordered stop sequence retrieval.

---

## 7. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **LINQ Query Optimization**: How your journey search avoids N+1 queries using `.Include(s => s.Route).ThenInclude(r => r.Stops)`.
- **Transfer Window Enforcement**: Point directly to the line of code enforcing `(leg2.Departure - leg1.Arrival).TotalMinutes >= 20`.
- **Sequence Order Integrity**: Explain why `OriginStop.SequenceOrder < DestinationStop.SequenceOrder` is validated server-side.
