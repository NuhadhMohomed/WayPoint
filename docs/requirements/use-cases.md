# WayPoint Use Cases Specification

This document details the major use cases for the **WayPoint** AI-Powered Intercity Journey Planner & Bus Operations Platform.

---

## Use Case Summary Table

| Use Case ID | Use Case Name | Primary Actor | Mapped Requirements |
| :--- | :--- | :--- | :--- |
| **`UC-01`** | Passenger Journey Search & Selection | Passenger | `FR-JOURNEY-003`, `FR-JOURNEY-004`, `FR-FE-08`, `FR-FE-09` |
| **`UC-02`** | Temporary Seat Hold & Transactional Booking | Passenger | `FR-FLEET-003`, `FR-BOOKING-001`, `FR-BOOKING-002`, `FR-BOOKING-003` |
| **`UC-03`** | Disruption Logging & AI Rebooking Proposal | Operator / Agentic AI | `FR-FLEET-004`, `FR-DISRUPTION-001`, `FR-DISRUPTION-002`, `FR-AI-001` |
| **`UC-04`** | Manager Disruption Review & Approval Execution | Transport Manager | `FR-DISRUPTION-003`, `FR-APPROVAL-001`, `FR-DISRUPTION-004`, `FR-AUDIT-001` |
| **`UC-05`** | Operator Fleet & Timetable Administration | Operator / Dispatcher | `FR-JOURNEY-001`, `FR-JOURNEY-002`, `FR-FLEET-001`, `FR-FLEET-002` |
| **`UC-06`** | QR E-Ticket Boarding Verification | Passenger / Operator | `FR-BOOKING-003`, `FR-OPERATOR-001`, `FR-FE-11`, `FR-FE-13` |

---

## 1. `UC-01`: Passenger Journey Search & Selection

- **Primary Actor**: Passenger
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database
- **Preconditions**: Passenger has opened the Flutter mobile application.
- **Trigger**: Passenger enters origin, destination, travel date/time, passenger count, and preferences, then taps "Search Journeys".

### Main Success Scenario
1. Passenger selects origin (e.g., Colombo), destination (e.g., Ella), travel date/time, and passenger count.
2. Passenger specifies optional preference filters (e.g., arrival before 6:00 PM, direct service preferred, AC amenity required).
3. Flutter app sends `POST /api/v1/journeys/search` request to ASP.NET Core Web API.
4. ASP.NET Core queries PostgreSQL database for active routes, intermediate stops, timetables, and available seat inventory matching the corridor.
5. Backend evaluates route transfer feasibility (checking minimum 20-minute buffer for connecting journeys).
6. Backend ranks candidate journey options based on arrival time, total duration, total fare, directness, and amenity match score.
7. Backend returns structured candidate journey list payload to Flutter app.
8. Flutter app renders journey comparison cards comparing departure, arrival, duration, total fare, directness, and seat availability.

### Extensions / Alternate Flows
- **3a. No direct services available**:
  - System generates connecting journey candidate combinations (Leg 1 + Leg 2) with valid transfer windows.
  - Returns connecting options flagged with transfer stop and wait duration.
- **5a. Transfer window insufficient (<20 minutes)**:
  - Backend excludes infeasible connecting service combination from candidate list.
- **7a. No matching services found**:
  - Backend returns empty list with `200 OK`.
  - Flutter app displays "No direct or connecting journeys found for selected date/corridor" with suggestion to adjust travel date or filters.

- **Postconditions**: Candidate journey list displayed to passenger; system ready for seat selection.

---

## 2. `UC-02`: Temporary Seat Hold & Transactional Booking

- **Primary Actor**: Passenger
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database, Payment Sandbox Provider
- **Preconditions**: Passenger selected a candidate journey option and navigated to seat layout view.
- **Trigger**: Passenger selects available seat(s) and taps "Hold & Proceed to Payment".

### Main Success Scenario
1. Flutter app sends seat selection request `POST /api/v1/bookings/hold` with Service ID and Seat Numbers to ASP.NET Core API.
2. ASP.NET Core opens PostgreSQL database transaction (`IDbContextTransaction`).
3. Backend checks real-time seat availability (`FR-FLEET-003`).
4. Seats are in `Available` state; backend inserts `SeatHold` record with 10-minute expiration timestamp and updates seat status to `Held`.
5. Backend commits database transaction and returns `SeatHold` ID with countdown timer.
6. Flutter app displays interactive 10-minute payment checkout timer and payment sandbox interface.
7. Passenger enters sandbox payment credentials and taps "Pay Now".
8. ASP.NET Core processes payment request through Payment Sandbox gateway proxy (`FR-BE-06`).
9. Payment Sandbox returns successful authorization response.
10. ASP.NET Core opens PostgreSQL transaction:
    - Transitions `SeatHold` record to `Booking` status (`Confirmed`).
    - Marks seat inventory as `Booked`.
    - Generates digital QR e-ticket record.
11. Backend commits transaction and returns confirmed booking details + QR code payload.
12. Flutter app displays booking confirmation screen and saves QR e-ticket into offline mobile wallet.

### Extensions / Alternate Flows
- **4a. Seat already held or booked by another user (Race Condition)**:
  - Database transaction detects conflict (`409 Conflict`).
  - Backend rolls back transaction.
  - Flutter app alerts passenger: "Selected seat was just reserved by another user. Please choose an alternative seat."
- **6a. Seat hold countdown expires (10 minutes elapsed without payment)**:
  - Server background job or lazy check marks hold expired.
  - Seat returns to `Available` state.
  - Attempting to pay after expiry returns `400 Bad Request` ("Seat hold expired. Please re-select your seats.").
- **9a. Payment Sandbox declines transaction or times out**:
  - Payment fails; ASP.NET Core cancels seat hold.
  - Seat returns to `Available` state.
  - Flutter app displays payment error with option to retry payment or change payment method.

- **Postconditions**: Confirmed booking record stored in database; seat locked; digital QR e-ticket issued to passenger wallet.

---

## 3. `UC-03`: Service Disruption Logging & Multi-Agent AI Rebooking Proposal

- **Primary Actor**: Operator / Dispatcher
- **Secondary Actors**: Agentic AI Subsystem (Planner Agent, Journey Analysis Agent, Resource Agent, Safety Agent), Transport Manager
- **Preconditions**: A ticketed bus service experiences a disruption (e.g., mechanical failure on Colombo–Ella route).
- **Trigger**: Operator logs a service disruption event in React web workspace.

### Main Success Scenario
1. Operator submits disruption details (Service ID, Disruption Reason: "Engine Failure", Severity: "Major") via React UI to `POST /api/v1/disruptions`.
2. ASP.NET Core logs `DisruptionCase` record in PostgreSQL and queries affected ticketed bookings.
3. Backend initiates Level 4 Agentic AI workflow (`POST /api/v1/rebooking/generate-proposal`).
4. **Planner Agent** analyzes disruption objective and generates a structured multi-step plan:
   - Step 1: Analyze candidate route alternatives (delegated to Journey Analysis Agent).
   - Step 2: Check replacement bus & driver feasibility (delegated to Resource Agent).
   - Step 3: Evaluate rebooking costs & fare rules (delegated to Resource Agent).
   - Step 4: Validate safety rules, classify impact, & check approval boundary (delegated to Validation & Safety Agent).
5. **Journey Analysis Agent** executes allow-listed tool `SearchRoutes` & `GetBoardingPoints` to find alternative departure candidates.
6. **Resource Agent** executes `CheckSeatAvailability` to check unassigned buses with equal/greater seat capacity and available drivers.
7. **Validation & Safety Agent** executes `CalculatePassengerImpact` and `CalculateFareDifference`.
8. Validation Agent determines the action involves cancelling a ticketed service and reassigning passengers, classifying impact as **High-Impact**.
9. Agent executes allow-listed tool `RequestManagerApproval`.
10. Backend sets workflow state to `PendingManagerApproval` and persists all agent steps, tool calls, and proposed rebooking plans in PostgreSQL tables (`AiWorkflow`, `AiWorkflowStep`, `AiToolCall`).
11. React web workspace updates Manager Approval Workbench with notification for Transport Manager.

### Extensions / Alternate Flows
- **6a. No replacement bus/driver available**:
  - Resource Agent reports resource infeasibility (`Feasible: false`).
  - Validation Agent constructs full passenger refund proposal + cancellation notification proposal.
- **7a. Agent tool call returns error or times out**:
  - System applies retry logic (max 3 retries).
  - If retries fail, workflow transitions gracefully to **Safe Failure** state (`AiWorkflowStatus: SafeFailure`).
  - System alerts operator with manual fallback controls; live operational data remains uncorrupted.
- **8a. Proposed change is low-impact (e.g., minor 5-minute schedule adjustment)**:
  - Safety Agent determines action does not require human approval.
  - Workflow automatically applies adjustment and dispatches passenger notifications.

- **Postconditions**: Proposed rebooking remedy persisted in PostgreSQL; workflow paused in `PendingManagerApproval` state awaiting manager action.

---

## 4. `UC-04`: Transport Manager Disruption Review & Approval Execution

- **Primary Actor**: Transport Manager
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database, Flutter Mobile App (Passengers)
- **Preconditions**: Workflow is in `PendingManagerApproval` state following `UC-03`.
- **Trigger**: Transport Manager opens Manager Approval Workbench in React web application.

### Main Success Scenario
1. Transport Manager selects pending disruption case from approval queue.
2. React workspace displays comprehensive decision evidence:
   - Original disrupted service & affected passenger count.
   - AI-recommended replacement service & bus resource details.
   - Before/after timetable and fare impact metrics.
   - AI agent tool execution trace and deterministic validation summary.
3. Transport Manager reviews evidence and clicks **"Approve Rebooking"**.
4. React app sends `POST /api/v1/approvals/{id}/decision` with decision: `Approve` and comments.
5. ASP.NET Core Web API executes `ApplyApprovedOperationalChange` tool logic inside a PostgreSQL database transaction:
   - Cancels original service status.
   - Transfers affected passenger bookings to replacement service seats transactionally.
   - Updates passenger ticket records with new departure time/bus details.
   - Inserts immutable `ApprovalDecision` record with manager timestamp and signature.
6. Backend commits transaction.
7. Backend triggers `SendPassengerNotification` tool logic to dispatch push/in-app notifications to affected passengers.
8. React workspace updates status to `Approved & Executed`.
9. Affected passengers receive push alert on Flutter app displaying updated journey itinerary and refreshed QR e-ticket.

### Extensions / Alternate Flows
- **3a. Transport Manager selects "Reject"**:
  - Manager enters rejection reason (e.g., "Cost too high; issue full refund instead").
  - Backend updates proposal state to `Rejected`.
  - Backend cancels rebooking proposal and initiates automatic full passenger refund workflow.
- **3b. Transport Manager selects "Request Revision"**:
  - Manager enters revision comments (e.g., "Check alternative departure at 2:00 PM instead").
  - Backend updates workflow state to `RevisionRequested` and notifies dispatcher/AI coordinator.

- **Postconditions**: Operational changes transactionally applied to database; immutable audit log recorded; passengers notified of updated tickets.

---

## 5. `UC-05`: Operator Fleet & Timetable Administration

- **Primary Actor**: Operator / Dispatcher
- **Secondary Actors**: ASP.NET Core Backend, PostgreSQL Database
- **Preconditions**: Operator logged into React web application.
- **Trigger**: Operator navigates to Fleet or Route Management section.

### Main Success Scenario
1. Operator opens "Bus Fleet Management" and clicks "Add New Bus".
2. Operator inputs registration number, total capacity (e.g., 40 seats), bus class (e.g., Super Luxury AC), and assigns a visual 2x2 seat matrix template.
3. React app sends `POST /api/v1/buses` request to backend.
4. ASP.NET Core validates DTO inputs and inserts bus and seat records into PostgreSQL.
5. Operator opens "Timetable Administration" and creates a new scheduled departure for Colombo–Ella corridor.
6. Operator assigns bus, driver, departure time, and fare rules.
7. Backend validates schedule non-overlap constraints and saves service listing.
8. New service becomes immediately searchable by passengers on Flutter app.

### Extensions / Alternate Flows
- **7a. Driver or bus schedule conflict detected**:
  - Backend detects assigned driver is already scheduled on another route during overlapping hours.
  - Backend rejects request with `400 Bad Request` ("Driver D-102 is already assigned to Service S-405 during requested timeframe").
  - React app highlights conflicting field.

- **Postconditions**: Fleet resources and scheduled services successfully saved in PostgreSQL database.

---

## 6. `UC-06`: QR E-Ticket Boarding Verification

- **Primary Actor**: Operator / Driver
- **Secondary Actor**: Passenger
- **Preconditions**: Passenger has confirmed booking and active QR e-ticket on Flutter app; bus is preparing for departure at boarding terminal.
- **Trigger**: Driver/Operator opens QR Scanner screen on Flutter/React app.

### Main Success Scenario
1. Passenger presents digital QR e-ticket on Flutter mobile screen.
2. Driver uses device camera feature to scan passenger's QR code.
3. Mobile app sends verification request `POST /api/v1/tickets/verify` with QR payload token to ASP.NET Core API.
4. Backend decrypts payload, verifies token signature, checks booking status in PostgreSQL (`Confirmed`), and verifies matching departure service and boarding stop.
5. Backend updates passenger boarding status to `Boarded` with timestamp.
6. Mobile scanner screen displays green confirmation alert: "Valid Ticket - Seat 14B - Boarded".
7. Operator manifest updates passenger status in real time.

### Extensions / Alternate Flows
- **4a. QR code invalid or tampered**:
  - Verification fails; scanner screen displays red alert: "Invalid Ticket - Signature Mismatch".
- **4b. QR code scanned for wrong service or date**:
  - Scanner screen displays warning: "Invalid Service - Ticket is for Departure at 2:00 PM tomorrow".
- **4c. Ticket already scanned (Duplicate Boarding Attempt)**:
  - Scanner screen displays warning: "Already Boarded at 10:15 AM".

- **Postconditions**: Passenger marked as `Boarded` in database; manifest updated.
