# WayPoint Software Requirements Specification (SRS) - Requirements

This document defines the functional and non-functional requirements for the **WayPoint** AI-Powered Intercity Journey Planner & Bus Operations Platform, developed for **SE3090 Assignment 1**.

---

## 1. Functional Requirements

### 1.1 Authentication & Authorization (`FR-AUTH`)

#### `FR-AUTH-001`: User Registration
- **Description**: Allow new passengers, operators, transport managers, and administrators to register accounts with validated credentials.
- **Actor**: Anonymous User / Administrator
- **Preconditions**: User is not logged into the system.
- **Expected Behavior**: The system validates email format, password complexity, and role parameters, hashes the password using BCrypt/Argon2, creates the user record in PostgreSQL, and returns a success response.
- **Acceptance Criteria**:
  1. System rejects registration attempts with duplicate email addresses with `409 Conflict`.
  2. System rejects weak passwords (<8 chars, no special char/number) with `400 Bad Request`.
  3. User record is saved with `CreatedAt` audit timestamp and hashed password.
- **Priority**: Must Have

#### `FR-AUTH-002`: User Authentication & Token Issuance
- **Description**: Authenticate users via email and password, issuing a signed JSON Web Token (JWT).
- **Actor**: Passenger / Operator / Transport Manager / Administrator
- **Preconditions**: User account exists in PostgreSQL.
- **Expected Behavior**: System validates credentials, generates a JWT containing User ID, Email, and Role claims with an expiration time, and returns the token.
- **Acceptance Criteria**:
  1. Invalid credentials return `401 Unauthorized` without revealing whether email or password was wrong.
  2. Valid authentication returns a JWT token signed with server secret.
  3. Tokens expire after the configured duration (e.g., 24 hours).
- **Priority**: Must Have

#### `FR-AUTH-003`: Role-Based Access Control (RBAC)
- **Description**: Restrict access to API endpoints and UI routes based on user role claims.
- **Actor**: All Authenticated Users
- **Preconditions**: User submits request with Bearer JWT header.
- **Expected Behavior**: Backend ASP.NET Core middleware inspects role claim; grants access if authorized, or returns `403 Forbidden`.
- **Acceptance Criteria**:
  1. Passengers cannot access administrative or operational endpoints (`/api/v1/admin/*`, `/api/v1/operator/*`).
  2. Dispatchers/Operators cannot execute Transport Manager approval actions (`/api/v1/approvals/*`).
  3. Unauthenticated requests to protected endpoints return `401 Unauthorized`.
- **Priority**: Must Have

---

### 1.2 Journey Planning & Route Catalogue (`FR-JOURNEY`)

#### `FR-JOURNEY-001`: Route & Stop Management
- **Description**: Provide CRUD functionality for intercity bus routes, intermediate route stops, boarding/drop points, and tourist corridor destinations.
- **Actor**: Operator / Dispatcher
- **Preconditions**: User authenticated with Operator or Administrator role.
- **Expected Behavior**: System allows adding, modifying, listing, and soft-deleting routes, stops, and boarding points in PostgreSQL.
- **Acceptance Criteria**:
  1. Route creation validates origin, destination, distance, estimated duration, and ordered stop sequences.
  2. Intermediate stops must specify arrival/departure offset minutes from origin departure.
  3. Boarding points include name, landmark, and GPS coordinates where supported.
- **Priority**: Must Have

#### `FR-JOURNEY-002`: Timetable & Service Listing Management
- **Description**: Manage scheduled bus services, departure times, frequency, bus class, amenities, and fare structures.
- **Actor**: Operator / Dispatcher
- **Preconditions**: Route and bus resources exist.
- **Expected Behavior**: System lists and manages scheduled service departures linked to specific routes, buses, and pricing rules.
- **Acceptance Criteria**:
  1. Services validate that assigned departure time and bus assignment do not conflict with existing schedules.
  2. Fare rules calculate pricing based on distance, seat class, and stop combinations.
- **Priority**: Must Have

#### `FR-JOURNEY-003`: Deterministic Journey Search & Candidate Generation
- **Description**: Generate feasible direct and connecting journey options based on passenger origin, destination, travel date/time, and count.
- **Actor**: Passenger
- **Preconditions**: Active services exist for the requested travel date.
- **Expected Behavior**: ASP.NET Core queries PostgreSQL, evaluates route networks and transfer feasibility windows, and returns direct and connecting journey options.
- **Acceptance Criteria**:
  1. Direct services matching origin and destination are generated first.
  2. Connecting services validate transfer windows (e.g., minimum 20 minutes buffer between arrival of leg 1 and departure of leg 2).
  3. Infeasible connections (insufficient transfer time or cancelled services) are excluded.
- **Priority**: Must Have

#### `FR-JOURNEY-004`: Preference-Aware Journey Ranking
- **Description**: Rank generated candidate journeys using passenger preferences (arrival deadline, lowest fare, shortest duration, direct service preference, bus amenities).
- **Actor**: Passenger
- **Preconditions**: Candidate journeys generated by `FR-JOURNEY-003`.
- **Expected Behavior**: System scores and orders candidate journeys according to passenger weighting criteria.
- **Acceptance Criteria**:
  1. Returned results include calculated total fare, total travel duration, transfer count, and match score.
  2. Filtering by amenities (e.g., AC, Wi-Fi, Reclining Seats) excludes non-compliant services.
- **Priority**: Should Have

---

### 1.3 Fleet, Seat & Resource Feasibility (`FR-FLEET`)

#### `FR-FLEET-001`: Bus & Seat Template Management
- **Description**: Manage bus fleet inventory, maintenance status, registration details, and visual seat layout templates.
- **Actor**: Operator / Dispatcher
- **Preconditions**: Operator authenticated.
- **Expected Behavior**: System records bus specifications, total seat counts, maintenance logs, and seat matrix templates (e.g., 2x2, 2x1 luxury layout).
- **Acceptance Criteria**:
  1. Seat layout templates define individual seat numbers, row/column positions, class (Luxury/Standard), and accessibility flags.
  2. Buses flagged under maintenance cannot be assigned to active timetable services.
- **Priority**: Must Have

#### `FR-FLEET-002`: Driver Assignment & Conflict Prevention
- **Description**: Assign licensed drivers to scheduled services and enforce schedule conflict validation.
- **Actor**: Operator / Dispatcher
- **Preconditions**: Driver and service exist.
- **Expected Behavior**: System validates driver availability and rest hours before confirming schedule assignment.
- **Acceptance Criteria**:
  1. System rejects assigning a driver to overlapping service timeframes with `400 Bad Request`.
  2. Driver status changes reflect active, off-duty, or leave status.
- **Priority**: Must Have

#### `FR-FLEET-003`: Real-Time Seat Availability Calculation
- **Description**: Compute real-time seat availability for any service by cross-referencing seat layouts against active server-side holds and confirmed bookings.
- **Actor**: Passenger / Operator / AI Agent
- **Preconditions**: Service ID requested.
- **Expected Behavior**: Backend checks PostgreSQL database state and returns exact seat map array with status (`Available`, `Held`, `Booked`, `Blocked`).
- **Acceptance Criteria**:
  1. Clients NEVER compute availability locally; backend is the authoritative source.
  2. Held seats within active expiration window are marked `Held`.
  3. Confirmed ticketed seats are marked `Booked`.
- **Priority**: Must Have

#### `FR-FLEET-004`: Replacement Resource Feasibility Evaluation
- **Description**: Evaluate whether alternative buses, drivers, or seat capacity can support a disrupted or cancelled service.
- **Actor**: Operator / AI Resource Agent
- **Preconditions**: Disruption event logged or simulated.
- **Expected Behavior**: System checks unassigned fleet inventory, driver rest hours, and minimum seat capacity matching affected passenger count.
- **Acceptance Criteria**:
  1. Replacement bus must have equal or greater seat capacity than booked passengers.
  2. Driver assignment must satisfy rest hour policies.
  3. Operation outputs structured feasibility report (`Feasible: true/false`, resource IDs, conflict details).
- **Priority**: Must Have

---

### 1.4 Booking, Ticketing & Passenger Options (`FR-BOOKING`)

#### `FR-BOOKING-001`: Temporary Seat Hold Reservation
- **Description**: Place a temporary, time-bound lock on chosen bus seats during passenger checkout.
- **Actor**: Passenger
- **Preconditions**: Selected seats are in `Available` state.
- **Expected Behavior**: ASP.NET Core executes a database transaction to lock seat records and insert a `SeatHold` record with expiration timestamp (e.g., 10 minutes).
- **Acceptance Criteria**:
  1. Concurrent hold requests for the same seat result in one success and one failure (`409 Conflict`).
  2. Seat holds automatically expire and revert to `Available` after timeout if unpaid.
  3. Returning seat status includes countdown timer payload.
- **Priority**: Must Have

#### `FR-BOOKING-002`: Transactional Payment & Booking Confirmation
- **Description**: Confirm seat reservation and issue booking record upon successful payment sandbox transaction.
- **Actor**: Passenger / Payment Gateway Callback
- **Preconditions**: Active valid `SeatHold` exists.
- **Expected Behavior**: Backend validates payment sandbox authorization, transitions `SeatHold` to `Booking` in a single database transaction, generates ticket IDs, and clears hold lock.
- **Acceptance Criteria**:
  1. Booking confirmation MUST occur inside `IDbContextTransaction`.
  2. Failed payment releases seat hold back to `Available`.
  3. Successful payment updates booking status to `Confirmed` and logs payment reference ID.
- **Priority**: Must Have

#### `FR-BOOKING-003`: Digital QR E-Ticket Generation
- **Description**: Generate verifiable digital e-tickets containing booking details and secure QR payload.
- **Actor**: Passenger / Operator Verification
- **Preconditions**: Booking status is `Confirmed`.
- **Expected Behavior**: Backend generates QR code payload containing encrypted/signed booking reference, route, departure time, and seat numbers.
- **Acceptance Criteria**:
  1. Passenger can view e-ticket in Flutter mobile app offline/online wallet.
  2. Operator can scan QR code via mobile camera/scanner device feature to verify passenger boarding.
- **Priority**: Must Have

#### `FR-BOOKING-004`: Passenger Cancellation & Refund Eligibility
- **Description**: Process passenger-initiated booking cancellations according to fare policy rules.
- **Actor**: Passenger
- **Preconditions**: Booking is `Confirmed` and service departure is in the future.
- **Expected Behavior**: Backend checks departure time offset, calculates eligible refund percentage (e.g., >24h = 90% refund, <24h = 50%), cancels seat booking, and records refund attempt.
- **Acceptance Criteria**:
  1. Seats associated with cancelled bookings immediately return to `Available` state.
  2. Non-refundable policy windows return detailed policy explanation to passenger.
- **Priority**: Must Have

---

### 1.5 Disruption, Rebooking & Approval (`FR-DISRUPTION`)

#### `FR-DISRUPTION-001`: Disruption Event Logging & Impact Assessment
- **Description**: Record service disruptions (bus breakdown, driver absence, road closure, weather cancellation) and calculate affected passenger metrics.
- **Actor**: Operator / Dispatcher / AI Agent
- **Preconditions**: Service exists.
- **Expected Behavior**: Operator or system logs `DisruptionCase`, links affected service ID, and calculates count of ticketed passengers affected.
- **Acceptance Criteria**:
  1. System generates an auditable disruption record with severity level (`Minor`, `Major`, `Critical`).
  2. Passenger impact query returns list of affected booking IDs and contact payloads.
- **Priority**: Must Have

#### `FR-DISRUPTION-002`: AI Rebooking Proposal Generation
- **Description**: Trigger multi-agent AI workflow to analyze alternative services and construct rebooking proposals for affected passengers.
- **Actor**: Disruption Agent / Transport Manager
- **Preconditions**: Active `DisruptionCase` logged.
- **Expected Behavior**: Agentic AI workflow queries available route capacity, evaluates alternative departures, constructs rebooking plans, and checks fare/seat compatibility.
- **Acceptance Criteria**:
  1. Rebooking proposal lists alternative service options, transfer adjustments, and fare difference coverage.
  2. Proposal is stored in `RebookingProposal` database table without modifying passenger bookings directly.
- **Priority**: Must Have

#### `FR-DISRUPTION-003`: Manager Approval Boundary Enforcement
- **Description**: Pause high-impact operational changes in `PendingManagerApproval` state until authorized by a Transport Manager.
- **Actor**: Validation & Safety Agent / Transport Manager
- **Preconditions**: Proposed operational change classified as high-impact (ticketed service cancellation, timetable shift >15 mins, bus/driver reassignment).
- **Expected Behavior**: System sets workflow state to `PendingManagerApproval`, notifies Transport Manager in React workspace, and blocks automated execution.
- **Acceptance Criteria**:
  1. Low-impact actions (routine recommendations, search ranking) execute automatically without manager approval.
  2. High-impact actions CANNOT be applied without an explicit signed `ApprovalDecision` record from a Transport Manager.
- **Priority**: Must Have

#### `FR-DISRUPTION-004`: Transactional Rebooking Application & Notification
- **Description**: Execute manager-approved rebooking proposal transactionally and notify affected passengers.
- **Actor**: Transport Manager / Backend System
- **Preconditions**: Manager executes `Approve` action in React Manager Workbench.
- **Expected Behavior**: Backend applies rebooking changes to database in a single transaction, updates passenger ticket records, releases old seats, reserves new seats, and dispatches notifications.
- **Acceptance Criteria**:
  1. Rebooking execution is transactional (all affected passengers rebooked or total rollback).
  2. Passengers receive push/in-app alert on Flutter app detailing updated journey.
- **Priority**: Must Have

---

### 1.6 Agentic AI Workflows (`FR-AI`)

#### `FR-AI-001`: Multi-Agent Workflow Orchestration
- **Description**: Execute a domain-relevant Level 4 multi-agent workflow receiving a travel or disruption objective.
- **Actor**: Passenger / Operator / System Workflow Trigger
- **Preconditions**: Objective payload submitted via ASP.NET Core API.
- **Expected Behavior**: Backend initiates workflow coordinator, delegates tasks across 4 specialized agents, coordinates allow-listed tool calls, and returns structured plan results.
- **Acceptance Criteria**:
  1. Workflow implements 4 distinct specialized agents: Journey Analysis Agent, Resource Feasibility Agent, Booking & Policy Agent, and Validation & Safety Agent (with overall multi-agent workflow graph coordination designated as TBD / shared team implementation).
  2. Workflow state is persisted durably in PostgreSQL at every step.
- **Priority**: Must Have

#### `FR-AI-002`: Allow-Listed Tool Execution
- **Description**: Restrict AI agents to executing strictly defined allow-listed system tools with validated inputs/outputs.
- **Actor**: Agentic AI Subsystem
- **Preconditions**: Agent requests tool call.
- **Expected Behavior**: Backend intercepts tool request, validates input DTO, executes business logic, formats structured JSON response, and returns output to agent.
- **Acceptance Criteria**:
  1. System supports 10 mandatory tools: `SearchRoutes`, `GetBoardingPoints`, `CheckTransferFeasibility`, `CheckSeatAvailability`, `CalculateFareDifference`, `CreateRebookingProposal`, `CalculatePassengerImpact`, `RequestManagerApproval`, `ApplyApprovedOperationalChange`, `SendPassengerNotification`.
  2. Agents CANNOT execute direct SQL, shell commands, or arbitrary HTTP requests.
- **Priority**: Must Have

#### `FR-AI-003`: Deterministic Server-Side Rule Validation
- **Description**: Validate all AI outputs and recommendations against server-side business rules before accepting results.
- **Actor**: Validation & Safety Agent / Backend API
- **Preconditions**: AI produces candidate journey or rebooking proposal.
- **Expected Behavior**: Backend checks schema validity, route connection times, seat counts, and fare rules. Rejects invalid proposals.
- **Acceptance Criteria**:
  1. Hallucinated routes or non-existent bus IDs are rejected with validation error.
  2. Invalid outputs trigger automated retry or safe-failure fallback.
- **Priority**: Must Have

#### `FR-AI-004`: Safe Failure & Fallback Execution
- **Description**: Ensure agent errors, timeouts, or invalid outputs gracefully transition to safe failure without disrupting live operations.
- **Actor**: System Error Handler
- **Preconditions**: Agent execution times out or fails schema validation after retry limit.
- **Expected Behavior**: System logs execution failure, halts workflow, reverts transient state, and returns deterministic fallback options to user.
- **Acceptance Criteria**:
  1. Database state remains uncorrupted on AI failure.
  2. UI displays clear error summary and manual fallback operational controls.
- **Priority**: Must Have

#### `FR-AI-005`: AI Workflow State Persistence
- **Description**: Persist all AI workflow execution states, step logs, tool call parameters, and validation outcomes durably in PostgreSQL.
- **Actor**: System Backend
- **Preconditions**: Multi-agent workflow initiated.
- **Expected Behavior**: Backend writes `AiWorkflow`, `AiWorkflowStep`, and `AiToolCall` records to PostgreSQL at every step transition, enabling post-hoc audit and observability.
- **Acceptance Criteria**:
  1. All workflow execution data survives system restart.
  2. Workflow state queryable by `workflowId` via API.
- **Priority**: Must Have
- **Traceability**: `REQ-AI-07`, `REQ-DB-05`

#### `FR-AI-006`: Human Approval Boundary Enforcement
- **Description**: Pause execution of high-impact operational changes in `PendingManagerApproval` state until authorized by a Transport Manager.
- **Actor**: Validation & Safety Agent / Backend API
- **Preconditions**: AI recommends action classified as High-Impact.
- **Expected Behavior**: Backend transitions workflow state to `PendingManagerApproval`, halting automated execution until explicit manager decision is recorded.
- **Acceptance Criteria**:
  1. High-impact changes CANNOT proceed without an `Approve` decision.
  2. Approval decision is logged immutably with manager ID and timestamp.
- **Priority**: Must Have
- **Traceability**: `REQ-AI-06`

#### `FR-AI-007`: AI Workflow Observability & Audit Display
- **Description**: Provide a React UI interface displaying AI workflow execution states, tool call traces, step timings, validation summaries, and error logs.
- **Actor**: Transport Manager / Operator
- **Preconditions**: At least one AI workflow has been executed.
- **Expected Behavior**: React workspace renders workflow execution timeline with per-step tool call details, durations, validation results, and final outcomes.
- **Acceptance Criteria**:
  1. Interface lists workflow ID, step execution timeline, tool arguments, and validation status.
  2. Failed or retried tool calls are highlighted with error details.
- **Priority**: Must Have
- **Traceability**: `REQ-AI-07`, `REQ-FE-05`

#### `FR-AI-008`: Prompt Injection Resistance & Safe Input Handling
- **Description**: Sanitize user-supplied inputs in system prompts, apply execution timeouts, and enforce retry caps before triggering safe-failure transitions.
- **Actor**: System Backend / AI Subsystem
- **Preconditions**: User or external input incorporated into agent prompt.
- **Expected Behavior**: Backend strips injection patterns, enforces maximum execution duration, and caps retry attempts before defaulting to safe-failure state.
- **Acceptance Criteria**:
  1. Prompt injection payloads do not alter agent behavior.
  2. Execution timeout triggers graceful workflow termination.
- **Priority**: Must Have
- **Traceability**: `REQ-AI-08`, `REQ-SEC-05`

---

### 1.7 Notifications (`FR-NOTIF`)

#### `FR-NOTIF-001`: In-App & Push Notification Dispatch
- **Description**: Dispatch automated alerts for booking confirmations, schedule changes, disruption notices, and rebooking options.
- **Actor**: System Backend / Passenger
- **Preconditions**: Trigger event occurs (e.g., booking confirmed, service delayed).
- **Expected Behavior**: Backend inserts notification record in database and dispatches push payload to passenger's registered Flutter device token.
- **Acceptance Criteria**:
  1. Passengers can view notification history list in Flutter app.
  2. Disruption alerts highlight affected booking reference and action required.
- **Priority**: Should Have

---

### 1.8 Operator Management (`FR-OPERATOR`)

#### `FR-OPERATOR-001`: Manifest Generation & Boarding Verification
- **Description**: Generate departure passenger manifests and support boarding verification for bus drivers and dispatchers.
- **Actor**: Operator / Dispatcher
- **Preconditions**: Service scheduled for departure.
- **Expected Behavior**: System lists all confirmed passengers, seat numbers, boarding points, and payment statuses; allows marking boarding verification.
- **Acceptance Criteria**:
  1. Manifest can be exported or viewed live on React/Flutter.
  2. QR ticket scan marks passenger as `Boarded` in real time.
- **Priority**: Should Have

---

### 1.9 Manager Approval (`FR-APPROVAL`)

#### `FR-APPROVAL-001`: Manager Approval Decision Execution
- **Description**: Provide Transport Managers with UI controls to review pending high-impact operational proposals and log `Approve`, `Reject`, or `Request Revision` decisions.
- **Actor**: Transport Manager
- **Preconditions**: Workflow in `PendingManagerApproval` state.
- **Expected Behavior**: Manager inspects before/after passenger impact evidence in React approval workbench, submits signed decision, triggering backend execution or cancellation.
- **Acceptance Criteria**:
  1. `Approve` decision executes operational changes transactionally.
  2. `Reject` decision cancels proposed change and notifies dispatcher.
  3. `Request Revision` sends proposal back to AI/dispatcher with comments.
- **Priority**: Must Have

---

### 1.10 Audit Logging (`FR-AUDIT`)

#### `FR-AUDIT-001`: Operational & AI Audit Trail Logging
- **Description**: Maintain an immutable, searchable log of all high-impact operational changes, manager approvals, and AI agent execution steps.
- **Actor**: System / Administrator
- **Preconditions**: Operational or AI action performed.
- **Expected Behavior**: Backend writes audit log record containing Timestamp, User ID / Agent Name, Action Type, Entity ID, Before State, After State, and IP Address.
- **Acceptance Criteria**:
  1. Audit records cannot be edited or deleted via API.
  2. Administrators can search and filter audit logs in React workspace.
- **Priority**: Must Have

---

### 1.11 Frontend Requirements (`FR-FE`)

> The following frontend requirement IDs (`FR-FE-01` through `FR-FE-13`) are defined in the SE3090 Assignment Specification and extracted in [`project-analysis.md`](../project/project-analysis.md) Sections 4–5 (`REQ-FE-01` through `REQ-FE-13`). They are cross-referenced here to formalise their standing in this SRS document.

- `FR-FE-01` (**Role-Based Dashboard**): Summary widgets covering route/service occupancy, revenue, upcoming departures, and journey planning analytics. *(Source: `REQ-FE-01`)*
- `FR-FE-02` (**Business Data Management**): Full CRUD interfaces with validation, search, filtering, sorting, and pagination for routes, stops, services, buses, seat layouts, drivers, and fare rules. *(Source: `REQ-FE-02`)*
- `FR-FE-03` (**Disruption Workbench**): Workspace displaying candidate alternatives, resource feasibility evidence, and passenger impact analysis. *(Source: `REQ-FE-03`)*
- `FR-FE-04` (**Manager Approval Workbench**): Dedicated UI for Transport Managers to inspect before/after operational impacts and execute approve/reject/revision actions. *(Source: `REQ-FE-04`)*
- `FR-FE-05` (**AI Workflow Observability Dashboard**): UI displaying agent execution summaries, tool call histories, step timings, and deterministic validation outputs. *(Source: `REQ-FE-05`)*
- `FR-FE-06` (**Journey Search & Booking**): Passenger mobile screens for search, seat selection, booking, and payment. *(Source: `REQ-FE-06`)*
- `FR-FE-07` (**Interactive Seat Map**): Real-time seat map with hold/available/booked status indicators. *(Source: `REQ-FE-07`)*
- `FR-FE-08` (**Digital E-Ticket Wallet**): Mobile wallet displaying active and historical e-tickets with QR codes. *(Source: `REQ-FE-08`)*
- `FR-FE-09` (**Booking History & Cancellation**): Passenger booking history with cancellation and refund tracking. *(Source: `REQ-FE-09`)*
- `FR-FE-10` (**Disruption Alert Notifications**): In-app disruption alerts highlighting affected bookings and required actions. *(Source: `REQ-FE-10`)*
- `FR-FE-11` (**Rebooking Response**): Passenger screens for accepting alternative journey proposals or requesting full refunds. *(Source: `REQ-FE-11`)*
- `FR-FE-12` (**QR Ticket Boarding Scanner**): Operator mobile scanner for verifying passenger QR tickets at boarding. *(Source: `REQ-FE-12`)*
- `FR-FE-13` (**Passenger Manifest**): Departure manifest view with real-time boarding verification status. *(Source: `REQ-FE-13`)*

---

## 2. Non-Functional Requirements

### 2.1 Security Requirements (`NFR-SEC`)

- `NFR-SEC-001` (**Authentication Security**): Passwords MUST be hashed using BCrypt or Argon2 before storage. Plaintext passwords MUST NEVER be logged or returned in API responses.
- `NFR-SEC-002` (**Secret Management**): API keys, JWT signing keys, and database passwords MUST be loaded strictly from environment variables (`.env`). Secrets MUST NOT be committed to Git.
- `NFR-SEC-003` (**Transport Security**): All API communications between React/Flutter and ASP.NET Core MUST use HTTPS in production deployments.
- `NFR-SEC-004` (**Least Privilege Tool Execution**): AI agents MUST execute allow-listed tools using scoped DTO permissions. Agents CANNOT access system command shells or raw DB connections.
- `NFR-SEC-005` (**AI Security Guardrails**): AI agents MUST operate within an allow-listed tool sandbox with DTO-validated inputs. Agents CANNOT access system command shells, raw database connections, or unsigned API keys. Execution timeouts and retry caps MUST be enforced.
- `NFR-SEC-006` (**Audit Trail Completeness**): All high-impact operational changes, manager approval decisions, and AI tool executions MUST generate immutable audit log records that cannot be edited or deleted via the API.

### 2.2 Performance Requirements (`NFR-PERF`)

- `NFR-PERF-001` (**API Response Time**): Standard CRUD API endpoints MUST return responses within 200ms under normal load (<50 concurrent requests).
- `NFR-PERF-002` (**Journey Search Latency**): Deterministic candidate journey search queries MUST complete within 1.5 seconds for complex intercity route searches.
- `NFR-PERF-003` (**Concurrency Protection**): Database transaction locks MUST handle concurrent seat hold requests without double-booking or deadlock issues under high request rates.
- `NFR-PERF-004` (**AI Workflow Latency**): Multi-agent execution workflow MUST complete plan generation, tool execution, and validation within 10 seconds.

### 2.3 Testing & Quality Requirements (`NFR-TEST`)

- `NFR-TEST-001` (**Automated Test Coverage**): Backend API, database services, React components, and Flutter widgets MUST achieve comprehensive unit and integration test coverage.
- `NFR-TEST-002` (**CI Pipeline Enforcement**): GitHub Actions CI workflow MUST automatically build code and run all backend unit/integration tests on every push and PR to `main`.
- `NFR-TEST-003` (**Deterministic Agent Evaluation**): AI agent evaluation MUST include golden test cases, schema assertion checks, and safe-failure tests. LLM-as-a-judge may only serve as supporting evidence.

### 2.4 Deployment Requirements (`NFR-DEPLOY`)

- `NFR-DEPLOY-001` (**Cloud Availability**): ASP.NET Core API, PostgreSQL database, and React web app MUST be deployed to cloud hosting platforms (e.g., Render, Azure, Railway) with live URLs.
- `NFR-DEPLOY-002` (**Mobile Deliverable**): Flutter mobile application MUST be delivered as a runnable compiled Android APK (`.apk`).
- `NFR-DEPLOY-003` (**No-Cost Services**): System deployment MUST operate within institution-provided or free-tier cloud resources without requiring paid subscriptions.
