# WayPoint Business Rules & Governance Specification

This document provides the complete, formal **Business Rules & Governance Specification** for the **WayPoint** AI-Powered Intercity Journey Planner & Bus Operations Platform (**SE3090 Assignment 1**).

---

## Architectural Core Principle: Authoritative Backend Governance

```text
+-----------------------------------------------------------------------------------+
|                            ASP.NET CORE WEB API & POSTGRESQL                      |
|                     (AUTHORITATIVE DETERMINISTIC BUSINESS LOGIC & GOVERNANCE)      |
+-----------------------------------------------------------------------------------+
                                   ▲                            ▲
                                   │ HTTPS / REST               │ Internal Allow-Listed Tools
                                   │                            │ (Validated Inputs & Outputs)
+----------------------------------+---+      +-----------------+------------------+
|           CLIENT APPLICATIONS        |      |       AGENTIC AI SUBSYSTEM         |
|  - React Web (Operator Workspace)    |      |  - Planner / Coordinator Agent    |
|  - Flutter Mobile (Passenger App)    |      |  - Journey Analysis Agent          |
+--------------------------------------+      |  - Resource & Booking Agent        |
                                              |  - Validation & Safety Agent       |
                                              +------------------------------------+
```

### Deterministic vs. AI-Assisted Distinction Rule
1. **DETERMINISTIC BUSINESS LOGIC**: Authoritative C# code executed within ASP.NET Core Web API and PostgreSQL database transactions. It enforces strict mathematical, transactional, temporal, and permission constraints. It can **NEVER** be overridden, bypassed, or modified by AI reasoning.
2. **AI-ASSISTED DECISION SUPPORT**: Controlled multi-agent workflows that analyze objectives, evaluate options, and propose solutions. AI outputs are strictly **advisory** until validated deterministically by the backend and, where required, approved by a human Transport Manager.

---

## Section 1: Detailed Domain Business Rules (23 Domain Areas)

### 1.1 Authentication (`BR-AUTH`)
- `BR-AUTH-001` (**Deterministic Password Hashing**): Passwords must be hashed using BCrypt or Argon2 with a unique salt prior to database storage. Plaintext passwords must never be stored, logged, or returned.
- `BR-AUTH-002` (**Account Lockout Policy**): 5 consecutive failed login attempts lock the account for 15 minutes.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: N/A.
  - *Backend Must Verify*: Valid email format, password complexity rules, correct password hash.
  - *AI Prohibited From*: Accessing raw passwords, generating auth tokens, or bypassing credential validation.
  - *Manager Approval Required*: No.

### 1.2 Authorization (`BR-AUTHZ`)
- `BR-AUTHZ-001` (**Role-Based Access Control - RBAC**): Endpoints enforce role claims matching the canonical `UserRoleType` enum: `Passenger`, `Operator`, `TransportManager`, `Admin`. (The term *Administrator* is used as a functional descriptor).
- `BR-AUTHZ-002` (**Resource Ownership Lock**): Passengers can only view or cancel their own bookings (`Booking.PassengerId == Token.UserId`).
- **AI-Assisted Boundary**:
  - *AI May Recommend*: N/A.
  - *Backend Must Verify*: Valid signed JWT bearer token, active expiration, matching role/ownership claims.
  - *AI Prohibited From*: Modifying user roles, elevating permissions, or accessing unauthorized endpoints.
  - *Manager Approval Required*: No.

### 1.3 Journey Search (`BR-SEARCH`)
- `BR-SEARCH-001` (**Active Service Filter**): Search queries include only services where `IsActive == true` and `Status != Cancelled`.
- `BR-SEARCH-002` (**Corridor Network Matching**): Origin and destination stops must match valid route stop sequences.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Preference-aware journey rankings based on arrival deadlines, budget, or amenities.
  - *Backend Must Verify*: Returned candidate services actually exist in PostgreSQL and operate on requested date.
  - *AI Prohibited From*: Inventing non-existent bus routes, altering scheduled departure times, or displaying cancelled services.
  - *Manager Approval Required*: No (Automatic).

### 1.4 Route Selection (`BR-ROUTE`)
- `BR-ROUTE-001` (**Sequential Stop Order Enforcement**): Boarding stop sequence index MUST be strictly less than drop-off stop sequence index (`OriginSeq < DestinationSeq`).
- `BR-ROUTE-002` (**Segment Fare Calculation**): Distance-based segment pricing calculates fare as `BaseFare + (DistanceKm * RatePerKm) * ClassMultiplier`.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Alternative corridor routes during peak travel days.
  - *Backend Must Verify*: Route stop order validity, correct fare segment calculations.
  - *AI Prohibited From*: Modifying base fare tables or overriding segment sequence rules.
  - *Manager Approval Required*: No.

### 1.5 Transfer Feasibility (`BR-TRANSFER`)
- `BR-TRANSFER-001` (**Minimum Transfer Time Window**): For connecting journeys, `Leg2.DepartureTime - Leg1.ArrivalTime` MUST be $\ge$ **20 minutes**.
- `BR-TRANSFER-002` (**Matching Transfer Hub**): `Leg1.DestinationStopId` MUST equal `Leg2.OriginStopId`.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Connecting journey candidates with multi-hub transfers.
  - *Backend Must Verify*: Exact 20-minute minimum transfer buffer and matching transfer hub stop IDs.
  - *AI Prohibited From*: Proposing connecting journeys with transfer buffers $<20$ minutes.
  - *Manager Approval Required*: No (Automatic).

### 1.6 Timetable Validation (`BR-TIME`)
- `BR-TIME-001` (**Resource Schedule Non-Overlap**): A bus or driver CANNOT be assigned to overlapping departure schedules (`DepartureTime` to `ArrivalTime`).
- `BR-TIME-002` (**Future Departure Precondition**): New bookings can only be placed on services with `DepartureTime > UtcNow`.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Timetable shift adjustments during disruption recovery.
  - *Backend Must Verify*: Absence of driver/bus schedule overlaps in PostgreSQL.
  - *AI Prohibited From*: Altering timetable schedules directly without backend API validation.
  - *Manager Approval Required*: **Yes** (if timetable shift $>15$ minutes).

### 1.7 Seat Availability (`BR-SEAT`)
- `BR-SEAT-001` (**Authoritative Seat Status Matrix**): A seat is `Available` ONLY if Seat ID is NOT in active `SeatHolds` (`HeldUntil > UtcNow`) AND NOT in confirmed `Bookings`.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Seat choices based on passenger preferences (e.g., window vs. aisle).
  - *Backend Must Verify*: Exact real-time seat availability state queried from PostgreSQL.
  - *AI Prohibited From*: Marking occupied or held seats as available.
  - *Manager Approval Required*: No.

### 1.8 Seat Holds (`BR-HOLD`)
- `BR-HOLD-001` (**10-Minute Hold Duration**): Seat holds grant exclusive reservation for exactly 10 minutes (`HeldUntil = UtcNow + 10m`).
- `BR-HOLD-002` (**Concurrency Isolation**): Database transactions (`IDbContextTransaction`) prevent concurrent holds on the same seat.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: N/A.
  - *Backend Must Verify*: Seat status is `Available` before inserting `SeatHold` record.
  - *AI Prohibited From*: Extending hold durations beyond 10 minutes or bypassing hold transactions.
  - *Manager Approval Required*: No.

### 1.9 Booking (`BR-BOOK`)
- `BR-BOOK-001` (**Payment Precondition for Booking**): A `Booking` record with status `Confirmed` is created ONLY upon successful payment authorization.
- `BR-BOOK-002` (**Transactional Integrity**): Booking creation, seat map locking, and ticket generation MUST occur inside a single database transaction.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Bookable journey option alternatives.
  - *Backend Must Verify*: Payment authorization code, active seat hold validity.
  - *AI Prohibited From*: Creating confirmed bookings without verified payment.
  - *Manager Approval Required*: No (Automatic).

### 1.10 Payment (`BR-PAY`)
- `BR-PAY-001` (**Dual-Strategy Sandbox Payment Verification**): Backend verifies payment authorization via ASP.NET Core (`FR-BE-06`). The system adopts a dual-strategy: an internal ASP.NET Core Mock Payment Gateway with configurable test cards (simulating instant approval, insufficient funds, and network failure) for 100% reliable viva exam demonstrations, with an optional Stripe Test Mode proxy.
- `BR-PAY-002` (**Failed Payment Release**): Payment failure or timeout immediately releases `SeatHold` back to `Available`.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: N/A.
  - *Backend Must Verify*: Sandbox gateway token validity, exact charge amount matching booking total.
  - *AI Prohibited From*: Accessing payment card data, tokens, or bypassing gateway verification.
  - *Manager Approval Required*: No.

### 1.11 Ticket Generation (`BR-TICK`)
- `BR-TICK-001` (**Cryptographic QR Payload**): Tickets generate a unique UUID and a signed QR code payload containing `BookingRef`, `ServiceId`, `SeatNo`, and `HMAC-SHA256` signature.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: N/A.
  - *Backend Must Verify*: Cryptographic signature validity during scanning (`FR-OPERATOR-001`).
  - *AI Prohibited From*: Generating, forging, or altering digital ticket payloads.
  - *Manager Approval Required*: No.

### 1.12 Cancellation (`BR-CANCEL`)
- `BR-CANCEL-001` (**Departure Offset Restriction**): Passenger cancellations are permitted only prior to departure (`DepartureTime > UtcNow`).
- `BR-CANCEL-002` (**Immediate Inventory Release**): Cancelling a booking immediately resets associated seats to `Available`.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Cancellation option during disruption rebooking workflow.
  - *Backend Must Verify*: Active booking status, departure time offset.
  - *AI Prohibited From*: Cancelling bookings outside policy rules.
  - *Manager Approval Required*: No (for passenger-initiated cancellations).

### 1.13 Refund Eligibility (`BR-REFUND`)
- `BR-REFUND-001` (**Tiered Passenger Refund Schedule**):
  - Departure offset $>24$ hours: **90% refund** (10% processing fee retained).
  - Departure offset $12 \text{ to } 24$ hours: **50% refund**.
  - Departure offset $<12$ hours: **0% refund** (non-refundable).
- `BR-REFUND-002` (**Operator Disruption Full Refund**): Operator-initiated service cancellations grant **100% full refund** to all ticketed passengers.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Refund proposal options during disruption recovery.
  - *Backend Must Verify*: Departure time timestamp delta and exact percentage calculation.
  - *AI Prohibited From*: Arbitrarily altering refund percentages.
  - *Manager Approval Required*: No (policy driven).

### 1.14 Service Disruption (`BR-DISRUPT`)
- `BR-DISRUPT-001` (**Disruption Case Logging**): Disruption events log a `DisruptionCase` record with severity (`Minor`, `Major`, `Critical`) and link affected service IDs.
- `BR-DISRUPT-002` (**Service Status Transition**): Affected service status transitions to `Disrupted` or `Cancelled`.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Rebooking proposal options, passenger re-assignment plans.
  - *Backend Must Verify*: Valid disruption ID, affected service linkage, accurate affected passenger count.
  - *AI Prohibited From*: Deleting disruption records or altering historical booking logs.
  - *Manager Approval Required*: **Yes** (if proposed remedy cancels ticketed services).

### 1.15 Replacement Resource Selection (`BR-RESOURCE`)
- `BR-RESOURCE-001` (**Seat Capacity Rule**): `ReplacementBus.SeatCapacity` MUST be $\ge$ `DisruptedService.BookedPassengerCount`.
- `BR-RESOURCE-002` (**Driver Duty Hours Rule**): Replacement driver MUST have $\ge$ 8 hours off-duty rest prior to assignment.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Replacement bus and driver pairings from unassigned fleet inventory.
  - *Backend Must Verify*: Exact seat capacity math and driver duty logs in PostgreSQL.
  - *AI Prohibited From*: Assigning buses with insufficient seat capacity or off-duty drivers under maintenance.
  - *Manager Approval Required*: **Yes** (if bus/driver reassignment causes schedule conflict).

### 1.16 Rebooking (`BR-REBOOK`)
- `BR-REBOOK-001` (**Transactional Passenger Transfer**): Rebooking transfers passenger from disrupted service to replacement service seats inside `IDbContextTransaction`.
- `BR-REBOOK-002` (**Fare Protection Guarantee**): Rebooked passengers are NOT charged additional fees if replacement service fare is higher. If lower, passenger receives fare difference refund.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Rebooking candidate pairings (Passenger A $\rightarrow$ Service S-202, Seat 12A).
  - *Backend Must Verify*: Seat availability on new service, transactional execution.
  - *AI Prohibited From*: Double-booking replacement seats or charging extra fees.
  - *Manager Approval Required*: **Yes** (for high-impact disruption remedies).

### 1.17 Passenger Impact Calculation (`BR-IMPACT`)
- `BR-IMPACT-001` (**Impact Metric Computation**): Impact metrics MUST calculate:
  $$\text{AffectedCount} = \sum \text{Confirmed Bookings}$$
  $$\text{TotalDelayMinutes} = \text{NewArrivalTime} - \text{OriginalArrivalTime}$$
  $$\text{NetFareDelta} = \text{ReplacementFare} - \text{OriginalFare}$$
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Qualitative impact summary narratives for manager review.
  - *Backend Must Verify*: Exact numerical mathematical calculations.
  - *AI Prohibited From*: Falsifying passenger counts or delay metrics.
  - *Manager Approval Required*: No.

### 1.18 Manager Approval (`BR-APPROVAL`)
- `BR-APPROVAL-001` (**Approval Gate Classification**):
  - **Automatic (No Manager Approval)**: Journey search ranking, seat holds, payment confirmation, policy cancellations, displaying AI alternatives.
  - **Pending Manager Approval (`PendingManagerApproval`)**:
    1. Cancelling a ticketed service.
    2. Timetable shift $>15$ minutes.
    3. Disruptive bus/driver reassignment affecting ticketed passengers.
- `BR-APPROVAL-002` (**Manager Signature**): Approval decisions require an immutable `ApprovalDecision` record signed by a Transport Manager ID.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Severity classification (`Low` vs `High`) and `RequestManagerApproval` tool call.
  - *Backend Must Verify*: Transport Manager JWT credentials, proposal state `PendingManagerApproval`.
  - *AI Prohibited From*: Approving its own proposals or bypassing the manager state gate.
  - *Manager Approval Required*: **Yes** (Enforced by backend).

### 1.19 Operational Change Application (`BR-APPLY`)
- `BR-APPLY-001` (**Post-Approval Execution Precondition**): Operational DB mutations for high-impact remedies occur ONLY when `ApprovalDecision.Status == Approved`.
- `BR-APPLY-002` (**Atomic Application**): Applying changes executes inside a single database transaction (all passenger bookings updated or complete rollback).
- **AI-Assisted Boundary**:
  - *AI May Recommend*: N/A.
  - *Backend Must Verify*: Signed manager approval record before calling `ApplyApprovedOperationalChange`.
  - *AI Prohibited From*: Executing DB mutations without approved manager signature.
  - *Manager Approval Required*: **Yes**.

### 1.20 Notifications (`BR-NOTIF`)
- `BR-NOTIF-001` (**Event-Driven Alert Dispatch**): Notifications trigger automatically on booking confirmation, service disruption, rebooking, and refund issuance.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Personalized notification text phrasing.
  - *Backend Must Verify*: Target user ID, device token, notification format sanitization.
  - *AI Prohibited From*: Sending unauthorized broadcast spam or misleading alerts.
  - *Manager Approval Required*: No.

### 1.21 AI Tool Access (`BR-AITOOL`)
- `BR-AITOOL-001` (**10 Allow-Listed Tool Sandbox**): AI agents can interact with system data ONLY via 10 allow-listed tools:
  `SearchRoutes`, `GetBoardingPoints`, `CheckTransferFeasibility`, `CheckSeatAvailability`, `CalculateFareDifference`, `CreateRebookingProposal`, `CalculatePassengerImpact`, `RequestManagerApproval`, `ApplyApprovedOperationalChange`, `SendPassengerNotification`.
- `BR-AITOOL-002` (**Tool Input DTO Validation**): Every tool invocation input MUST pass backend DTO schema validation prior to execution.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Tool selection and argument payloads.
  - *Backend Must Verify*: Argument DTO schema validity, parameter bounds, role permissions.
  - *AI Prohibited From*: Executing direct SQL, shell commands, arbitrary HTTP calls, or unlisted tools.
  - *Manager Approval Required*: No (tool level).

### 1.22 AI Validation (`BR-AIVAL`)
- `BR-AIVAL-001` (**Deterministic Server Assertion**): Backend validates AI outputs against domain rules (route existence, seat map math, fare rules) before accepting results.
- `BR-AIVAL-002` (**Max Retries & Safe Failure**): If AI output fails validation, backend requests output correction (max 3 retries). On 3rd failure, workflow transitions to `SafeFailure` state.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: Corrected JSON output during retry loops.
  - *Backend Must Verify*: Schema compliance, business rule assertions, retry counter.
  - *AI Prohibited From*: Overriding validation failures or infinitely looping.
  - *Manager Approval Required*: No.

### 1.23 Audit Logging (`BR-AUDIT`)
- `BR-AUDIT-001` (**Immutable Audit Records**): All high-impact operational changes, manager approvals, and AI tool calls generate immutable audit rows (`Timestamp`, `ActorId`/`AgentName`, `ActionType`, `EntityId`, `BeforeState`, `AfterState`).
- `BR-AUDIT-002` (**Zero Edit/Delete API**): Audit log tables have no `UPDATE` or `DELETE` API endpoints exposed.
- **AI-Assisted Boundary**:
  - *AI May Recommend*: N/A.
  - *Backend Must Verify*: Automatic DB interceptor logging on entity save.
  - *AI Prohibited From*: Modifying, suppressing, or deleting audit log entries.
  - *Manager Approval Required*: No.

---

## Section 2: AI-Assisted Operations Governance Matrix

The table below explicitly governs every AI-assisted operation across the 23 domain areas:

| Domain Area | AI May Recommend | Backend Deterministic Validation Must Verify | AI Prohibited Actions | Manager Approval Required? |
| :--- | :--- | :--- | :--- | :--- |
| **Authentication & AuthZ** | N/A | Password hashes, JWT tokens, RBAC claims. | Accessing raw passwords, issuing tokens, elevating roles. | **No** |
| **Journey Search & Ranking** | Ranked itinerary options matching preferences. | Route existence, active service status, scheduled date match. | Inventing non-existent routes, showing cancelled services. | **No** (Automatic) |
| **Route & Timetable Admin** | Alternative corridor route suggestions. | Stop sequence order, fare formulas, non-overlapping schedules. | Modifying base fare tables directly, altering schedules without validation. | **Yes** (if timetable shift $>15$ mins) |
| **Transfer Feasibility** | Connecting journey candidate pairs. | Minimum 20-minute transfer window, matching hub stop IDs. | Proposing transfer windows $<20$ minutes. | **No** (Automatic) |
| **Seat Availability & Holds** | Seat preference recommendations (window/aisle). | Real-time seat status in PostgreSQL, 10-minute hold lock inside DB transaction. | Marking occupied seats as available, extending hold timeouts beyond 10 mins. | **No** (Automatic) |
| **Booking & Payment** | Bookable journey alternative cards. | Gateway authorization code, seat hold validity, DB transaction. | Creating confirmed bookings without payment, accessing card credentials. | **No** (Automatic) |
| **Ticket Generation** | N/A | Cryptographic HMAC-SHA256 QR payload signature. | Generating, altering, or forging digital QR ticket payloads. | **No** (Automatic) |
| **Cancellation & Refund** | Cancellation option during disruption recovery. | Departure offset timestamps, exact policy percentage math. | Altering policy refund percentages, cancelling past journeys. | **No** (Policy driven) |
| **Disruption Management** | Rebooking proposals, alternative service pairings. | Affected passenger count, valid disruption case linkage. | Deleting disruption logs, altering historical booking records. | **Yes** (if ticketed service cancelled) |
| **Replacement Resources** | Unassigned bus and driver pairings. | `ReplacementCapacity >= AffectedPassengers`, driver rest hours $\ge$ 8h. | Assigning buses under maintenance or drivers lacking rest hours. | **Yes** (if schedule conflict caused) |
| **Rebooking Execution** | Passenger-to-seat rebooking mappings. | Replacement seat availability, atomic DB transaction. | Double-booking seats, charging extra fees for disruption rebooking. | **Yes** (High-impact changes) |
| **Passenger Impact** | Qualitative impact summary text. | Exact numerical formula calculations for count, delay mins, and fare delta. | Falsifying affected passenger counts or delay metrics. | **No** |
| **Manager Approval Boundary** | Severity classification (`Low` vs `High`), `RequestManagerApproval` tool call. | Signed Transport Manager JWT, proposal state `PendingManagerApproval`. | Approving own proposals, bypassing human manager approval gate. | **Yes** (Backend enforced) |
| **Operational Change Application** | N/A | Valid signed manager approval record, atomic DB transaction. | Executing DB mutations without manager signature. | **Yes** |
| **Notifications** | Personalized message text phrasing. | Target user ID, device token, message sanitization. | Sending unauthorized broadcast spam or misleading alerts. | **No** |
| **AI Tool Access** | Tool selection and argument payloads. | Tool allow-list (10 tools), input DTO schema validation. | Direct SQL, shell access, arbitrary HTTP requests, unlisted tools. | **No** |
| **AI Validation & Resilience** | Corrected JSON payloads during retry loops. | Schema assertions, rule compliance, max 3 retries before `SafeFailure`. | Overriding validation failures, infinite retry loops. | **No** |
| **Audit Logging** | N/A | Automatic DB interceptor logging on entity save. | Modifying, suppressing, or deleting audit log rows. | **No** |
