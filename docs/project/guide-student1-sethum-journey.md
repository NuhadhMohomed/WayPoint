# Student 1 Implementation Guide: Journey Planning & Route Catalogue

- **Assigned Student**: **Sethum** (Student 1)
- **Component**: **Component 1 — Journey Planning & Route Catalogue**
- **Core Domain Focus**: Intercity route networks, intermediate stops, timetables, tourist destinations, and journey candidate generation.
- **Assigned Feature Branch**: `feature/journey-planning`

---

## 1. Executive Component Overview

As the owner of **Component 1**, you are responsible for the foundational transit infrastructure of WayPoint. Your work enables operators to manage routes, stops, and scheduled departures on the web, and enables passengers to search and compare feasible direct and connecting travel options across Sri Lankan corridors (e.g., Colombo–Ella, Colombo–Kandy, Colombo–Galle) on mobile.

### Assigned User Stories
- `US-PASS-002` (Intercity Journey Search & Preferences)
- `US-OP-001` (Route & Timetable Administration)

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
   git checkout -b feature/journey-planning
   ```

---

## 3. Design System & UI Contract (`docs/design/DESIGN.md`)

All UI screens must strictly comply with [`docs/design/DESIGN.md`](docs/design/DESIGN.md):
- **Brand Tokens**: Lanka Blue (`#0056D2`), Sunset Amber (`#FEB300`), Jungle Green (`#005312`), Surface (`#F8F9FA` / `#FFFFFF`).
- **Typography Pairing**: **Plus Jakarta Sans** (headings) and **Inter** (body, tabular timetables, and prices in LKR).
- **Reusable Primitives**:
  - Web: Use `Button`, `Card`, and `TransitBadge` in `web/src/components/ui/`.
  - Mobile: Use `WayPointButton`, `WayPointCard`, and `TransitBadge` in `mobile/lib/core/widgets/`.

---

## 4. Google Stitch UI Screen Specifications

Reference your assigned pre-designed screens in [`docs/design/stitch-screens-index.md`](docs/design/stitch-screens-index.md):

| Screen Code | Screen Title | Stitch Screen ID | Platform | Target File |
| :--- | :--- | :--- | :--- | :--- |
| **MOB-02** | Journey Search, Corridors & Dates | `4baf1853d7a14d7abd597916567b5370` | Mobile | `mobile/lib/features/journey/screens/journey_search_screen.dart` |
| **MOB-03** | Preference Filter Sheet & Sliders | `fb4b74ea904c435b93f05e9dc324e989` | Mobile | `mobile/lib/features/journey/widgets/preference_filter_sheet.dart` |
| **MOB-04** | Journey Comparison Cards & Buffer | `aa124497024b48a3adc01888fed1a5a3` | Mobile | `mobile/lib/features/journey/screens/journey_comparison_screen.dart` |
| **WEB-02** | Route & Intermediate Stop Manager | `387e0fc877bb4f919e190d4c80b8ca56` | Web | `web/src/features/journey/RouteManagerPage.jsx` |
| **WEB-03** | Tourist Corridor Destination Showcase | `b5395deeca344413844a9396e1ad0ace` | Web | `web/src/features/journey/TouristCorridorsPage.jsx` |
| **WEB-04** | Timetable & Service Departure Scheduler | `4b4d5f6dbac54679beebab9ca5585e07` | Web | `web/src/features/journey/ServiceSchedulerPage.jsx` |

---

## 5. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Journey` (`backend/WayPoint.Domain/Entities/Journey/JourneyEntities.cs`):

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`Route`** | `Id`, `RouteCode`, `OriginCity`, `DestinationCity`, `TotalDistanceKm`, `IsActive` | e.g., RT-01 Colombo–Kandy, EX-08 Colombo–Ella |
| **`RouteStop`** | `Id`, `RouteId`, `StopName`, `SequenceOrder`, `ArrivalOffsetMinutes`, `DistanceFromOriginKm` | Enforces sequence ordering (`OriginSeq < DestinationSeq`) |
| **`BoardingPoint`**| `Id`, `RouteId`, `PointName`, `Landmark`, `Latitude`, `Longitude` | Pickup locations with GPS coordinates |
| **`TouristDestination`** | `Id`, `RouteId`, `AttractionName`, `Description`, `ImageUrl` | Highlights (e.g., Nine Arch Bridge, Little Adam's Peak) |
| **`Service`** | `Id`, `ServiceCode`, `RouteId`, `BusId`, `DriverId`, `DepartureTime`, `ArrivalTime`, `BaseFare`, `Status` | Scheduled bus departures |
| **`FareRule`** | `Id`, `ServiceId`, `BusClass`, `RatePerKm`, `ClassMultiplier` | Distance and class segment pricing |
| **`JourneySearch`** | `Id`, `PassengerId`, `OriginCity`, `DestinationCity`, `TravelDate`, `PassengerCount`, `SearchedAt` | Query history tracking |
| **`JourneyCandidate`** | `Id`, `JourneySearchId`, `CandidateType` (`Direct`/`Connecting`), `TotalFare`, `TotalDurationMinutes`, `MatchScore` | Calculated journey options |

---

## 6. Backend Implementation Blueprint (`backend/`)

### 6.1 Controllers to Implement
Create these controllers under `backend/WayPoint.API/Controllers/`:
1. `RouteController.cs` (`/api/v1/routes`):
   - `GET /api/v1/routes` (List & filter routes by city, active status)
   - `GET /api/v1/routes/{id}` (Get route details with ordered stops & boarding points)
   - `POST /api/v1/routes` (`[Authorize(Roles = "Admin,TransportManager")]` - Create route with stops)
   - `PUT /api/v1/routes/{id}` (`[Authorize(Roles = "Admin,TransportManager")]` - Update route)
2. `ServiceController.cs` (`/api/v1/services`):
   - `GET /api/v1/services` (Filter departures by route, date, status)
   - `GET /api/v1/services/{id}` (Retrieve service departure details, assigned bus, driver)
   - `POST /api/v1/services` (`[Authorize(Roles = "Admin,TransportManager,Operator")]` - Schedule new departure)
3. `JourneySearchController.cs` (`/api/v1/journeys`):
   - `POST /api/v1/journeys/search` (Search & rank feasible direct & connecting journeys)

### 6.2 Complex Business Operation (Beyond CRUD)
- **Operation**: *Preference-Aware Candidate Journey & Transfer Window Generator*.
- **Logic**:
  1. Search for **Direct Services** matching `OriginCity` and `DestinationCity` on the requested date.
  2. Search for **Connecting Services** via intermediate transfer hubs (e.g., Colombo $\rightarrow$ Kandy and Kandy $\rightarrow$ Ella).
  3. **Strict Transfer Validation (`BR-TRANSFER-001`)**: `Leg2.DepartureTime - Leg1.ArrivalTime >= 20 minutes`. Reject any connection with $< 20$ minutes transfer buffer.
  4. **Scoring Engine**: Score candidate journeys (0.00 to 1.00) based on passenger preferences: early arrival bonus, AC bus multiplier, and lower total fare.

---

## 7. Agentic AI Responsibilities (Student 1)

- **Assigned Agent**: **Journey Planner / Journey Analysis Agent** (`ADR-003`).
- **Domain Purpose**: Decomposes complex travel inquiries (e.g., "fastest route to Ella with AC") into viable route combinations.
- **Allow-Listed Tools**:
  - `SearchRoutes` / `SearchServices`
  - `GetBoardingPoints` / `GetTimetable`
- **Safety Rule**: AI cannot alter scheduled service timetables; output must be structured JSON validated against backend candidate rules.

---

## 8. Testing Requirements

1. **Unit Tests (`WayPoint.Tests/JourneyTests.cs`)**:
   - Test segment fare calculation formula (`BaseFare + (Distance * RatePerKm) * ClassMultiplier`).
   - Test candidate journey ranking algorithm under various passenger preference weightings.
2. **Integration Tests**:
   - Query test verifying connecting services with $<20$ min transfer buffer are rejected with proper status.
   - Database test validating ordered stop sequence retrieval (`OriginSeq < DestinationSeq`).

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **LINQ Query Optimization**: How your journey search avoids N+1 queries using `.Include(s => s.Route).ThenInclude(r => r.Stops)`.
- **Transfer Window Enforcement**: Point directly to the line of code enforcing `(leg2.Departure - leg1.Arrival).TotalMinutes >= 20`.
- **Sequence Order Integrity**: Explain why `OriginStop.SequenceOrder < DestinationStop.SequenceOrder` is validated server-side.
