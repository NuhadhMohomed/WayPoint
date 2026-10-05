# Student 1 Implementation Guide: Journey Planning & Route Catalogue

- **Assigned Student**: **Sethum** (Student 1)
- **Component**: **Component 1 — Journey Planning & Route Catalogue**
- **Core Domain Focus**: Intercity route networks, intermediate stops, timetables, tourist destinations, and journey candidate generation.
- **Assigned Feature Branch**: `feature/journey-planning`
- **Architecture**: **Headless API-First Architecture** ([ADR-006](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-006-headless-architecture.md))

---

## 1. Executive Component Overview

As the owner of **Component 1**, you are responsible for the foundational transit infrastructure of WayPoint. Your work enables transit operators to administer routes, stops, and scheduled departures via authoritative RESTful endpoints, and enables external client applications (mobile apps, booking platforms) to query, search, and rank feasible direct and connecting travel options across Sri Lankan transit corridors (e.g., Colombo–Ella, Colombo–Kandy, Colombo–Galle).

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

## 3. Headless API Contract & OpenAPI Specification

All endpoints must be thoroughly annotated for OpenAPI/Swagger documentation (`/swagger`):
- **Response Format**: Standard JSON with PascalCase/camelCase serialization compliance.
- **Status Codes**: `200 OK` for lookups, `201 Created` for route definitions, `400 Bad Request` for invalid coordinates/stop sequences, `404 Not Found` for nonexistent route codes, `409 Conflict` for overlapping timetable slots.
- **Currency Format**: All fares rendered in Sri Lankan Rupees (LKR / Rs.).

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

- **Assigned Agent**: **Journey Planner / Journey Analysis Agent** (`ADR-003`).
- **Domain Purpose**: Decomposes complex travel inquiries (e.g., "fastest route to Ella with AC") into viable route combinations.
- **Allow-Listed Tools**:
  - `SearchRoutes` / `SearchServices`
  - `GetBoardingPoints` / `GetTimetable`
- **Safety Rule**: AI cannot alter scheduled service timetables; output must be structured JSON validated against backend candidate rules.

---

## 8. Testing Requirements

1. **Unit Tests (`backend/WayPoint.Tests/JourneyTests.cs`)**:
   - Test segment fare calculation formula (`BaseFare + (Distance * RatePerKm) * ClassMultiplier`).
   - Test candidate journey ranking algorithm under various passenger preference weightings.
2. **Integration Tests**:
   - Query test verifying connecting services with $<20$ min transfer buffer are rejected with proper status (`BR-TRANSFER-001`).
   - Database test validating ordered stop sequence retrieval (`OriginSeq < DestinationSeq`).

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **LINQ Query Optimization**: How your journey search avoids N+1 queries using `.Include(s => s.Route).ThenInclude(r => r.Stops)`.
- **Transfer Window Enforcement**: Point directly to the line of code enforcing `(leg2.Departure - leg1.Arrival).TotalMinutes >= 20`.
- **Sequence Order Integrity**: Explain why `OriginStop.SequenceOrder < DestinationStop.SequenceOrder` is validated server-side.
