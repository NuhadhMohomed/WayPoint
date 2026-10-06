# Comprehensive Test Strategy & Quality Assurance Guide

**Project**: WayPoint — Integrated Full-Stack & Agentic AI Transit Platform  
**Course**: SE3090 — Integrated Full-Stack and Agentic AI Application Development (Assignment 1)  
**Standard**: SE3090 Testing & Verification Requirements (`REQ-TEST-01` to `REQ-TEST-08`)  

---

## 1. Executive Summary & Testing Pyramid

WayPoint employs a comprehensive, multi-layered testing strategy spanning all tiers of the integrated full-stack architecture. In accordance with the SE3090 Assignment 1 specification, the quality assurance framework enforces strict coverage across backend business logic, database transactions, web management interfaces, mobile passenger workflows, autonomous AI multi-agent pipelines, and end-to-end cross-platform integration.

```
                    ┌─────────────────────────┐
                    │  Closed-Loop Cross-     │
                    │  Platform E2E Tests     │
                    │  (REQ-TEST-05: Fig. 2)  │
                    ├─────────────────────────┴─┐
                    │  Cross-Service Integration │
                    │  (ASP.NET ↔ AI ↔ DB)      │
               ┌────┴───────────────────────────┴────┐
               │  Agentic AI Evaluation Benchmarks   │
               │  (LangGraph, Guardrails, Pytest)     │
          ┌────┴─────────────────────────────────────┴────┐
          │  Frontend UI Tests: React (Vitest) & Flutter   │
          │  (React Testing Library + Flutter Test / BLoC) │
     ┌────┴───────────────────────────────────────────────┴────┐
     │  Authoritative Backend Unit & Concurrency Test Suite   │
     │  (77+ .NET 8 xUnit Tests, EF Core Concurrency, Rules)   │
     └─────────────────────────────────────────────────────────┘
```

---

## 2. Requirement Traceability Matrix (`REQ-TEST-01` – `REQ-TEST-08`)

| Requirement ID | Test Level / Focus | Target Subsystem | Implementation File / Suite | Verification Command |
| :--- | :--- | :--- | :--- | :--- |
| **`REQ-TEST-01`** | Backend Unit Testing | ASP.NET Core API & Domain | `backend/WayPoint.Tests/` | `dotnet test backend/WayPoint.sln` |
| **`REQ-TEST-02`** | Database Concurrency | PostgreSQL & EF Core | `backend/WayPoint.Tests/Domain/SeatHoldTests.cs` | `dotnet test --filter "Category=Concurrency"` |
| **`REQ-TEST-03`** | Web Component Testing | React 18 Admin & Operator UI | `web/src/**/__tests__/*.test.jsx` | `cd web && npm test` |
| **`REQ-TEST-04`** | Mobile Widget Testing | Flutter Mobile App (Dart 3) | `mobile/test/features/**/*_test.dart` | `cd mobile && flutter test` |
| **`REQ-TEST-05`** | Closed-Loop E2E Workflow | Multi-Tier Integrated Stack | `tests/e2e/` & Integration Scenarios | End-to-End Cross-Platform Script |
| **`REQ-TEST-06`** | AI Multi-Agent Evaluation | Python LangGraph Subsystem | `ai/tests/` | `cd ai && pytest tests/ -v` |
| **`REQ-TEST-07`** | Security & Auth Testing | JWT & Role Authorization | `backend/WayPoint.Tests/Controllers/` | `dotnet test --filter "Category=Security"` |
| **`REQ-TEST-08`** | CI/CD Pipeline Automation | GitHub Actions Workflows | `.github/workflows/` | GitHub Actions Execution |

---

## 3. Tier 1: Authoritative Backend Testing (.NET 8 / xUnit)

### 3.1 Scope & Architecture
The backend test suite ([backend/WayPoint.Tests/](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/backend/WayPoint.Tests/)) validates core business logic, domain invariants, CQRS handlers, and controller endpoints using **xUnit**, **FluentAssertions**, and **Moq**.

- **Total Test Cases**: 77+ automated tests with 100% pass rate.
- **Coverage Areas**:
  - `WayPoint.Domain`: Invariant protection, entity state transitions, seat status calculations.
  - `WayPoint.Application`: Use case handlers, input validation, DTO mapping.
  - `WayPoint.Infrastructure`: EF Core configuration, PostgreSQL type conversions.
  - `WayPoint.API`: Controller endpoints, JWT authentication middleware, custom exception filters.

### 3.2 Key Tested Business Invariants
1. **10-Minute Seat Hold Expiration (`BR-HOLD-001`)**:
   - `SeatHold.Create()` initializes active hold expiring at $t_0 + 10 \text{ minutes}$.
   - Expired holds automatically release seats back to the available inventory pool.
2. **Connecting Transfer Buffer (`BR-TRANSFER-001`)**:
   - Minimum 20-minute gap required between connecting services; invalid itineraries rejected.
3. **Tiered Refund Calculation (`BR-REFUND-001`)**:
   - Cancellation $>24$ hrs before departure: 90% refund (10% platform fee retained).
   - Cancellation $12\text{--}24$ hrs: 50% refund.
   - Cancellation $<12$ hrs: 0% refund.
4. **Manager Approval Gate (`BR-APPROVAL-001`)**:
   - Timetable changes $>15$ minutes or route cancellations require explicit Transport Manager sign-off.

### 3.3 Execution Command
```bash
# Run all backend tests with Release configuration
dotnet test backend/WayPoint.sln --configuration Release --verbosity normal
```

---

## 4. Tier 2: Database & Concurrency Testing (PostgreSQL 18 / EF Core)

### 4.1 Concurrency Protection (`REQ-TEST-02`)
High-demand intercity routes experience race conditions when multiple passengers select the same seat simultaneously.

- **Isolation & Atomic Transactions**: Seat hold creation executes within an isolated database transaction.
- **Optimistic Concurrency**: The `Seats` table utilizes concurrency tokens (`xmin` / row versioning). If two transactions attempt to hold seat `12A` simultaneously, the first transaction commits and the second receives a `DbUpdateConcurrencyException`, mapping to HTTP `409 Conflict`.
- **Double Booking Prevention**: Validated by parallel task execution tests in `backend/WayPoint.Tests/Domain/SeatHoldTests.cs`.

---

## 5. Tier 3: React Web Frontend Testing (Vitest & Testing Library)

### 5.1 Test Framework
The React web application ([web/](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/)) uses **Vitest** and **React Testing Library** with simulated JSDOM environments.

### 5.2 Functional Component Coverage (`REQ-TEST-03`)

| Student | Component Area | Test File | Key Test Cases |
| :--- | :--- | :--- | :--- |
| **Sethum** (Student 1) | Routes & Catalogue | [web/src/features/journey/\_\_tests\_\_/RouteManager.test.jsx](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/features/journey/__tests__/RouteManager.test.jsx) | Route list rendering, stop creation modal, timetable filter |
| **Nuhadh** (Student 2) | Fleet & Seat Design | [web/src/features/fleet/\_\_tests\_\_/SeatLayoutDesigner.test.jsx](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx) | 2+2 vs 1+2 layout generation, driver rostering validation |
| **Mithila** (Student 3) | Bookings & Operations | [web/src/features/bookings/\_\_tests\_\_/BookingManifest.test.jsx](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/features/bookings/__tests__/BookingManifest.test.jsx) | Real-time passenger manifest, hold countdown monitor, search |
| **Dineth** (Student 4) | Disruptions & Approvals | [web/src/features/disruptions/\_\_tests\_\_/DisruptionHub.test.jsx](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/features/disruptions/__tests__/DisruptionHub.test.jsx) | Incident intake form, Transport Manager approval button gate |
| **Shared** | Admin & Security | [web/src/features/admin/\_\_tests\_\_/AdminUsersPage.test.jsx](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/web/src/features/admin/__tests__/AdminUsersPage.test.jsx) | User role management, RBAC route guard protection |

### 5.3 Execution Command
```bash
cd web
npm test
```

---

## 6. Tier 4: Flutter Mobile Client Testing (flutter_test)

### 6.1 Test Framework
The Flutter mobile application ([mobile/](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/)) uses **flutter_test** and **bloc_test** for unit, widget, and state management testing.

### 6.2 Screen & Feature Coverage (`REQ-TEST-04`)

| Student | Screen / Feature | Test File | Key Test Cases |
| :--- | :--- | :--- | :--- |
| **Sethum** (Student 1) | Journey Search | [mobile/test/features/journey/journey_search_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/journey/journey_search_test.dart) | Origin/destination picker, route card rendering, filter tags |
| **Nuhadh** (Student 2) | Interactive Seat Picker | [mobile/test/features/seat_picker_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/seat_picker_test.dart) | Available/held/booked color coding, single-seat selection |
| **Nuhadh** (Student 2) | Fleet Reviews | [mobile/test/features/settings_reviews_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/settings_reviews_test.dart) | Star rating input, passenger comment submission |
| **Mithila** (Student 3) | Payment & Checkout | [mobile/test/features/payment_checkout_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/payment_checkout_test.dart) | Fare summary breakdown, payment method selection, hold timer |
| **Mithila** (Student 3) | Ticket Wallet | [mobile/test/features/ticket_wallet_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/ticket_wallet_test.dart) | Cryptographic QR rendering, active/past ticket tabs |
| **Dineth** (Student 4) | Disruption Alerts | [mobile/test/features/disruption_alert_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/disruption_alert_test.dart) | Push alert notification display, reroute acceptance button |
| **Dineth** (Student 4) | Conductor Tools | [mobile/test/features/conductor_tools_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/conductor_tools_test.dart) | QR camera scanner simulation, passenger manifest checklist |
| **Shared** | Navigation Shells | [mobile/test/features/navigation_shells_test.dart](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/navigation_shells_test.dart) | Bottom navigation bar switching between Search, Tickets, Alerts |

### 6.3 Execution Command
```bash
cd mobile
flutter test
```

---

## 7. Tier 5: Agentic AI Subsystem Evaluation (Pytest / LangGraph)

### 7.1 Scope & Architecture (`REQ-TEST-06`)
The Python AI subsystem ([ai/](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/ai/)) is tested with **pytest** and **pytest-asyncio**, verifying:
- LangGraph 5-node StateGraph execution flow.
- Strict 10 allow-listed tools registry enforcement (`BR-AITOOL-001`).
- Deterministic guardrails and output validators (`FR-AI-003`).
- Safe-failure fallback mechanisms (`FR-AI-004`).
- Telemetry recording for PostgreSQL persistence ([ADR-004](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-004-ai-workflow-persistence.md)).

### 7.2 Guardrail & Deterministic Rule Verifications
1. **`validate_bus_capacity`**: Verifies that replacement vehicle passenger allocation never exceeds physical bus seating capacity.
2. **`validate_driver_rest`**: Enforces Sri Lankan labor safety rules ($>8\text{ hours}$ mandatory rest between intercity shifts).
3. **`validate_fare_difference_arithmetic`**: Mathematically checks $\text{fare\_difference} = \text{replacement\_fare} - \text{original\_fare}$.
4. **`validate_cancellation_refund_schedule`**: Validates 90%/50%/0% tier assignments.
5. **`validate_high_impact_approval_gate`**: Ensures timetable shifts $>15$ min require Manager Approval.

### 7.3 Execution Command
```bash
cd ai
pytest tests/ -v
```

---

## 8. Tier 6: Closed-Loop Cross-Platform E2E Test Workflow (`REQ-TEST-05`)

As mandated by Figure 2 of the SE3090 Assignment Specification, the complete cross-platform workflow operates across all technical tiers in a closed loop:

```
[Flutter Mobile]           [ASP.NET Core API]          [React Web]             [LangGraph AI]
      │                            │                        │                        │
  1.  │── Search Journey (Kandy) ─>│                        │                        │
      │<─ Return Timetable ────────│                        │                        │
  2.  │── Hold Seat 12A (10m) ────>│ (BR-HOLD-001)          │                        │
      │<─ Hold Confirmed (201) ────│                        │                        │
      │                            │                        │                        │
  3.  │                            │<─ Report Disruption ───│ (Operator Intake)      │
      │                            │── Trigger Mitigation ──────────────────────────>│
      │                            │<── Multi-Agent Plan (Rebook + Resource) ────────│
  4.  │                            │<─ Manager Approve ─────│ (BR-APPROVAL-001)      │
      │                            │                        │                        │
  5.  │── Complete Checkout ──────>│ (Atomic Transaction)   │                        │
      │<─ Cryptographic QR Ticket ─│                        │                        │
      │                            │                        │                        │
  6.  │── Conductor Scans QR ─────>│ (Boarded Status)       │                        │
      │<─ Ticket Validated (200) ──│                        │                        │
```

### Verification Criteria
1. Seat 12A status transitions: `Available` $\rightarrow$ `Held` (countdown active) $\rightarrow$ `Booked` (after payment).
2. Concurrency token prevents simultaneous reservation of Seat 12A by any other user.
3. Disruption mitigation proposal generated by AI remains in `PendingApproval` until Transport Manager clicks "Approve".
4. QR ticket token carries valid HMAC signature; conductor scanner confirms passenger as `Boarded`.

---

## 9. Tier 7: Security & Authorization Testing (`REQ-TEST-07`)

The test suite validates role-based access control (RBAC) across all protected endpoints:

| Role | Permitted Actions | Forbidden Actions (HTTP 403) |
| :--- | :--- | :--- |
| **Passenger** | Search routes, hold seats, checkout, view own tickets | Disruption intake, fleet creation, manager approval |
| **Operator** | View schedules, assign buses, report disruptions | High-impact disruption approval, user role management |
| **TransportManager** | Approve operational changes, view AI audit logs, override holds | Passenger payment processing |
| **Conductor** | Scan QR tickets, view passenger manifests | Route creation, fleet modification |
| **Admin** | Full system access, user role assignment, audit log review | None |

---

## 10. Summary Test Commands Reference

For grading and CI verification, execute these commands from the repository root:

```bash
# 1. Authoritative Backend (.NET 8)
dotnet test backend/WayPoint.sln --configuration Release

# 2. React Web Frontend (Vitest)
cd web && npm test

# 3. Flutter Mobile Client (Dart 3)
cd mobile && flutter test

# 4. Agentic AI Subsystem (Python 3.11)
cd ai && pytest tests/ -v
```
