# WayPoint Team Responsibilities & Component Ownership Specification

This document defines the ownership boundaries for the **four (4) primary business components** of the **WayPoint** platform, assigned across **Student 1**, **Student 2**, **Student 3**, and **Student 4** in accordance with **SE3090 Assignment 1** requirements.

---

## Executive Ownership Summary

| Component ID | Component Title | Assigned Owner | Core Business Focus |
| :--- | :--- | :--- | :--- |
| **Component 1** | **Journey Planning & Route Catalogue** | **Student 1** | Route networks, intermediate stops, timetables, tourist corridors, journey search & candidate generation. |
| **Component 2** | **Fleet, Seat & Resource Feasibility** | **Student 2** | Bus fleet inventory, seat map templates, driver scheduling, maintenance, and resource replacement feasibility. |
| **Component 3** | **Booking, Ticketing & Passenger Options** | **Student 3** | Temporary seat holds, payment sandbox checkout, QR e-tickets, cancellations, and refund processing. |
| **Component 4** | **Disruption, Rebooking & Approval** | **Student 4** | Disruption logging, passenger impact, multi-agent AI rebooking, alerts, and Transport Manager approval. |

---

## 1. Component 1: Journey Planning & Route Catalogue (Student 1)

### 1.1 Business Purpose
Provides the foundational intercity transit network infrastructure. Enables operators to define routes, intermediate stops, timetables, and tourist corridor destinations. Powers the core journey search engine to compute feasible direct and connecting travel options for Sri Lankan transit corridors.

### 1.2 Main User Stories
- `US-PASS-002` (Intercity Journey Search & Preferences)
- `US-OP-001` (Route & Timetable Administration)

### 1.3 Main Backend Responsibilities (ASP.NET Core)
- Implement `RouteController`, `StopController`, `ServiceController`, and `JourneySearchController`.
- Develop deterministic journey candidate generator algorithm evaluating direct routes and intermediate transfer stops.
- Implement connecting transfer feasibility logic enforcing the minimum 20-minute transfer window.
- Implement preference-aware scoring engine (fare, duration, arrival time, amenities).

### 1.4 Main PostgreSQL Responsibilities
- Design schemas, Entity Framework Core entities, and migrations for `Routes`, `RouteStops`, `BoardingPoints`, `TouristDestinations`, `Services`, and `FareRules`.
- Define composite indexes on `(OriginStopId, DestinationStopId, DepartureTime)` for fast candidate search.

### 1.5 Main React Responsibilities
- Build CRUD management views for intercity routes, stops, boarding points, and tourist destinations.
- Develop timetable scheduling interface for managing bus departures and frequency.
- Build search and filtering interface for browsing active services.

### 1.6 Main Flutter Responsibilities
- Develop passenger Journey Search screen with origin, destination, date/time, passenger count, and preference inputs.
- Build Preference Filter bottom sheet (AC, Wi-Fi, arrival deadline, direct bus).
- Build Journey Comparison Card widgets displaying departure, arrival, total fare, travel duration, and amenities.

### 1.7 Main Agentic AI Responsibilities
- Own and implement the **Journey Planner / Journey Analysis Agent**.
- Agent responsibility: Decomposes complex passenger travel objectives, analyzes route structures, and identifies compatible route/service candidate combinations.
- Implement tool integrations for allow-listed tools: `SearchRoutes` / `SearchServices` and `GetBoardingPoints` / `GetTimetable`.

### 1.8 API Endpoints (Minimum 4 Endpoints)
1. `GET /api/v1/routes` (List & filter intercity routes)
2. `POST /api/v1/routes` (Create new route & stop sequence)
3. `GET /api/v1/services` (List scheduled service departures)
4. `POST /api/v1/journeys/search` (Search & rank candidate journeys)

### 1.9 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Preference-Aware Candidate Journey & Transfer Window Generator*. Computes multi-leg connecting routes, evaluates transfer window feasibility, and scores options based on passenger preferences.

### 1.10 Unit Tests
- Test route distance and fare calculation formulas.
- Test candidate journey ranking algorithm under various passenger preference weightings.

### 1.11 Integration Tests
- Test PostgreSQL candidate journey search queries against seed route data.
- Test transfer window rejection logic for connecting services under 20 minutes.

### 1.12 Frontend Tests
- React: Unit tests for Route Creation Form validation.
- Flutter: Widget tests for Journey Search Screen and Preference Filter selection.

### 1.13 AI Evaluation Responsibilities
- Conduct plan assertion and route compatibility tests for the Journey Analysis Agent.

### 1.14 Documentation Responsibilities
- Author Journey Engine technical report section; contribute to `ADR-01` (React State) and `ADR-02` (Flutter State).

### 1.15 GitHub Evidence
- Own feature branch `feature/journey-planning`, associated GitHub Issues, PR code reviews, and commit log.

---

## 2. Component 2: Fleet, Seat & Resource Feasibility (Student 2)

### 2.1 Business Purpose
Manages physical transport assets, including bus fleet inventory, driver assignments, maintenance schedules, and custom seat matrix templates. Evaluates real-time seat availability and computes replacement resource feasibility during operational disruptions.

### 2.2 Main User Stories
- `US-PASS-003` (Interactive Seat Selection)
- `US-OP-002` (Fleet & Driver Resource Management)
- `US-OP-003` (Disruption Logging & Resource Feasibility Check)

### 2.3 Main Backend Responsibilities (ASP.NET Core)
- Implement `BusController`, `SeatLayoutController`, `DriverController`, and `ResourceFeasibilityController`.
- Build real-time seat availability engine cross-referencing seat map templates with active DB seat holds and bookings.
- Develop replacement resource feasibility solver checking unassigned buses, seat capacity constraints, and driver rest hours.

### 2.4 Main PostgreSQL Responsibilities
- Design schemas and EF Core migrations for `Buses`, `SeatLayouts`, `Seats`, `Drivers`, `DriverAssignments`, and `MaintenanceRecords`.
- Define unique constraints preventing double-assignment of drivers to overlapping departure schedules.

### 2.5 Main React Responsibilities
- Build Bus Fleet CRUD management interface with maintenance status toggles.
- Develop interactive visual **Seat Template Matrix Builder** (e.g., 2x2, 2x1 luxury layouts).
- Build Driver Roster and assignment management grid.

### 2.6 Main Flutter Responsibilities
- Build interactive visual **Bus Seat Picker Map widget** rendering seat layouts with real-time status indicators (`Available`, `Held`, `Booked`).
- Build seat selection state handler displaying active hold countdown.

### 2.7 Main Agentic AI Responsibilities
- Own and implement the **Resource Feasibility Agent**.
- Agent responsibility: Evaluates replacement bus inventory, driver availability schedules, seat capacity constraints, and resource conflict limits under disruption scenarios.
- Implement tool integrations for allow-listed tools: `CheckSeatAvailability` / `CheckReplacementResources` and `CheckTransferFeasibility`.

### 2.8 API Endpoints (Minimum 4 Endpoints)
1. `GET /api/v1/buses` (List bus fleet inventory & maintenance status)
2. `POST /api/v1/seats/layouts` (Define visual seat layout template)
3. `GET /api/v1/drivers` (Manage drivers & schedule assignments)
4. `GET /api/v1/services/{id}/seats` (Calculate real-time seat availability map)

### 2.9 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Replacement Resource Feasibility Evaluation Operation*. Evaluates unassigned fleet resources, seat capacity matching, and driver rest hours to determine replacement feasibility for disrupted services.

### 2.10 Unit Tests
- Test driver schedule overlap detection algorithm.
- Test seat layout matrix generator and seat capacity summation logic.

### 2.11 Integration Tests
- Test real-time seat status aggregation against active `SeatHolds` and `Bookings` in PostgreSQL.
- Test replacement resource feasibility query under simulated fleet shortages.

### 2.12 Frontend Tests
- React: Unit tests for Seat Template Builder component.
- Flutter: Widget tests for interactive Bus Seat Picker map rendering and seat selection.

### 2.13 AI Evaluation Responsibilities
- Conduct tool selection and constraint assertion tests for the Resource Feasibility Agent.

### 2.14 Documentation Responsibilities
- Author Fleet & Resource Feasibility technical report section; contribute to `ADR-04` (AI State Schema).

### 2.15 GitHub Evidence
- Own feature branch `feature/fleet-feasibility`, associated GitHub Issues, PR code reviews, and commit log.

---

## 3. Component 3: Booking, Ticketing & Passenger Options (Student 3)

### 3.1 Business Purpose
Handles passenger seat reservations, temporary hold timeouts, payment sandbox checkout transactions, digital QR e-ticket issuance, passenger cancellations, and refund eligibility processing.

### 3.2 Main User Stories
- `US-PASS-003` (Temporary Seat Hold)
- `US-PASS-004` (Payment Sandbox Checkout & QR E-Ticket Issuance)
- `US-PASS-005` (Booking Cancellation & Refund Request)

### 3.3 Main Backend Responsibilities (ASP.NET Core)
- Implement `BookingController`, `SeatHoldController`, `PaymentController`, `TicketController`, and `CancellationController`.
- Implement `IDbContextTransaction` concurrency locks for temporary seat holds and booking confirmations.
- Build Payment Sandbox gateway integration service (`FR-BE-06`).
- Build digital QR code cryptography/signing service and cancellation refund policy engine.

### 3.4 Main PostgreSQL Responsibilities
- Design schemas and EF Core migrations for `SeatHolds`, `Bookings`, `Tickets`, `PaymentAttempts`, and `Refunds`.
- Configure concurrency tokens (`[ConcurrencyCheck]`) on seat status fields to prevent race condition double-booking.

### 3.5 Main React Responsibilities
- Build Booking Search, Filter, and History management tables for operators.
- Build Payment Attempts and failure monitoring dashboard.
- Build Passenger Manifest view with boarding verification status.

### 3.6 Main Flutter Responsibilities
- Build Payment Checkout screen with payment sandbox integration.
- Build **Digital E-Ticket Wallet** screen displaying active and historical e-tickets.
- Build offline-ready **QR Code Display Screen** for ticket verification at boarding.

### 3.7 Main Agentic AI Responsibilities
- Own and implement the **Booking Options Agent**.
- Agent responsibility: Evaluates bookable journey alternatives, fare difference calculations, seat availability rules, and booking cancellation/refund policy outcomes.
- Implement tool integrations for allow-listed tools: `CalculateFareDifference` / `CheckCancellationPolicy` and `SendPassengerNotification`.

### 3.8 API Endpoints (Minimum 4 Endpoints)
1. `POST /api/v1/bookings/hold` (Reserve temporary seat hold with countdown)
2. `POST /api/v1/payments/confirm-sandbox-charge` (Execute transactional payment & booking confirmation)
3. `GET /api/v1/tickets/{id}` (Retrieve digital QR e-ticket payload)
4. `POST /api/v1/bookings/cancel` (Calculate refund eligibility & cancel booking)

### 3.9 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Transactional Seat Hold & Payment Confirmation Operation*. Uses `IDbContextTransaction` to validate seat holds, process payment sandbox authorization, lock seats, issue QR tickets, and clear holds inside a single database transaction.

### 3.10 Unit Tests
- Test cancellation policy refund percentage calculation logic (>24h, 12-24h, <12h).
- Test QR code payload encryption and signature verification algorithm.

### 3.11 Integration Tests
- Test database transaction rollback on payment sandbox failure.
- Test concurrent seat hold requests (verify 1 success, 1 HTTP 409 conflict).

### 3.12 Frontend Tests
- React: Unit tests for Booking History table filtering and manifest views.
- Flutter: Widget tests for E-Ticket Wallet and QR Code renderer screens.

### 3.13 AI Evaluation Responsibilities
- Conduct fare difference calculation and policy compliance assertion tests for the Booking Options Agent.

### 3.14 Documentation Responsibilities
- Author Booking & Ticketing technical report section; contribute to `ADR-01` & `ADR-02`.

### 3.15 GitHub Evidence
- Own feature branch `feature/booking-ticketing`, associated GitHub Issues, PR code reviews, and commit log.

---

## 4. Component 4: Disruption, Rebooking & Approval (Student 4)

### 4.1 Business Purpose
Manages service disruption events, logs passenger impacts, orchestrates multi-agent AI rebooking proposals, sends passenger alerts, and enforces the Transport Manager approval boundary for high-impact operational changes.

### 4.2 Main User Stories
- `US-PASS-006` (Disruption Rebooking Response)
- `US-OP-003` (Disruption Logging & Feasibility)
- `US-MGR-001` (Disruption Review & Before/After Impact Inspection)
- `US-MGR-002` (Manager Approval Execution)
- `US-MGR-003` (Agent Execution Summary & Observability Inspection)

### 4.3 Main Backend Responsibilities (ASP.NET Core)
- Implement `DisruptionController`, `RebookingController`, `ApprovalController`, and `AiWorkflowController`.
- Build Multi-Agent AI Workflow Coordinator executing the multi-step disruption recovery process.
- Implement Approval State Machine gating high-impact actions in `PendingManagerApproval` state.
- Build transactional rebooking application engine executing approved remedies.

### 4.4 Main PostgreSQL Responsibilities
- Design schemas and EF Core migrations for `ServiceAlerts`, `DisruptionCases`, `RebookingProposals`, `ApprovalDecisions`, `AiWorkflows`, `AiWorkflowSteps`, and `AiToolCalls`.
- Configure foreign key relationships connecting disruptions to affected services, bookings, and audit records.

### 4.5 Main React Responsibilities
- Build Disruption Logging Workbench for dispatchers.
- Build **Manager Approval Workbench** displaying before/after operational impacts, affected passenger metrics, validation summaries, and `Approve`/`Reject`/`Revise` controls.
- Build **AI Agent Execution Summary Monitor** displaying step execution timelines, tool logs, timings, retries, and safe-failure states.

### 4.6 Main Flutter Responsibilities
- Build In-App Disruption Alert banner and detailed disruption view.
- Build Passenger Rebooking Response screen allowing passengers to accept alternative journey proposals or select full refund.

### 4.7 Main Agentic AI Responsibilities
- Own and implement the **Validation & Safety Agent** and overall Multi-Agent Workflow Coordinator.
- Agent responsibility: Validates proposed rebooking remedies against business rules, classifies operational impact severity, enforces human manager approval boundaries, and ensures safe failure.
- Implement tool integrations for allow-listed tools: `CreateRebookingProposal`, `CalculatePassengerImpact`, `RequestManagerApproval`, and `ApplyApprovedOperationalChange`.

### 4.8 API Endpoints (Minimum 4 Endpoints)
1. `POST /api/v1/disruptions` (Log service disruption case & impact)
2. `POST /api/v1/rebooking/generate-proposal` (Initiate multi-agent AI rebooking proposal)
3. `GET /api/v1/approvals/pending` (Retrieve pending high-impact approval queue)
4. `POST /api/v1/approvals/{id}/decision` (Execute Transport Manager approval/rejection decision)

### 4.9 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Multi-Agent Disruption Rebooking & Manager Approval Boundary Operation*. Coordinates 4 agents to analyze disruptions, generate validated rebooking remedies, gate high-impact changes in `PendingManagerApproval`, and transactionally apply approved changes.

### 4.10 Unit Tests
- Test operational impact classification logic (Low-Impact vs High-Impact).
- Test manager approval state machine transition assertions.

### 4.11 Integration Tests
- Test full end-to-end disruption workflow: Disruption Log → AI Plan → Pending Manager Approval → Manager Approve → DB Transaction → Passenger Ticket Update.
- Test safe-failure fallback execution on simulated AI execution error.

### 4.12 Frontend Tests
- React: Unit tests for Manager Approval Workbench decision buttons and impact metric cards.
- Flutter: Widget tests for Disruption Alert banner and Rebooking Acceptance screen.

### 4.13 AI Evaluation Responsibilities
- Lead overall Agentic AI Evaluation Suite (golden test cases, plan assertions, approval enforcement, prompt injection resistance, safe failure tests).

### 4.14 Documentation Responsibilities
- Author Disruption & AI Approval technical report section; lead `ADR-03` (AI Framework) and `ADR-04` (AI State Schema).

### 4.15 GitHub Evidence
- Own feature branch `feature/disruption-approval`, associated GitHub Issues, PR code reviews, and commit log.

---

## 5. Cross-Stack Responsibility Matrix

To guarantee that no team member works in isolation, every student MUST contribute meaningfully across all required technologies:

| Required Technology Layer | Student 1 (Journey) | Student 2 (Fleet) | Student 3 (Booking) | Student 4 (Disruption) |
| :--- | :--- | :--- | :--- | :--- |
| **ASP.NET Core Web API** | Route, Stop, Service, Search Controllers & Ranking logic. | Bus, Seat Layout, Driver, Resource Controllers. | Booking, Seat Hold, Payment, Ticket Controllers. | Disruption, Approval, AI Workflow Controllers. |
| **PostgreSQL & EF Core** | `Routes`, `Stops`, `Services`, `FareRules` tables & search indexes. | `Buses`, `SeatLayouts`, `Seats`, `Drivers` tables & constraints. | `SeatHolds`, `Bookings`, `Tickets`, `Payments` tables & DB locks. | `Disruptions`, `Approvals`, `AiWorkflows`, `AiToolCalls` tables. |
| **React Web Application** | Route/Stop CRUD views & Timetable admin. | Fleet CRUD, visual Seat Template Builder, Driver Grid. | Booking history tables, Manifest views, Payment dashboards. | Disruption Workbench, Manager Approval Workbench, AI Summary Monitor. |
| **Flutter Mobile Application** | Journey Search screen, Preference filters, Comparison cards. | Interactive Bus Seat Picker map widget. | Payment Sandbox checkout, E-Ticket Wallet, QR display. | Disruption Alert banner & Rebooking Response screen. |
| **Agentic AI Contribution** | Journey Planner / Analysis Agent (`SearchRoutes` tool). | Resource Feasibility Agent (`CheckReplacementResources` tool). | Booking Options Agent (`CalculateFareDifference` tool). | Validation & Safety Agent & Coordinator (`RequestManagerApproval` tool). |
| **Testing Suite** | Route calculation & candidate search unit/integration tests. | Seat matrix & resource availability unit/integration tests. | Transactional hold & payment confirmation unit/integration tests. | End-to-End workflow, approval boundary, & AI evaluation tests. |
| **Documentation & ADRs** | Route Engine report section; `ADR-01` & `ADR-02` input. | Fleet & Resource report section; `ADR-04` input. | Booking & Ticketing report section; `ADR-01` & `ADR-05` input. | Disruption & AI report section; Lead `ADR-03` & `ADR-04`. |

---

## 6. Shared System Responsibilities (Cross-Cutting Infrastructure)

To prevent component isolation, the team shares joint responsibility for system-wide infrastructure:

1. **Shared Authentication & JWT Middleware**: All members implement JWT token validation and role-based policy enforcement (`[Authorize]`) across their respective controllers.
2. **Shared Database Context (`WayPointDbContext`)**: All members configure entity mappings, relationships, audit fields (`CreatedAt`/`UpdatedAt`), and EF Core migrations within the unified DbContext.
3. **Shared Design System & Layouts**:
   - React: Joint responsibility for base app layout, sidebar navigation, theme tokens, and protected route wrappers.
   - Flutter: Joint responsibility for app theme (`ThemeData`), navigation bar, HTTP client wrapper (`Dio`/`http`), and secure storage service.
4. **Shared Agent Tool Runner Infrastructure**: Shared execution wrapper for validating tool DTO inputs, invoking backend services, and logging tool traces into `AiToolCall` database tables.
5. **Cross-Platform End-to-End Workflow Test**: Joint responsibility for creating and executing the mandatory E2E workflow test: Flutter Search → ASP.NET Core API → PostgreSQL → Agentic AI Execution → React Manager Approval → ASP.NET Core Transaction → Flutter Status Update.
6. **CI/CD & Cloud Deployment**: Joint responsibility for maintaining `.github/workflows/ci.yml`, Docker configurations, and live cloud deployment verification.
