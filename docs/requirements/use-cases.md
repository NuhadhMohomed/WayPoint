# WayPoint Use Cases Specification

This document details the major use cases for the **WayPoint** AI-Powered Intercity Journey Planner & Bus Operations Platform operating under a **Headless API-First Architecture** ([ADR-006](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-006-headless-architecture.md)).

---

## Use Case Summary Table

| Use Case ID | Use Case Name | Primary Actor | Mapped Requirements |
| :--- | :--- | :--- | :--- |
| **`UC-01`** | Passenger Journey Search & Selection | API Client / Passenger | `FR-JOURNEY-003`, `FR-JOURNEY-004` |
| **`UC-02`** | Temporary Seat Hold & Transactional Booking | API Client / Passenger | `FR-FLEET-003`, `FR-BOOKING-001`, `FR-BOOKING-002`, `FR-BOOKING-003` |
| **`UC-03`** | Disruption Logging & AI Rebooking Proposal | Operator / Agentic AI | `FR-FLEET-004`, `FR-DISRUPTION-001`, `FR-DISRUPTION-002`, `FR-AI-001` |
| **`UC-04`** | Manager Disruption Review & Approval Execution | Transport Manager | `FR-DISRUPTION-003`, `FR-APPROVAL-001`, `FR-DISRUPTION-004`, `FR-AUDIT-001` |
| **`UC-05`** | Operator Fleet & Timetable Administration | Operator / Dispatcher | `FR-JOURNEY-001`, `FR-JOURNEY-002`, `FR-FLEET-001`, `FR-FLEET-002` |
| **`UC-06`** | QR E-Ticket Boarding Verification | Operator / Scanner | `FR-BOOKING-003`, `FR-OPERATOR-001` |
| **`UC-07`** | Post-Trip Review & Rating Submission | Passenger Client | `FR-REVIEW-001`, `FR-REVIEW-002`, `FR-REVIEW-003`, `FR-REVIEW-004` |

---

## 1. `UC-01`: Passenger Journey Search & Selection

- **Primary Actor**: Passenger Client (API Consumer)
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database
- **Preconditions**: Transit service timetables and routes are seeded in PostgreSQL.
- **Trigger**: Client submits a journey search request to `POST /api/v1/journeys/search`.

### Main Success Scenario
1. Client submits origin (e.g., Colombo), destination (e.g., Ella), travel date/time, passenger count, and preference filters (arrival deadline, direct service, AC amenity).
2. Backend queries PostgreSQL database for active routes, intermediate stops, timetables, and available seat inventory matching the corridor.
3. Backend evaluates route transfer feasibility (checking minimum 20-minute buffer for connecting journeys, `BR-TRANSFER-001`).
4. Backend ranks candidate journey options based on arrival time, total duration, total fare, directness, and amenity match score.
5. Backend returns structured JSON candidate journey list payload to client (`200 OK`).

### Extensions / Alternate Flows
- **2a. No direct services available**:
  - System generates connecting journey candidate combinations (Leg 1 + Leg 2) with valid transfer windows.
  - Returns connecting options flagged with transfer stop and wait duration.
- **3a. Transfer window insufficient (<20 minutes)**:
  - Backend excludes infeasible connecting service combination from candidate list.
- **5a. No matching services found**:
  - Backend returns empty list with `200 OK`.

- **Postconditions**: Candidate journey list returned to client; ready for seat selection.

---

## 2. `UC-02`: Temporary Seat Hold & Transactional Booking

- **Primary Actor**: Passenger Client
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database, Payment Sandbox Provider
- **Preconditions**: Candidate service selected; available seats queried via `GET /api/v1/services/{id}/seats`.
- **Trigger**: Client submits seat reservation hold request to `POST /api/v1/bookings/hold`.

### Main Success Scenario
1. Client sends seat selection request `POST /api/v1/bookings/hold` with `ServiceId` and `SeatNumbers`.
2. ASP.NET Core opens PostgreSQL database transaction (`IDbContextTransaction`).
3. Backend checks real-time seat availability (`FR-FLEET-003`).
4. Seats are in `Available` state; backend inserts `SeatHold` record with 10-minute expiration timestamp (`HeldUntil = UtcNow + 10m`) and updates seat status to `Held`.
5. Backend commits database transaction and returns `SeatHold` ID with countdown timer (`expiresInSeconds: 600`).
6. Client sends payment authorization request to `POST /api/v1/payments/confirm-sandbox-charge` with `HoldId` and payment token.
7. ASP.NET Core processes payment request through Payment Sandbox gateway proxy.
8. Payment Sandbox returns successful authorization response.
9. ASP.NET Core opens PostgreSQL transaction:
   - Transitions `SeatHold` record to `Booking` status (`Confirmed`).
   - Marks seat inventory as `Booked`.
   - Generates digital QR e-ticket record with HMAC-SHA256 signature.
10. Backend commits transaction and returns confirmed booking details + QR code payload.

### Extensions / Alternate Flows
- **3a. Seat already held or booked by another user (Race Condition)**:
  - Database transaction detects conflict (`409 Conflict`).
  - Backend rolls back transaction and returns error detail.
- **6a. Seat hold countdown expires (10 minutes elapsed without payment)**:
  - Backend detects `HeldUntil < DateTime.UtcNow`.
  - Seat returns to `Available` state; returns `400 Bad Request` ("Seat hold expired. Please re-select your seats.").
- **8a. Payment Sandbox declines transaction or times out**:
  - Payment fails; ASP.NET Core cancels seat hold.
  - Seat returns to `Available` state; returns `402 Payment Required`.

- **Postconditions**: Confirmed booking record stored in database; seat locked; digital QR e-ticket payload issued.

---

## 3. `UC-03`: Service Disruption Logging & Multi-Agent AI Rebooking Proposal

- **Primary Actor**: Transit Operator (API Consumer)
- **Secondary Actors**: Agentic AI Subsystem (Planner, Journey Analysis, Resource, Safety Agents), Transport Manager
- **Preconditions**: A ticketed bus service experiences a disruption (e.g., mechanical breakdown).
- **Trigger**: Operator posts disruption details to `POST /api/v1/disruptions`.

### Main Success Scenario
1. Operator submits disruption details (Service ID, Reason: "Engine Failure", Severity: "Major") to `POST /api/v1/disruptions`.
2. ASP.NET Core logs `DisruptionCase` record in PostgreSQL and queries affected ticketed bookings.
3. Backend initiates Level 4 Agentic AI workflow (`POST /api/v1/rebooking/generate-proposal`).
4. **Planner Agent** analyzes disruption objective and generates a structured multi-step plan:
   - Step 1: Analyze candidate route alternatives (delegated to Journey Analysis Agent).
   - Step 2: Check replacement bus & driver feasibility (delegated to Resource Agent).
   - Step 3: Evaluate rebooking costs & fare rules (delegated to Resource Agent).
   - Step 4: Validate safety rules, classify impact, & check approval boundary (delegated to Validation & Safety Agent).
5. **Journey Analysis Agent** executes allow-listed tools `SearchRoutes` & `GetBoardingPoints` to find alternative departure candidates.
6. **Resource Agent** executes `CheckSeatAvailability` to check unassigned buses with equal/greater seat capacity and available drivers.
7. **Validation & Safety Agent** executes `CalculatePassengerImpact` and `CalculateFareDifference`.
8. Validation Agent determines the action involves cancelling a ticketed service, classifying impact as **High-Impact** (`BR-APPROVAL-001`).
9. Agent executes allow-listed tool `RequestManagerApproval`.
10. Backend sets workflow state to `PendingManagerApproval` and persists all agent steps, tool calls, and proposed rebooking plans in PostgreSQL tables (`AiWorkflow`, `AiWorkflowStep`, `AiToolCall`).
11. Proposal is exposed on `GET /api/v1/approvals/pending` awaiting Transport Manager review.

### Extensions / Alternate Flows
- **6a. No replacement bus/driver available**:
  - Resource Agent reports resource infeasibility (`Feasible: false`).
  - Validation Agent constructs full passenger refund proposal + cancellation notification proposal.
- **7a. Agent tool call returns error or times out**:
  - System applies retry logic (max 3 retries).
  - If retries fail, workflow transitions gracefully to **Safe Failure** state (`AiWorkflowStatus: SafeFailure`).
  - Live operational data remains uncorrupted.
- **8a. Proposed change is low-impact (e.g., minor 5-minute schedule adjustment)**:
  - Safety Agent determines action does not require human approval.
  - Workflow automatically applies adjustment and dispatches passenger notification records.

- **Postconditions**: Proposed rebooking remedy persisted in PostgreSQL; workflow paused in `PendingManagerApproval` state.

---

## 4. `UC-04`: Transport Manager Disruption Review & Approval Execution

- **Primary Actor**: Transport Manager (API Consumer)
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database, Notification Service
- **Preconditions**: Workflow is in `PendingManagerApproval` state following `UC-03`.
- **Trigger**: Transport Manager queries pending approval queue via `GET /api/v1/approvals/pending`.

### Main Success Scenario
1. Transport Manager queries pending disruption case from `GET /api/v1/approvals/pending`.
2. Backend returns comprehensive decision evidence payload:
   - Original disrupted service & affected passenger count.
   - AI-recommended replacement service & bus resource details.
   - Before/after timetable and fare impact metrics.
   - AI agent tool execution trace and deterministic validation summary.
3. Transport Manager submits formal decision to `POST /api/v1/approvals/{id}/decision` with `decision: "Approve"`, manager signature, and rationale.
4. ASP.NET Core Web API executes `ApplyApprovedOperationalChange` tool logic inside a PostgreSQL database transaction:
   - Cancels original service status.
   - Transfers affected passenger bookings to replacement service seats transactionally.
   - Updates passenger ticket records with new departure time/bus details.
   - Inserts immutable `ApprovalDecision` record with manager timestamp and signature.
5. Backend commits transaction.
6. Backend triggers `SendPassengerNotification` tool logic to insert notification records for affected passengers.
7. Backend returns confirmation payload with status `Approved & Executed`.

### Extensions / Alternate Flows
- **3a. Transport Manager selects "Reject"**:
  - Manager submits rejection decision with rationale to `POST /api/v1/approvals/{id}/decision`.
  - Backend updates proposal state to `Rejected`.
  - Backend cancels rebooking proposal and initiates automatic full passenger refund workflow.
- **3b. Transport Manager selects "Request Revision"**:
  - Manager submits revision comments.
  - Backend updates workflow state to `RevisionRequested`.

- **Postconditions**: Operational changes transactionally applied to database; immutable audit log recorded; passengers notified.

---

## 5. `UC-05`: Operator Fleet & Timetable Administration

- **Primary Actor**: Operator / Dispatcher
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database
- **Preconditions**: User authenticated with Operator or Administrator role.
- **Trigger**: Operator submits fleet or timetable modifications.

### Main Success Scenario
1. Operator submits new bus registration to `POST /api/v1/buses` with registration number, total capacity, bus class, and seat layout template ID.
2. ASP.NET Core validates DTO inputs and inserts bus and seat records into PostgreSQL.
3. Operator schedules a new departure via `POST /api/v1/services` specifying route, bus, driver, departure time, and base fare.
4. Backend validates schedule non-overlap constraints and driver rest hours (`BR-RESOURCE-002`).
5. Backend saves service departure record in PostgreSQL.
6. New service becomes immediately searchable by clients via `POST /api/v1/journeys/search`.

### Extensions / Alternate Flows
- **4a. Driver or bus schedule conflict detected**:
  - Backend detects assigned driver is already scheduled on another route during overlapping hours.
  - Backend rejects request with `400 Bad Request` ("Driver D-102 is already assigned to Service S-405 during requested timeframe").

- **Postconditions**: Fleet resources and scheduled services successfully saved in PostgreSQL database.

---

## 6. `UC-06`: QR E-Ticket Boarding Verification

- **Primary Actor**: Conductor / Station Scanner (API Consumer)
- **Secondary Actor**: Passenger
- **Preconditions**: Passenger has confirmed booking and valid HMAC-signed QR ticket payload.
- **Trigger**: Station scanner submits QR verification request to `POST /api/v1/tickets/verify-qr`.

### Main Success Scenario
1. Conductor scans passenger's cryptographic QR payload string.
2. Scanner client sends verification request `POST /api/v1/tickets/verify-qr` with QR token payload to ASP.NET Core API.
3. Backend validates HMAC-SHA256 signature against server secret, verifies booking status (`Confirmed`), and verifies matching departure service and boarding stop.
4. Backend updates passenger boarding status to `Boarded` with timestamp.
5. Backend returns `200 OK` with payload: `Valid Ticket - Seat 14B - Boarded`.

### Extensions / Alternate Flows
- **3a. Tampered or invalid signature**:
  - Signature verification fails.
  - Backend returns `400 Bad Request` ("Invalid or tampered ticket token").
- **3b. Ticket already boarded**:
  - Backend detects `IsBoarded == true`.
  - Backend returns `409 Conflict` ("Ticket already boarded at 06:12 AM").

- **Postconditions**: Ticket marked `Boarded` in database; manifest updated in real time.

---

## 7. `UC-07`: Post-Trip Review & Rating Submission

- **Primary Actor**: Passenger Client
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database
- **Preconditions**: Passenger has a completed booking (`Boarded` status or service completed).
- **Trigger**: Passenger submits rating and optional comment via `POST /api/v1/reviews`.

### Main Success Scenario
1. Passenger client sends review submission to `POST /api/v1/reviews` with `BookingId`, `BusRating` (1-5), `DriverRating` (1-5), optional comments, and `IsAnonymous` boolean.
2. Backend verifies trip arrival occurred within the last 7 days (`FR-REVIEW-004`).
3. Backend runs profanity filter sanitization on comment text (`FR-REVIEW-003`).
4. If `IsAnonymous` is true, passenger identity is masked (`FR-REVIEW-002`).
5. Review record is saved to PostgreSQL database.
6. Backend returns `201 Created` with created review summary.
