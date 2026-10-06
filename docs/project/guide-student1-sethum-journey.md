# Student 1 Implementation Guide: Journey Planning & Route Catalogue

- **Assigned Student**: **Sethum** (Student 1)
- **Component**: **Component 1 — Journey Planning & Route Catalogue**
- **Core Domain Focus**: Intercity route networks, intermediate stops, timetables, tourist destinations, and journey candidate generation.
- **Assigned Feature Branch**: `feature/journey-planning`
- **Architecture**: **Integrated Full-Stack Architecture** ([SPEC-2026-10-05-FRONTEND-RECONSTRUCTION](../superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md))

---

## 1. Executive Component Overview

As the owner of **Component 1**, you are responsible for the foundational transit infrastructure of WayPoint. Your work spans:
1. **ASP.NET Core Web API**: Authoritative route directory, stop sequencing, timetable scheduling, and candidate journey generator.
2. **PostgreSQL Relational DB**: Schemas and EF Core migrations for `Routes`, `RouteStops`, `BoardingPoints`, `TouristDestinations`, `Services`, and `FareRules`.
3. **React Web Application (`web/src/features/journey/`)**: Operator interfaces for route network mapping, service scheduling, and tourist corridor tagging (`RouteManagerPage`, `ServiceSchedulerPage`, `TouristCorridorsPage`).
4. **Flutter Mobile Application (`mobile/lib/features/journey/`)**: Passenger journey search, preference filtering bottom sheet, and candidate comparison cards (`JourneySearchScreen`, `JourneyComparisonScreen`).
5. **Agentic AI**: Specialized **Journey Analysis Agent** with allow-listed tools (`SearchRoutes`, `GetBoardingPoints`, `CheckTransferFeasibility`).

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
   - Swagger Documentation: `http://localhost:5010/swagger`.
2. **Run Backend API**:
   ```bash
   dotnet restore backend/WayPoint.sln
   dotnet run --project backend/WayPoint.API -- --seed
   ```
3. **Run React Web Application**:
   ```bash
   cd web
   npm install
   npm run dev
   ```
4. **Run Flutter Mobile Application**:
   ```bash
   cd mobile
   flutter pub get
   flutter run
   ```

---

## 3. Client Presentation Tier Implementations

### 3.1 React Web Pages (`web/src/features/journey/`)
- **`RouteManagerPage.jsx`**: Interactive route catalogue with stop sequence management, distance/time offsets, and validation.
- **`ServiceSchedulerPage.jsx`**: Timetable scheduler linking routes to buses and drivers for scheduled departures.
- **`TouristCorridorsPage.jsx`**: Showcase of scenic Sri Lankan tourist corridors (e.g., Colombo–Ella, Colombo–Sigiriya).
- State: TanStack Query v5 queries with optimistic caching.

### 3.2 Flutter Mobile Screens (`mobile/lib/features/journey/`)
- **`JourneySearchScreen.dart`**: Multi-criteria journey search with origin/destination autocomplete, date selection, and preference filter sheet (AC, Wi-Fi, direct only).
- **`JourneyComparisonScreen.dart`**: Candidate comparison cards comparing duration, transfer buffer (`BR-TRANSFER-001`), total fare, and seat availability.
- State: `JourneySearchBloc` handling async search state streams.

---

## 4. Authoritative Request & Response DTO Specifications

### 4.1 Route Creation DTO (`CreateRouteDto`)
```json
{
  "routeCode": "EX-08",
  "originCity": "Colombo",
  "destinationCity": "Ella",
  "totalDistanceKm": 210.5,
  "stops": [
    { "stopName": "Colombo Fort", "sequenceOrder": 1, "arrivalOffsetMinutes": 0, "distanceFromOriginKm": 0 },
    { "stopName": "Kumbalwella", "sequenceOrder": 2, "arrivalOffsetMinutes": 240, "distanceFromOriginKm": 195.0 },
    { "stopName": "Ella Station", "sequenceOrder": 3, "arrivalOffsetMinutes": 270, "distanceFromOriginKm": 210.5 }
  ]
}
```

### 4.2 Journey Search Query & Candidate Response DTO (`JourneySearchResponseDto`)
```json
{
  "originCity": "Colombo",
  "destinationCity": "Ella",
  "travelDate": "2026-10-15",
  "candidateJourneys": [
    {
      "candidateType": "Connecting",
      "totalFare": 2400.0,
      "totalDurationMinutes": 310,
      "transferBufferMinutes": 25,
      "matchScore": 0.94,
      "legs": [
        { "serviceCode": "SRV-CLKDY-01", "origin": "Colombo", "destination": "Kandy", "departure": "06:00", "arrival": "09:00" },
        { "serviceCode": "SRV-KDYELA-04", "origin": "Kandy", "destination": "Ella", "departure": "09:25", "arrival": "11:10" }
      ]
    }
  ]
}
```

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
Controllers reside under `backend/WayPoint.API/Controllers/`:
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

- **Assigned Agent**: **Journey Analysis Agent** (`ai/agents/journey_agent.py`, `ADR-003`).
- **Domain Purpose**: Decomposes complex travel inquiries (e.g., "fastest route to Ella with AC") into viable route combinations.
- **Allow-Listed Tools**:
  - `SearchRoutes` / `SearchServices`
  - `GetBoardingPoints` / `GetTimetable`
  - `CheckTransferFeasibility`
- **Safety Rule**: AI cannot alter scheduled service timetables; output must be structured JSON validated against deterministic candidate rules (`BR-TRANSFER-001`).

---

## 8. Testing Requirements

1. **Unit & Integration Tests (`backend/WayPoint.Tests/JourneyTests.cs`)**:
   - Test segment fare calculation formula (`BaseFare + (Distance * RatePerKm) * ClassMultiplier`).
   - Test candidate journey ranking algorithm under various passenger preference weightings.
   - Query test verifying connecting services with $<20$ min transfer buffer are rejected with proper status (`BR-TRANSFER-001`).
   - Database test validating ordered stop sequence retrieval (`OriginSeq < DestinationSeq`).
   ```bash
   dotnet test backend/WayPoint.sln --filter "FullyQualifiedName~Journey"
   ```
2. **React Web Tests (`web/src/features/journey/__tests__/`)**:
   - Vitest component tests for `RouteManagerPage` form validation and stop sequencing.
   ```bash
   cd web && npm test
   ```
3. **Flutter Mobile Tests (`mobile/test/features/journey/`)**:
   - Widget tests for `journey_search_test.dart` and candidate comparison views.
   ```bash
   cd mobile && flutter test test/features/journey/
   ```

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **LINQ Query Optimization**: How your journey search avoids N+1 queries using `.Include(s => s.Route).ThenInclude(r => r.Stops)`.
- **Transfer Window Enforcement**: Point directly to the line of code enforcing `(leg2.Departure - leg1.Arrival).TotalMinutes >= 20`.
- **Sequence Order Integrity**: Explain why `OriginStop.SequenceOrder < DestinationStop.SequenceOrder` is validated server-side.
