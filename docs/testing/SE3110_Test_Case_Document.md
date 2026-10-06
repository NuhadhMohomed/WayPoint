# SE3110 — Software Testing & Quality Evaluation
## Comprehensive Test Case Specification Document
### Target System: WayPoint — Integrated Full-Stack & Agentic AI Transit Platform

---

## 📌 Document Control & Metadata

| Field | Detail |
| :--- | :--- |
| **Module** | **SE3110: Quality Management in Software Engineering** |
| **Academic Year / Semester** | Year 3, Semester 1 — Academic Year 2026 |
| **Institution** | Sri Lanka Institute of Information Technology (SLIIT) |
| **Project Title** | WayPoint Transit Operations & Journey Planning Platform |
| **Document Type** | Comprehensive Test Case Specification Repository |
| **Version** | 1.0 (Final Release) |
| **Date** | 8th October 2026 |
| **Total Test Cases** | 68 Structured Test Specifications (Traceable to 448 Automated Executions) |
| **Overall Pass Rate** | **100% (448 / 448 automated test instances passed)** |

---

## 👥 Student Component & Responsibility Matrix

| Student Name | Student IT Number | Assigned Component | Testing Focus & Scope |
| :--- | :--- | :--- | :--- |
| **Vinranga M.W.S** | **IT24100374** | **Component 1**:<br>Journey Planning & Routes | Route catalogue, timetables, connecting transfer window logic (`BR-TRANSFER-001`), journey search, Journey Agent. |
| **Mohomed N.M.N.**<br>*(Leader)* | **IT24102476** | **Component 2**:<br>Fleet, 2D Seats & Operations | Bus fleet inventory, 2D seat map designer, driver rostering rest hours (`BR-TIME-001`), Resource Feasibility Agent. |
| **Dissanayaka A.D.M.N.K.** | **IT24100225** | **Component 3**:<br>Seat Hold, Payments & QR | 10-minute hold concurrency lock (`BR-HOLD-001`), payment sandbox, HMAC-SHA256 QR tickets, tiered refunds (`BR-REFUND-001`). |
| **Dineth sasmitha J.S.D.** | **IT24102912** | **Component 4**:<br>Disruption, Approval & Safety | Disruption intake, passenger blast radius, Transport Manager approval gate (`BR-APPROVAL-001`), conductor scanner, Safety Agent. |

---

## 1. Testing Category 1: Authoritative Backend & API Testing (.NET 8 / xUnit)

| Test Case ID | Component / Feature | Test Type | Preconditions | Inputs & Execution Steps | Expected Result | Actual Result | Status | Traceability | Member |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`TC-BE-001`** | Booking / Tiered Refund | Unit (Normal) | Booking exists $>24$h before departure | Calculate refund for Rs. 5,700 fare at $T-30$ hours. | 90% refund (Rs. 5,130), 10% cancellation fee retained. | Exact Rs. 5,130 refund calculated. | **Passed** | `BR-REFUND-001` | Mithila |
| **`TC-BE-002`** | Booking / Tiered Refund | Unit (Boundary) | Booking exists $12\text{--}24$h before departure | Calculate refund for Rs. 5,700 fare at $T-18$ hours. | 50% refund (Rs. 2,850), 50% cancellation fee. | Exact Rs. 2,850 refund calculated. | **Passed** | `BR-REFUND-001` | Mithila |
| **`TC-BE-003`** | Booking / Tiered Refund | Unit (Edge/Fail) | Booking exists $<12$h before departure | Calculate refund for Rs. 5,700 fare at $T-6$ hours. | 0% refund (Rs. 0.00), non-refundable message returned. | Rs. 0 refund returned; cancellation fee 100%. | **Passed** | `BR-REFUND-001` | Mithila |
| **`TC-BE-004`** | Security / HMAC QR Ticket | Unit (Normal) | Legitimate booking reference `WP-7B92K1` | Invoke `GenerateTicketPayloadAsync` with valid booking data. | Returns HMAC-SHA256 signed payload string with `WP\|` prefix. | Valid 64-char HMAC token produced and verified. | **Passed** | `BR-HMAC-001` | Mithila |
| **`TC-BE-005`** | Security / HMAC QR Ticket | Unit (Invalid/Tamper) | Authentic QR ticket generated | Modify seat from `4A` to `1A` in payload and call `VerifyTicketQrAsync`. | Verification fails with `Security Violation: Tampered Signature`. | IsValid=False; Security violation flagged. | **Passed** | `BR-HMAC-001` | Mithila |
| **`TC-BE-006`** | Booking / Payment Sandbox | Unit (Normal) | Sandbox gateway active | Submit card `4000 0000 0000 0001` with amount Rs. 5,700. | Gateway returns `IsSuccess=True` with `TXN-` transaction ID. | Status `Success`, `TXN-` generated. | **Passed** | `US-PASS-004` | Mithila |
| **`TC-BE-007`** | Booking / Payment Sandbox | Unit (Failure) | Sandbox gateway active | Submit card `4000 0000 0000 0002` (declined preset). | Gateway returns `IsSuccess=False`, GatewayStatus=`Declined`. | IsSuccess=False, Declined status returned. | **Passed** | `US-PASS-004` | Mithila |
| **`TC-BE-008`** | Disruption / Impact Severity | Unit (Normal) | Disrupted service logged | Timetable shift is 10 minutes ($<15$ min). | Impact classified as `Low`; auto-executable by operator. | Impact=`Low` returned. | **Passed** | `BR-DISRUPT-001` | Dineth |
| **`TC-BE-009`** | Disruption / Impact Severity | Unit (Boundary) | Disrupted service logged | Timetable shift is exactly 15 minutes. | Impact classified as `Low` (inclusive threshold). | Impact=`Low` returned. | **Passed** | `BR-DISRUPT-001` | Dineth |
| **`TC-BE-010`** | Disruption / Impact Severity | Unit (Boundary) | Disrupted service logged | Timetable shift is 16 minutes ($>15$ min). | Impact classified as `High`; requires Transport Manager sign-off. | Impact=`High` returned. | **Passed** | `BR-APPROVAL-001` | Dineth |
| **`TC-BE-011`** | Fleet / Driver Rest Rules | Unit (Normal) | Driver assigned to Shift A ending 10:00 | Assign Driver to Shift B starting 19:00 (9 hrs gap). | Assignment accepted; rest buffer exceeds mandatory 8 hours. | Validation passed; no overlap. | **Passed** | `BR-TIME-001` | Nuhadh |
| **`TC-BE-012`** | Fleet / Driver Rest Rules | Unit (Invalid) | Driver assigned to Shift A ending 10:00 | Assign Driver to Shift B starting 15:00 (5 hrs gap). | Assignment rejected; violates 8-hour mandatory rest gap. | Validation failed with labor safety exception. | **Passed** | `BR-TIME-001` | Nuhadh |

---

## 2. Testing Category 2: Relational Database & Concurrency Testing (PostgreSQL / EF Core)

| Test Case ID | Component / Feature | Test Type | Preconditions | Inputs & Execution Steps | Expected Result | Actual Result | Status | Traceability | Member |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`TC-DB-001`** | SeatHold / Concurrency Token | Integration (Race Condition) | Service active, seat `12A` available in DB | Fire two parallel asynchronous hold requests for seat `12A` simultaneously. | Exactly 1 request receives HTTP `200/201 OK`; exactly 1 request receives HTTP `409 Conflict`. | Request 1 = 200 OK; Request 2 = 409 Conflict. Zero double-holds. | **Passed** | `REQ-TEST-02` | Mithila |
| **`TC-DB-002`** | SeatHold / Expiry Lifecycle | Integration (Boundary) | Hold created at $T_0$ with 10-minute expiry | Query seat status at $T_0 + 9\text{m }50\text{s}$ vs $T_0 + 10\text{m }01\text{s}$. | Seat status transitions from `Held` to `Available` after 10 minutes. | Hold expired automatically; seat returned to available inventory. | **Passed** | `BR-HOLD-001` | Mithila |
| **`TC-DB-003`** | Identity / Unique Constraint | Integration (Invalid) | User `passenger@waypoint.lk` exists in DB | Attempt `INSERT` with duplicate email address. | PostgreSQL throws `23505 UniqueViolation`; EF Core throws `DbUpdateException`. | HTTP 400/409 duplicate registration rejected. | **Passed** | Security / DB | Nuhadh |
| **`TC-DB-004`** | Audit / Log Hash Chain | Integration (Data Integrity) | Operational change committed | Verify SHA-256 tamper-evident hash chain linking log entries. | Each audit entry hashes previous record's hash + current payload. | Hash integrity verified across log series. | **Passed** | `REQ-AUDIT-01` | Dineth |
| **`TC-DB-005`** | Fleet / Seat Layout Matrix | Integration (Constraint) | Bus layout defined with 10 rows $\times$ 4 cols | Insert seats violating foreign key or matrix bounds. | Constraint violation prevents orphaned seat records. | Relational integrity strictly maintained. | **Passed** | `REQ-FLEET-01` | Nuhadh |

---

## 3. Testing Category 3: React Web Frontend Testing (Vitest & RTL)

| Test Case ID | Component / Feature | Test Type | Preconditions | Inputs & Execution Steps | Expected Result | Actual Result | Status | Traceability | Member |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`TC-WEB-001`** | Journey / RouteManager | Component (Normal) | Operator logged in | Render `RouteManagerPage` with mocked catalogue data. | Renders route list table, corridor badges, and stop creation modal. | Component rendered cleanly; all columns visible. | **Passed** | `REQ-TEST-03` | Sethum |
| **`TC-WEB-002`** | Fleet / SeatLayoutDesigner | Component (Normal) | Fleet manager logged in | Render `SeatLayoutDesignerPage` and select 2+2 layout preset. | Generates 4-column seat grid with central aisle gap visualization. | Grid rendered; seat click toggles availability state. | **Passed** | `REQ-TEST-03` | Nuhadh |
| **`TC-WEB-003`** | Bookings / ManifestMonitor | Component (Normal) | Active service selected | Render `BookingManifestMonitorPage` with passenger list. | Displays real-time manifest, hold countdown monitor, and search bar. | Filter input filters passenger rows instantly. | **Passed** | `REQ-TEST-03` | Mithila |
| **`TC-WEB-004`** | Disruptions / DisruptionHub | Component (Normal) | Transport Manager logged in | Render `DisruptionHubPage` with high-impact rebooking proposal. | Disruption details shown; Manager Approval button enabled. | "Approve Rebooking" triggers action handler. | **Passed** | `REQ-TEST-03` | Dineth |
| **`TC-WEB-005`** | Admin / UserGovernance | Component (Normal) | Admin logged in | Render `AdminUsersPage` with locked user account. | Displays lockout badge and provides "Unlock Account" button. | Account unlocked; table state reflects active status. | **Passed** | `REQ-SEC-01` | Nuhadh |
| **`TC-WEB-006`** | Auth / RBAC Route Guard | Protected Route | Unauthenticated visitor | Attempt navigation to `/admin/users` or `/disruptions/manage`. | Protected route intercepts navigation and redirects to `/login`. | User redirected to `/login` with returnUrl set. | **Passed** | `REQ-SEC-02` | Sethum |

---

## 4. Testing Category 4: Flutter Mobile Client Testing (flutter_test)

| Test Case ID | Component / Feature | Test Type | Preconditions | Inputs & Execution Steps | Expected Result | Actual Result | Status | Traceability | Member |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`TC-MOB-001`** | Journey / SearchScreen | Widget (Normal) | App launched on Explore tab | Select Origin="Colombo", Dest="Kandy", Date="2026-10-15". | Displays timetable card list with departure times and fare badges. | Journey cards displayed with corridor chips. | **Passed** | `REQ-TEST-04` | Sethum |
| **`TC-MOB-002`** | Fleet / SeatPickerBloc | Unit (State Transition) | Service seat layout loaded | Dispatch `SelectSeat("12A")` followed by `HoldSeats()`. | State transitions: `Loaded` $\rightarrow$ `SeatsHolding` $\rightarrow$ `SeatsHeld`. | State emitted with 600s countdown timer. | **Passed** | `REQ-TEST-04` | Nuhadh |
| **`TC-MOB-003`** | Fleet / SeatPickerBloc | Unit (Invalid/Duplicate) | Seat `12A` already selected | Dispatch `SelectSeat("12A")` again. | Duplicate selection ignored; selection count remains 1. | Duplicate rejected; state preserved. | **Passed** | `REQ-TEST-04` | Nuhadh |
| **`TC-MOB-004`** | Fleet / ReviewSubmission | Widget (Validation) | Passenger completed journey | Tap "Submit Review" with 0 stars selected. | Form displays error: "Please select a star rating". | Validation banner displayed; API call blocked. | **Passed** | `REQ-TEST-04` | Nuhadh |
| **`TC-MOB-005`** | Tickets / TicketWallet | Widget (Normal) | Confirmed booking exists | Navigate to Tickets tab. | Renders dynamic QR barcode and brightness booster control. | QR widget rendered with booking reference `WP-XXXXXX`. | **Passed** | `REQ-TEST-04` | Mithila |
| **`TC-MOB-006`** | Disruptions / AlertBanner | Widget (Normal) | Disruption broadcasted | Push alert received for booked service. | Displays persistent alert banner with delay info and reroute choice. | Alert card displayed with action buttons. | **Passed** | `REQ-TEST-04` | Dineth |
| **`TC-MOB-007`** | Conductor / QRScanner | Widget (Normal) | Conductor logged in | Simulate camera scan of valid QR ticket payload. | Displays green validation checkmark and passenger boarding button. | Valid ticket confirmed; boarding status updated. | **Passed** | `REQ-TEST-04` | Dineth |

---

## 5. Testing Category 5: Closed-Loop Cross-Platform Integration & E2E Testing

| Test Case ID | Workflow Step | Test Level | Triggering Actor | Execution Steps & Target Endpoints | Expected System Result | Actual Result | Status | Traceability | Member |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`TC-E2E-001`** | Step 1: Health & Connectivity | E2E Integration | System Client | `GET /health` | Status=200, `database="Connected"`. | HTTP 200, DB healthy. | **Passed** | Baseline | Shared |
| **`TC-E2E-002`** | Step 2: Journey Exploration | E2E Integration | Passenger (Mobile) | `POST /api/v1/journeysearch/search` | Returns matching bus services between Colombo & Kandy. | Search returned valid service payload. | **Passed** | `BR-TRANSFER-001` | Sethum |
| **`TC-E2E-003`** | Step 3: Interactive Seat Hold | E2E Integration | Passenger (Mobile) | `POST /api/v1/seathold` (Seat `12A`) | Returns 201 Created with 10-minute hold lock; seat unavailable to others. | Hold lock created; HTTP 200/201. | **Passed** | `BR-HOLD-001` | Mithila |
| **`TC-E2E-004`** | Step 4: Disruption & AI Proposal | E2E Integration | Dispatcher (Web) | `POST /api/v1/disruption/cases` | LangGraph AI generates rebooking plan; marks status `PendingApproval`. | Case logged; impact evaluated. | **Passed** | `FR-AI-003` | Dineth |
| **`TC-E2E-005`** | Step 5: Manager Approval Gate | E2E Integration | Manager (Web) | `POST /api/v1/approval/decide` | Decision recorded; high-impact rebooking plan approved for execution. | Manager approval registered. | **Passed** | `BR-APPROVAL-001` | Dineth |
| **`TC-E2E-006`** | Step 6: Checkout & QR Issuance | E2E Integration | Passenger (Mobile) | `POST /api/v1/payment/charge` | Commits payment, assigns seat, generates HMAC-SHA256 signed QR pass. | QR generated with tamper-proof HMAC. | **Passed** | `BR-HMAC-001` | Mithila |
| **`TC-E2E-007`** | Step 7: Conductor Boarding Scan | E2E Integration | Conductor (Mobile) | `POST /api/v1/ticket/verify-qr` | Validates HMAC signature; marks ticket status as `Boarded`. | Signature verified; boarding complete. | **Passed** | Closed-Loop | Dineth |

---

## 6. Testing Category 6: Non-Functional Testing (Performance & Security — REQUIRED)

| Test Case ID | Non-Functional Area | Tool / Framework | Test Configuration | Execution Steps & Payload | Expected Result | Actual Measured Result | Status | Member |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`TC-NF-PERF-001`** | Concurrency / Load Testing | k6 & Python Benchmark | 20 concurrent VUs, 50 requests | `GET /health` under concurrent load. | Throughput $\ge 5$ req/s, P95 $< 5000$ms. | **7.76 req/s**, P50=1243ms, P95=4645ms, 0% errors. | **Passed** | Mithila |
| **`TC-NF-PERF-002`** | High-Traffic Search Latency | k6 & Python Benchmark | 20 concurrent VUs, 50 requests | `POST /journeysearch/search` concurrent search queries. | P95 latency $< 1000$ms, 0 server crashes. | **28.19 req/s**, P50=574ms, P95=624ms, 0% 500 errors. | **Passed** | Sethum |
| **`TC-NF-PERF-003`** | Seat Hold Collision Contention | k6 & Python Benchmark | 25 concurrent VUs, 50 requests | Rapid concurrent seat hold attempts on single seat `12A`. | No double bookings; atomic isolation maintained. | **12.35 req/s**, P50=1924ms, atomic barrier verified. | **Passed** | Mithila |
| **`TC-NF-SEC-001`** | Forged JWT Token Rejection | Python Requests & Pytest | Forged HS256 JWT token | `GET /api/v1/user/profile` with tampered role claim (`Admin`). | Rejects with HTTP 401 Unauthorized; invalid signature. | **HTTP 401**; access denied. | **Passed** | Nuhadh |
| **`TC-NF-SEC-002`** | RBAC Privilege Escalation | Python Requests & Pytest | Valid Passenger token | `POST /api/v1/bus` (attempting fleet vehicle creation). | Rejects with HTTP 403 Forbidden; role unauthorized. | **HTTP 403 / 401**; blocked. | **Passed** | Nuhadh |
| **`TC-NF-SEC-003`** | SQL Injection Immunity | Python Requests & Pytest | Parameterized EF Core queries | Search query with `' OR 1=1 --` and `'; DROP TABLE;`. | No SQL syntax errors; no database schema leaks; status $\ne 500$. | **Status 200/404**; 0 crashes; safe parameterized query. | **Passed** | Sethum |
| **`TC-NF-SEC-004`** | HMAC QR Tamper Detection | Python Pytest & HMAC | Altered seat data in QR string | Modify seat from `4B` to `1A` in cryptographic payload. | Signature mismatch detected; rejected immediately. | **HMAC signature mismatch flagged**; forgery prevented. | **Passed** | Mithila |
| **`TC-NF-SEC-005`** | AI Prompt Injection Defusing | Python Pytest & Regex | Adversarial jailbreak string | `sanitize_user_input("IGNORE PREVIOUS INSTRUCTIONS...")`. | Injection markers stripped; user input safely wrapped in tags. | **Injection pattern neutralized**; prompt defused. | **Passed** | Dineth |
| **`TC-NF-SEC-006`** | Safe-Failure Fault Tolerance | Python Pytest & Asyncio | Simulated 3 cascading outages | Agent fails 3 consecutive times during rebooking execution. | Circuit breaker trips; workflow enters `SafeFailure` (FR-AI-004). | **SafeFailure status emitted**; zero unhandled crashes. | **Passed** | Dineth |

---

## 7. Testing Category 7: Agentic AI Testing & Evaluation (Pytest / LangGraph)

| Test Case ID | AI Subsystem Area | Test Type | Preconditions | Inputs & Execution Steps | Expected Result | Actual Result | Status | Traceability | Member |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`TC-AI-001`** | Tool Registry Strict Allow-List | Evaluation (Security) | Tool registry initialized | Query allow-listed tools in AI microservice. | Exactly 10 registered tools; unauthorized tools rejected. | Exactly 10 tools; strict registry enforced. | **Passed** | `BR-AITOOL-001` | Dineth |
| **`TC-AI-002`** | LangGraph State Transition | Workflow (Normal) | Disruption workflow invoked | Pass valid disrupted service state through LangGraph graph. | 5 nodes execute in sequence: Journey $\rightarrow$ Resource $\rightarrow$ Booking $\rightarrow$ Safety $\rightarrow$ End. | All 5 nodes executed successfully. | **Passed** | `REQ-AI-001` | Dineth |
| **`TC-AI-003`** | Driver Rest Hours Guardrail | Guardrail (Labor Safety) | Replacement driver assigned | Replacement driver has 6 hours rest prior to shift. | Deterministic guardrail rejects assignment; requires $>8$ hours. | Guardrail flagged labor violation. | **Passed** | `BR-TIME-001` | Nuhadh |
| **`TC-AI-004`** | Bus Seating Capacity Guardrail | Guardrail (Physical Limits) | Replacement vehicle assigned | Replacement bus capacity (40 seats) $<$ affected passengers (45). | Guardrail rejects vehicle; capacity deficit flagged. | Guardrail rejected allocation. | **Passed** | `BR-FLEET-001` | Nuhadh |
| **`TC-AI-005`** | Manager Approval Gate Override | Guardrail (Governance) | AI classifies disruption as minor | Route cancellation occurs or delay exceeds 15 minutes. | Deterministic guardrail forces classification to `HighImpact`. | Guardrail overrode to HighImpact; Manager Gate engaged. | **Passed** | `BR-APPROVAL-001` | Dineth |
| **`TC-AI-006`** | Safe-Failure Consecutive Trip | Guardrail (Resilience) | Agent encounters network error | Re-attempt execution up to retry threshold ($N=3$). | Workflow marks status as `SafeFailure`; zero silent failures. | SafeFailure triggered on 3rd failure. | **Passed** | `FR-AI-004` | Dineth |

---

## 8. Test Execution Summary Matrix

```
========================================================================================
                      WAYPOINT AUTOMATED TEST EXECUTION SUMMARY
========================================================================================
Test Suite / Subsystem                 Tool / Framework        Total Tests   Passed   Failed
----------------------------------------------------------------------------------------
1. Authoritative Backend (.NET 8)      xUnit / Moq / Fluent           82        82        0
2. Web Frontend Single-Page App        Vitest / Testing Library       36        36        0
3. Flutter Mobile Application          flutter_test / mocktail        65        65        0
4. Agentic AI Subsystem (Python)       Pytest / LangGraph            246       246        0
5. Closed-Loop E2E Workflow            Python / Requests / Pytest      7         7        0
6. Cross-Service API & DB Integration  Python / Requests / Pytest      6         6        0
7. Automated Security Audit & Pen-Test Python / Pytest / Cryptography  6         6        0
----------------------------------------------------------------------------------------
TOTAL AUTOMATED TEST VERIFICATIONS                                   448       448        0  (100% PASS)
========================================================================================
```
