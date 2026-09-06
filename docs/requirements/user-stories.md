# WayPoint User Stories

This document outlines the user stories for the **WayPoint** AI-Powered Intercity Journey Planner & Bus Operations Platform across all four primary user roles: **Passenger**, **Operator / Dispatcher**, **Transport Manager**, and **Administrator**.

---

## 1. Passenger User Stories (`US-PASS`)

### `US-PASS-001`: Account Registration & Authentication
- **User Story**: *As a Passenger*, I want to create an account and log in securely on my mobile phone, *so that* I can access saved bookings, e-tickets, and personalized journey recommendations.
- **Mapped Requirement**: `FR-AUTH-001`, `FR-AUTH-002`, `FR-FE-07`
- **Acceptance Criteria**:
  1. Registration requires valid email, full name, phone number, and password.
  2. Successful login stores JWT securely in `flutter_secure_storage` and navigates to the home screen.
  3. Invalid login displays a user-friendly error message.
- **Priority**: Must Have

### `US-PASS-002`: Intercity Journey Search & Preferences
- **User Story**: *As a Passenger*, I want to search for travel options by entering origin, destination, travel date, and preferences (e.g., arrival deadline, direct bus, budget, AC/Wi-Fi amenities), *so that* I can find the best journey suited to my schedule.
- **Mapped Requirement**: `FR-JOURNEY-003`, `FR-JOURNEY-004`, `FR-FE-08`
- **Acceptance Criteria**:
  1. Search displays candidate journeys clearly comparing departure time, arrival time, travel duration, total fare, directness, and amenities.
  2. Results distinguish between direct services and feasible connecting services.
  3. Preference filters immediately update and rank search results.
- **Priority**: Must Have

### `US-PASS-003`: Interactive Seat Selection & Temporary Hold
- **User Story**: *As a Passenger*, I want to view a visual seat layout map of the bus and select my preferred seats, *so that* I can hold them temporarily while I complete payment.
- **Mapped Requirement**: `FR-FLEET-003`, `FR-BOOKING-001`, `FR-FE-10`
- **Acceptance Criteria**:
  1. Seat map displays live status (`Available`, `Held`, `Booked`).
  2. Selecting available seats places a temporary 10-minute hold on the server.
  3. A countdown timer displays remaining hold time before automatic release.
- **Priority**: Must Have

### `US-PASS-004`: Payment Sandbox Checkout & QR E-Ticket Issuance
- **User Story**: *As a Passenger*, I want to complete checkout via a secure payment sandbox and receive a digital e-ticket with a QR code, *so that* I have guaranteed seat confirmation and can board the bus easily.
- **Mapped Requirement**: `FR-BOOKING-002`, `FR-BOOKING-003`, `FR-FE-11`
- **Acceptance Criteria**:
  1. Successful sandbox payment converts temporary seat hold into a confirmed booking transactionally.
  2. Passenger receives a digital e-ticket containing journey details and a scannable QR code.
  3. E-tickets are accessible offline within the mobile app wallet.
- **Priority**: Must Have

### `US-PASS-005`: Booking Cancellation & Refund Request
- **User Story**: *As a Passenger*, I want to cancel an eligible booking prior to departure, *so that* I can receive a refund according to policy rules.
- **Mapped Requirement**: `FR-BOOKING-004`, `FR-FE-12`
- **Acceptance Criteria**:
  1. System displays eligible refund percentage based on time remaining before departure.
  2. Confirming cancellation releases seats back to available inventory and records refund status.
- **Priority**: Should Have

### `US-PASS-006`: Disruption Rebooking Response
- **User Story**: *As a Passenger*, I want to be notified if my booked bus service is disrupted and receive AI-recommended alternative journey choices, *so that* I can rebook quickly without losing my trip.
- **Mapped Requirement**: `FR-DISRUPTION-002`, `FR-DISRUPTION-004`, `FR-NOTIF-001`
- **Acceptance Criteria**:
  1. Disruption alerts arrive via push/in-app notification detailing service changes.
  2. Passenger can view and accept AI-generated alternative options or request a full refund.
- **Priority**: Must Have

---

## 2. Operator / Dispatcher User Stories (`US-OP`)

### `US-OP-001`: Route & Timetable Administration
- **User Story**: *As an Operator/Dispatcher*, I want to create and manage intercity routes, intermediate stops, tourist corridor destinations, and departure timetables, *so that* passengers can search and book valid transport services.
- **Mapped Requirement**: `FR-JOURNEY-001`, `FR-JOURNEY-002`, `FR-FE-02`
- **Acceptance Criteria**:
  1. Interfaces support adding, editing, listing, and soft-deleting routes and stops.
  2. Timetables enforce non-conflicting departure schedules.
- **Priority**: Must Have

### `US-OP-002`: Fleet & Driver Resource Management
- **User Story**: *As an Operator/Dispatcher*, I want to manage bus inventory, seat layout templates, maintenance statuses, and driver assignments, *so that* operations run smoothly and safely.
- **Mapped Requirement**: `FR-FLEET-001`, `FR-FLEET-002`, `FR-FE-02`
- **Acceptance Criteria**:
  1. Buses under maintenance cannot be assigned to active departures.
  2. Driver assignment checks for schedule overlaps and rest hour policies.
- **Priority**: Must Have

### `US-OP-003`: Disruption Logging & Resource Feasibility Check
- **User Story**: *As an Operator/Dispatcher*, I want to log service disruptions (e.g., breakdown, road closure) and evaluate replacement bus/driver feasibility, *so that* affected passengers can be accommodated.
- **Mapped Requirement**: `FR-FLEET-004`, `FR-DISRUPTION-001`, `FR-FE-03`
- **Acceptance Criteria**:
  1. Disruption case logs severity level, affected service ID, and affected passenger count.
  2. System runs resource feasibility check to find available replacement buses with sufficient seat capacity.
- **Priority**: Must Have

### `US-OP-004`: Manifest Inspection & QR Boarding Scanning
- **User Story**: *As an Operator/Dispatcher*, I want to view departure passenger manifests and scan QR e-tickets at boarding points, *so that* I can verify passenger boarding efficiently.
- **Mapped Requirement**: `FR-OPERATOR-001`, `FR-BOOKING-003`
- **Acceptance Criteria**:
  1. Manifest lists confirmed passenger names, seat numbers, and boarding status.
  2. Scanning a valid QR code updates passenger status to `Boarded` in real time.
- **Priority**: Should Have

---

## 3. Transport Manager User Stories (`US-MGR`)

### `US-MGR-001`: Disruption Review & Before/After Impact Inspection
- **User Story**: *As a Transport Manager*, I want to review pending high-impact operational proposals (e.g., ticketed service cancellations, major timetable shifts) along with before/after passenger impact evidence, *so that* I can make accountable approval decisions.
- **Mapped Requirement**: `FR-DISRUPTION-003`, `FR-APPROVAL-001`, `FR-FE-04`
- **Acceptance Criteria**:
  1. Approval workbench displays affected passenger count, financial impact, alternative feasibility, and AI validation evidence.
  2. Actions are gated: proposals remain paused in `PendingManagerApproval` state until acted upon.
- **Priority**: Must Have

### `US-MGR-002`: Manager Approval Execution
- **User Story**: *As a Transport Manager*, I want to execute `Approve`, `Reject`, or `Request Revision` decisions on operational proposals, *so that* approved remedies can be executed transactionally and unapproved changes blocked.
- **Mapped Requirement**: `FR-APPROVAL-001`, `FR-DISRUPTION-004`
- **Acceptance Criteria**:
  1. Executing `Approve` triggers backend transactional rebooking and passenger notification.
  2. Executing `Reject` cancels the proposed remedy and logs the manager's reason.
  3. Every decision logs an immutable record in the audit trail.
- **Priority**: Must Have

### `US-MGR-003`: Agent Execution Summary & Observability Inspection
- **User Story**: *As a Transport Manager*, I want to inspect AI agent execution summaries, tool call histories, step timings, and deterministic validation outputs, *so that* I can verify the safety and compliance of AI-recommended solutions.
- **Mapped Requirement**: `FR-AI-001`, `FR-AI-007`, `FR-FE-05`
- **Acceptance Criteria**:
  1. Interface lists workflow ID, prompt objective, step execution timeline, tool arguments, validation status, and final state.
  2. Failed or retried tool calls are highlighted clearly with error details.
- **Priority**: Must Have

---

## 4. Administrator User Stories (`US-ADMIN`)

### `US-ADMIN-001`: User & Role Management
- **User Story**: *As an Administrator*, I want to manage user accounts, assign operational roles (Passenger, Operator, Manager, Admin), and configure system settings, *so that* application security and access control are maintained.
- **Mapped Requirement**: `FR-AUTH-003`, `FR-BE-03`
- **Acceptance Criteria**:
  1. Administrator can grant or revoke staff roles.
  2. Role updates take effect upon next token refresh.
- **Priority**: Must Have

### `US-ADMIN-002`: System Audit Trail & Compliance Inspection
- **User Story**: *As an Administrator*, I want to search and filter operational and AI audit logs, *so that* I can conduct compliance audits and investigate system events.
- **Mapped Requirement**: `FR-AUDIT-001`, `NFR-SEC-006`
- **Acceptance Criteria**:
  1. Audit trail displays timestamp, actor ID/agent name, action type, entity ID, and state diff.
  2. Audit logs are immutable and cannot be deleted or altered via the UI.
- **Priority**: Must Have
