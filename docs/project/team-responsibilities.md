# WayPoint Team Responsibilities & Component Ownership Specification

This document defines the ownership boundaries for the **four (4) primary business components** of the **WayPoint** platform, assigned across **Student 1**, **Student 2**, **Student 3**, and **Student 4** in accordance with **SE3090 Assignment 1** requirements.

Operating under an **Integrated Full-Stack Architecture** ([SPEC-2026-10-05-FRONTEND-RECONSTRUCTION](../superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md)), each student takes vertical ownership across the complete software stack: ASP.NET Core Web API, PostgreSQL database, React Web Application, Flutter Mobile Application, Agentic AI multi-agent workflows, and automated test suites.

---

## Executive Ownership Summary

| Component ID | Component Title | Assigned Owner | Core Business Focus |
| :--- | :--- | :--- | :--- |
| **Component 1** | **Journey Planning & Route Catalogue** | **Sethum** (Student 1) | Route networks, intermediate stops, timetables, tourist corridors, journey search, candidate generation, and Journey Analysis Agent. |
| **Component 2** | **Fleet, Seat & Resource Feasibility** | **Nuhadh** (Student 2) | Bus fleet inventory, 2D seat map designer, driver rostering, maintenance, replacement feasibility, and Resource Feasibility Agent. |
| **Component 3** | **Booking, Ticketing & Passenger Options** | **Mithila** (Student 3) | Temporary seat holds, payment sandbox checkout, QR e-tickets, manifest monitor, refunds, and Booking & Policy Agent. |
| **Component 4** | **Disruption, Rebooking & Approval** | **Dineth** (Student 4) | Disruption logging, passenger impact, multi-agent AI rebooking, alerts, Transport Manager approval gate, and Validation & Safety Agent. |

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
- Implement connecting transfer feasibility logic enforcing the minimum 20-minute transfer window (`BR-TRANSFER-001`).
- Implement preference-aware scoring engine (fare, duration, arrival time, amenities).

### 1.4 Main PostgreSQL Responsibilities
- Design schemas, Entity Framework Core entities, and migrations for `Routes`, `RouteStops`, `BoardingPoints`, `TouristDestinations`, `Services`, and `FareRules`.
- Define composite indexes on `(OriginStopId, DestinationStopId, DepartureTime)` for fast candidate search.

### 1.5 Main React Web Application Responsibilities (`web/`)
- Implement **`RouteManagerPage.jsx`**: Interactive route catalogue, drag/drop stop sequencer, distance & estimated duration calculator.
- Implement **`ServiceSchedulerPage.jsx`**: Service departure scheduler, recurring timetable generator, bus & driver schedule assignment.
- Implement **`TouristCorridorsPage.jsx`**: Scenic tourist destination showcase and promotional corridor tagger.
- State & networking: TanStack Query v5 hooks (`useRoutes`, `useServices`), Vitest component tests.

### 1.6 Main Flutter Mobile Application Responsibilities (`mobile/`)
- Implement **`JourneySearchScreen.dart`**: Multi-criteria journey search with origin/destination autocomplete, date pickers, and preference filter bottom sheet (AC, Wi-Fi, direct only).
- Implement **`JourneyComparisonScreen.dart`**: Candidate comparison cards displaying departure/arrival times, travel duration, connecting transfer warnings, total fares, and live seat counts.
- State & networking: `JourneySearchBloc` using event-driven state stream, Dio HTTP client, unit & widget tests.

### 1.7 Main REST API & Swagger Contract Responsibilities
- Define strongly-typed DTOs with validation rules for route creation, stop sequencing, and timetable scheduling.
- Author OpenAPI/Swagger documentation with request/response examples for journey queries and multi-stop lookups.
- Expose search filtering parameters (origin, destination, date, amenities, max transfers).

### 1.8 Main Agentic AI Responsibilities
- Own and implement the **Journey Analysis Agent** (`ai/agents/journey_agent.py`).
- Agent responsibility: Decomposes complex passenger travel objectives, analyzes route structures, and identifies compatible route/service candidate combinations.
- Implement tool integrations for allow-listed tools: `SearchRoutes` / `SearchServices`, `GetBoardingPoints` / `GetTimetable`, and `CheckTransferFeasibility`.

### 1.9 API Endpoints (Minimum 4 Endpoints)
1. `GET /api/v1/routes` (List & filter intercity routes)
2. `POST /api/v1/routes` (Create new route & stop sequence)
3. `GET /api/v1/services` (List scheduled service departures)
4. `POST /api/v1/journeys/search` (Search & rank candidate journeys)

### 1.10 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Preference-Aware Candidate Journey & Transfer Window Generator*. Computes multi-leg connecting routes, evaluates transfer window feasibility (`BR-TRANSFER-001 >= 20 mins`), and scores options based on passenger preferences.

### 1.11 Testing Responsibilities
- **Backend**: Unit tests for distance/fare formulas; integration tests for candidate search queries against PostgreSQL.
- **React Web**: Vitest component rendering, stop form validation, and routing tests.
- **Flutter Mobile**: Widget tests for `JourneySearchScreen` and candidate card rendering in `JourneyComparisonScreen`.
- **AI Evaluation**: Plan assertion and route compatibility tests for Journey Analysis Agent.

### 1.12 Documentation Responsibilities
- Author Journey Engine technical report section; contribute to `ADR-001` (React State), `ADR-002` (Flutter State), and `ADR-005` (Cloud Deployment).

### 1.13 GitHub Evidence
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

### 2.5 Main React Web Application Responsibilities (`web/`)
- Implement **`FleetMatrixBuilderPage.jsx`**: Bus fleet inventory, vehicle registration, luxury/standard class toggle, and maintenance status management.
- Implement **`SeatLayoutDesignerPage.jsx`**: Interactive 2D drag/grid seat map designer with row/column coordinates, aisle gaps, and accessible seat indicators.
- Implement **`DriverRosteringPage.jsx`**: Driver directory, license validation, departure assignment with rest-hour compliance enforcement.
- Implement **`FleetReviewsDashboardPage.jsx`**: Operational reviews dashboard, bus/driver sentiment scores, and rating distribution charts.
- State & networking: TanStack Query v5 hooks (`useBuses`, `useSeatLayouts`, `useDrivers`, `useReviews`), Vitest component tests.

### 2.6 Main Flutter Mobile Application Responsibilities (`mobile/`)
- Implement **`SeatPickerScreen.dart`**: Real-time 2D bus seat picker rendering available, held, and booked seats with interactive seat selection and 10-minute hold countdown timer (`BR-HOLD-001`).
- Implement **`ReviewSubmissionScreen.dart`**: Post-journey 5-star rating, punctuality & cleanliness tags, optional anonymous review mode, and comment submission.
- State & networking: `SeatPickerBloc` and `ReviewBloc` with Dio HTTP client, unit & widget tests.

### 2.7 Main REST API & Swagger Contract Responsibilities
- Define JSON schemas and DTOs representing 2D seat grid matrices (rows, columns, aisle positions, seat types, accessible seats).
- Provide real-time seat status aggregation endpoints (`Available`, `Held`, `Booked`, `Blocked`).
- Author OpenAPI specs for fleet inventory management, driver rostering, and review submission.

### 2.8 Main Agentic AI Responsibilities
- Own and implement the **Resource Feasibility Agent** (`ai/agents/resource_agent.py`).
- Agent responsibility: Evaluates replacement bus inventory, driver availability schedules, seat capacity constraints, and resource conflict limits under disruption scenarios.
- Implement tool integrations for allow-listed tools: `CheckSeatAvailability` / `CheckReplacementResources` and `CheckTransferFeasibility`.

### 2.9 API Endpoints (Minimum 4 Endpoints)
1. `GET /api/v1/buses` (List bus fleet inventory & maintenance status)
2. `POST /api/v1/seats/layouts` (Define visual seat layout template)
3. `GET /api/v1/drivers` (Manage drivers & schedule assignments)
4. `GET /api/v1/services/{id}/seats` (Calculate real-time seat availability map)
5. `POST /api/v1/reviews/buses` (Submit passenger bus review & rating)

### 2.10 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Replacement Resource Feasibility Evaluation Operation*. Evaluates unassigned fleet resources, seat capacity matching, and driver rest hours to determine replacement feasibility for disrupted services.

### 2.11 Testing Responsibilities
- **Backend**: Unit tests for driver schedule overlap detection and seat layout matrix generation; integration tests for seat availability aggregation and resource feasibility evaluation.
- **React Web**: Vitest component tests for `SeatLayoutDesignerPage` and `FleetMatrixBuilderPage`.
- **Flutter Mobile**: Widget tests for `SeatPickerScreen` and `ReviewSubmissionScreen`.
- **AI Evaluation**: Tool selection and constraint assertion tests for Resource Feasibility Agent.

### 2.12 Documentation Responsibilities
- Author Fleet & Resource Feasibility technical report section; contribute to `ADR-001` (React State), `ADR-002` (Flutter State), and `ADR-004` (AI State Persistence).

### 2.13 GitHub Evidence
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
- Implement `IDbContextTransaction` concurrency locks for temporary seat holds and booking confirmations (`FR-BOOKING-001`).
- Build Payment Sandbox gateway integration service.
- Build digital QR code cryptography/signing service (HMAC-SHA256) and cancellation refund policy engine (`BR-REFUND-001`).

### 3.4 Main PostgreSQL Responsibilities
- Design schemas and EF Core migrations for `SeatHolds`, `Bookings`, `Tickets`, `PaymentAttempts`, and `Refunds`.
- Configure concurrency tokens (`[ConcurrencyCheck]`) on seat status fields to prevent race condition double-booking.

### 3.5 Main React Web Application Responsibilities (`web/`)
- Implement **`OperatorDashboardPage.jsx`**: Departure summary boards, service occupancy KPIs, revenue analytics, and quick operational action links.
- Implement **`BookingManifestMonitorPage.jsx`**: Real-time passenger manifest table, boarding verification statuses, passenger name search, and CSV export.
- State & networking: TanStack Query v5 hooks (`useBookings`, `useManifest`), Zustand `manifestStore`, Vitest component tests.

### 3.6 Main Flutter Mobile Application Responsibilities (`mobile/`)
- Implement **`PaymentCheckoutScreen.dart`**: Payment sandbox checkout integration, card input validation, total fare breakdown, and instant booking confirmation.
- Implement **`TicketWalletScreen.dart`**: Digital ticket wallet rendering active e-tickets with HMAC-SHA256 cryptographically signed QR codes and offline access.
- Implement **`BookingHistoryScreen.dart`**: Passenger booking records, tiered cancellation refund calculation preview (`BR-REFUND-001`), and cancellation execution.
- State & networking: `TicketWalletBloc` and `CheckoutBloc` with Dio HTTP client, unit & widget tests.

### 3.7 Main REST API & Swagger Contract Responsibilities
- Define API request contracts for 10-minute hold reservation with countdown expiry timestamps (`HeldUntil`).
- Define digital ticket verification endpoints accepting HMAC-signed QR token payloads.
- Expose tiered refund calculation preview endpoints before cancellation execution.

### 3.8 Main Agentic AI Responsibilities
- Own and implement the **Booking & Policy Agent** (`ai/agents/booking_agent.py`).
- Agent responsibility: Evaluates bookable journey alternatives, fare difference calculations, seat availability rules, and booking cancellation/refund policy outcomes.
- Implement tool integrations for allow-listed tools: `CalculateFareDifference` / `CheckCancellationPolicy` and `SendPassengerNotification`.

### 3.9 API Endpoints (Minimum 4 Endpoints)
1. `POST /api/v1/bookings/hold` (Reserve temporary seat hold with countdown)
2. `POST /api/v1/payments/confirm-sandbox-charge` (Execute transactional payment & booking confirmation)
3. `GET /api/v1/tickets/{id}` (Retrieve digital QR e-ticket payload)
4. `POST /api/v1/bookings/cancel` (Calculate refund eligibility & cancel booking)

### 3.10 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Transactional Seat Hold & Payment Confirmation Operation*. Uses `IDbContextTransaction` to validate seat holds, process payment sandbox authorization, lock seats, issue QR tickets, and clear holds inside a single database transaction.

### 3.11 Testing Responsibilities
- **Backend**: Unit tests for tiered cancellation refund percentages; integration tests for transaction rollbacks on payment failure and concurrent seat holds (HTTP 409 conflict).
- **React Web**: Vitest component tests for `OperatorDashboardPage` and `BookingManifestMonitorPage`.
- **Flutter Mobile**: Widget tests for `PaymentCheckoutScreen` and `TicketWalletScreen`.
- **AI Evaluation**: Fare difference arithmetic and policy compliance assertion tests for Booking & Policy Agent.

### 3.12 Documentation Responsibilities
- Author Booking & Ticketing technical report section; contribute to `ADR-001` (React State), `ADR-002` (Flutter State), and `ADR-005` (Cloud Deployment).

### 3.13 GitHub Evidence
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
- Implement integration with the multi-agent disruption recovery workflow.
- Implement Approval State Machine gating high-impact actions in `PendingManagerApproval` state (`BR-APPROVAL-001`).
- Build transactional rebooking application engine executing approved remedies.

### 4.4 Main PostgreSQL Responsibilities
- Design schemas and EF Core migrations for `ServiceAlerts`, `DisruptionCases`, `RebookingProposals`, `ApprovalDecisions`, `AiWorkflows`, `AiWorkflowSteps`, and `AiToolCalls`.
- Configure foreign key relationships connecting disruptions to affected services, bookings, and audit records.

### 4.5 Main React Web Application Responsibilities (`web/`)
- Implement **`DisruptionIntakePage.jsx`**: Incident intake form, affected service tagging, candidate remedy review, and blast-radius impact analysis.
- Implement **`ManagerApprovalWorkbenchPage.jsx`**: Dedicated Transport Manager approval portal with before/after operational comparison, affected passenger metrics, and Approve / Reject / Request-Revision action controls (`BR-APPROVAL-001`).
- Implement **`AiObservabilityPage.jsx`**: Execution timeline dashboard rendering agent execution logs, tool invocation traces, execution latencies, and validation outcomes.
- Implement **`ServiceAlertBroadcastPage.jsx`**: Public and in-app service alert broadcaster.
- State & networking: TanStack Query v5 hooks (`useDisruptions`, `useApprovals`, `useAiWorkflows`), Zustand `disruptionStore`, Vitest component tests.

### 4.6 Main Flutter Mobile Application Responsibilities (`mobile/`)
- Implement **`DisruptionAlertScreen.dart`**: Prominent in-app alert banner and remediation screen alerting affected passengers and offering one-tap rebooking acceptance or instant refund requests.
- Implement **`ConductorScannerScreen.dart`**: Camera-based QR ticket scanner utilizing `mobile_scanner`, verifying cryptographic HMAC-SHA256 signatures, with haptic feedback on boarding.
- Implement **`ConductorManifestScreen.dart`**: Mobile passenger manifest with real-time boarding verification checklist.
- State & networking: `DisruptionBloc` and `ScannerBloc` with Dio HTTP client, unit & widget tests.

### 4.7 Main REST API & Swagger Contract Responsibilities
- Define structured endpoints for disruption event intake and blast-radius impact queries.
- Expose Transport Manager approval endpoints accepting cryptographic manager signatures and decision rationales.
- Provide AI execution observability endpoints exposing step logs, tool invocations, and timing metrics via JSON.

### 4.8 Main Agentic AI Responsibilities
- Own and implement the **Validation & Safety Agent** (`ai/agents/safety_agent.py`) and LangGraph orchestration graph (`ai/agents/graph.py`).
- Agent responsibility: Validates proposed rebooking remedies against business rules, classifies operational impact severity, enforces human manager approval boundaries, and ensures safe failure (`FR-AI-004`).
- Implement tool integrations for allow-listed tools: `CreateRebookingProposal`, `CalculatePassengerImpact`, `RequestManagerApproval`, and `ApplyApprovedOperationalChange`.

### 4.9 API Endpoints (Minimum 4 Endpoints)
1. `POST /api/v1/disruptions` (Log service disruption case & impact)
2. `POST /api/v1/rebooking/generate-proposal` (Initiate multi-agent AI rebooking proposal)
3. `GET /api/v1/approvals/pending` (Retrieve pending high-impact approval queue)
4. `POST /api/v1/approvals/{id}/decision` (Execute Transport Manager approval/rejection decision)

### 4.10 Business-Specific Operation Beyond Basic CRUD
- **Operation**: *Multi-Agent Disruption Rebooking & Manager Approval Boundary Operation*. Coordinates 4 agents to analyze disruptions, generate validated rebooking remedies, gate high-impact changes in `PendingManagerApproval`, and transactionally apply approved changes.

### 4.11 Testing Responsibilities
- **Backend**: Unit tests for operational impact classification (Low vs High) and manager approval state transitions; integration tests for the full disruption workflow.
- **React Web**: Vitest component tests for `ManagerApprovalWorkbenchPage` and `DisruptionIntakePage`.
- **Flutter Mobile**: Widget tests for `DisruptionAlertScreen` and `ConductorScannerScreen`.
- **AI Evaluation**: Lead the full Agentic AI Evaluation Suite (golden test cases, plan assertions, approval enforcement, prompt injection resistance, safe-failure tests).

### 4.12 Documentation Responsibilities
- Author Disruption & AI Approval technical report section; lead `ADR-003` (AI Framework), `ADR-004` (AI State Schema), and contribute to `ADR-001`, `ADR-002`, and `ADR-005`.

### 4.13 GitHub Evidence
- Own feature branch `feature/disruption-approval`, associated GitHub Issues, PR code reviews, and commit log.

---

## 5. Cross-Stack Responsibility Matrix

To guarantee that no team member works in isolation, every student contributes meaningfully across all core platform layers, satisfying the 70 individual marks rubric:

| Layer / Aspect | Student 1 (Journey) | Student 2 (Fleet) | Student 3 (Booking) | Student 4 (Disruption) |
| :--- | :--- | :--- | :--- | :--- |
| **ASP.NET Core API (10 M)** | Route, Stop, Service, Search Controllers & Ranking logic. | Bus, Seat Layout, Driver, Resource Controllers. | Booking, Seat Hold, Payment, Ticket Controllers. | Disruption, Approval, AI Workflow Controllers. |
| **PostgreSQL & EF Core (10 M)** | `Routes`, `Stops`, `Services`, `FareRules` tables & search indexes. | `Buses`, `SeatLayouts`, `Seats`, `Drivers` tables & constraints. | `SeatHolds`, `Bookings`, `Tickets`, `Payments` tables & DB locks. | `Disruptions`, `Approvals`, `AiWorkflows`, `AiToolCalls` tables. |
| **React Web App (10 M)** | `RouteManagerPage`, `ServiceSchedulerPage`, `TouristCorridorsPage`. | `FleetMatrixBuilderPage`, `SeatLayoutDesignerPage`, `DriverRosteringPage`. | `OperatorDashboardPage`, `BookingManifestMonitorPage`. | `DisruptionIntakePage`, `ManagerApprovalWorkbenchPage`, `AiObservabilityPage`. |
| **Flutter Mobile App (10 M)** | `JourneySearchScreen`, `JourneyComparisonScreen`. | `SeatPickerScreen`, `ReviewSubmissionScreen`. | `PaymentCheckoutScreen`, `TicketWalletScreen`, `BookingHistoryScreen`. | `DisruptionAlertScreen`, `ConductorScannerScreen`, `ConductorManifestScreen`. |
| **Agentic AI Contribution (12 M)** | Journey Analysis Agent (`SearchRoutes` tool). | Resource Feasibility Agent (`CheckReplacementResources` tool). | Booking & Policy Agent (`CalculateFareDifference` tool). | Validation & Safety Agent & Coordinator (`RequestManagerApproval` tool). |
| **API & Security (10 M)** | Route & Service DTO validation, search query authorization. | Driver rostering RBAC, 2D seat matrix serialization. | Concurrency locks (`IDbContextTransaction`), HMAC QR signing. | Manager approval state machine, role authorization, audit logs. |
| **Testing & CI Workflow (8 M)** | Route & search unit, integration, and widget tests. | Seat matrix & driver schedule unit, integration, and widget tests. | Concurrency hold & payment unit, integration, and widget tests. | E2E cross-platform test, AI golden tests, and CI/CD workflow. |
| **Documentation & ADRs** | Route Engine report; `ADR-001`, `ADR-002`, `ADR-005` input. | Fleet & Resource report; `ADR-001`, `ADR-002`, `ADR-004` input. | Booking & Ticketing report; `ADR-001`, `ADR-002`, `ADR-005` input. | Disruption & AI report; Lead `ADR-003`, `ADR-004`, `ADR-005`. |

---

## 6. Shared System Responsibilities (Cross-Cutting Infrastructure)

To prevent component isolation, the team shares joint responsibility for system-wide infrastructure:

1. **Shared Authentication & JWT Middleware**: All members implement JWT token validation and role-based policy enforcement (`[Authorize]`) across their respective controllers.
2. **Shared Database Context (`WayPointDbContext`)**: All members configure entity mappings, relationships, audit fields (`CreatedAt`/`UpdatedAt`), and EF Core migrations within the unified DbContext.
3. **Shared REST API & Swagger Infrastructure**: Joint responsibility for OpenAPI 3.0 annotations, DTO validations, standardized error handling middleware, and interactive testing via `/swagger`.
4. **Shared Agent Tool Runner Infrastructure**: Shared execution wrapper for validating tool DTO inputs, invoking backend services, and logging tool traces into `AiToolCall` database tables.
5. **Cross-Service End-to-End Workflow Test (`REQ-TEST-05`)**: Joint responsibility for creating and executing the mandatory E2E workflow test: Flutter search & booking → ASP.NET Core API → PostgreSQL → Agentic AI Triage → React Manager Approval → ASP.NET Core Transaction → Flutter In-App Notification.
6. **CI/CD & Cloud Deployment**: Joint responsibility for maintaining `.github/workflows/ci.yml`, Docker configurations, Render static hosting, and Railway live deployment verification.
