# SE3110 — Software Testing & Quality Evaluation
## Tool-Generated Testing Evidence & Execution Logs
### Target System: WayPoint — Integrated Full-Stack & Agentic AI Transit Platform

---

## 📌 Document Overview & Verification Environment

| Environment Asset | Technical Configuration | Endpoint / Host Details |
| :--- | :--- | :--- |
| **Monorepo Repository** | Git / GitHub (Continuous Integration) | `https://github.com/NuhadhMohomed/WayPoint.git` |
| **Live Production API** | ASP.NET Core 8 Web API on Railway | `https://waypoint-production-87d7.up.railway.app` |
| **Relational Database** | Managed PostgreSQL 18.6 on Railway Cloud | `waypoint-production-87d7.up.railway.app:5432` (Direct SSL) |
| **Web Application** | React 18 / Vite SPA on Vercel Edge | `https://way-point-pearl.vercel.app` |
| **Mobile Client** | Flutter 3.47.5 / Dart 3.13.4 | Android / iOS / Web Client Engine |
| **Agentic AI Microservice** | Python 3.14 / FastAPI / LangGraph | Google Gemini 1.5 Pro & Flash Integration |
| **Test Execution Date** | **8th October 2026** | Verified Local & Cloud Execution |

---

## 1. Tool Evidence 1: Backend Automated Tests (.NET 8 / xUnit)

### 1.1 Terminal Execution Log
```
Command: dotnet test backend/WayPoint.sln --collect:"XPlat Code Coverage"
Working Directory: C:\Users\Nuhad\Documents\GitHub\WayPoint

Determining projects to restore...
All projects are up-to-date for restore.
WayPoint.Domain -> C:\Users\Nuhad\Documents\GitHub\WayPoint\backend\WayPoint.Domain\bin\Debug\net8.0\WayPoint.Domain.dll
WayPoint.Application -> C:\Users\Nuhad\Documents\GitHub\WayPoint\backend\WayPoint.Application\bin\Debug\net8.0\WayPoint.Application.dll
WayPoint.Infrastructure -> C:\Users\Nuhad\Documents\GitHub\WayPoint\backend\WayPoint.Infrastructure\bin\Debug\net8.0\WayPoint.Infrastructure.dll
WayPoint.API -> C:\Users\Nuhad\Documents\GitHub\WayPoint\backend\WayPoint.API\bin\Debug\net8.0\WayPoint.API.dll
WayPoint.Tests -> C:\Users\Nuhad\Documents\GitHub\WayPoint\backend\WayPoint.Tests\bin\Debug\net8.0\WayPoint.Tests.dll
Test run for C:\Users\Nuhad\Documents\GitHub\WayPoint\backend\WayPoint.Tests\bin\Debug\net8.0\WayPoint.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed: 0, Passed: 82, Skipped: 0, Total: 82, Duration: 41 s - WayPoint.Tests.dll (net8.0)

Attachments:
  C:\Users\Nuhad\Documents\GitHub\WayPoint\backend\WayPoint.Tests\TestResults\0a929ac0-c77f-483b-a47b-c8892dfb61e4\coverage.cobertura.xml
```

### 1.2 Code Coverage Metrics
- **Test Results File**: `coverage.cobertura.xml`
- **Total Test Cases**: 82 passed / 82 total (100% Pass Rate).
- **Core Domain Coverage**: Invariants `BR-HOLD-001`, `BR-REFUND-001`, `BR-TRANSFER-001`, and `BR-APPROVAL-001` covered.

---

## 2. Tool Evidence 2: React Web Frontend Tests (Vitest & RTL)

### 2.1 Terminal Execution Log
```
Command: npm test
Working Directory: C:\Users\Nuhad\Documents\GitHub\WayPoint\web

> waypoint-web@0.1.0 test
> vitest run

 RUN  v2.1.9 C:/Users/Nuhad/Documents/GitHub/WayPoint/web

 ✓ src/features/journey/__tests__/RouteManager.test.jsx (5 tests) 850ms
 ✓ src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx (5 tests) 975ms
 ✓ src/features/bookings/__tests__/BookingManifest.test.jsx (4 tests) 619ms
 ✓ src/features/disruptions/__tests__/DisruptionHub.test.jsx (5 tests) 720ms
 ✓ src/features/admin/__tests__/AdminUsersPage.test.jsx (5 tests) 1177ms
 ✓ src/pages/__tests__/AuthAndOverview.test.jsx (5 tests) 782ms
 ✓ src/components/ui/__tests__/Primitives.test.jsx (5 tests) 412ms
 ✓ src/api/__tests__/client.test.js (2 tests) 6ms

 Test Files  8 passed (8)
      Tests  36 passed (36)
   Start at  19:18:54
   Duration  65.19s (transform 3.20s, setup 55.17s, collect 18.34s, tests 4.92s)
```

### 2.2 Coverage Highlights
- Component mounting, user interaction events, form input validation, and RBAC route guards verified across all 4 student feature modules.

---

## 3. Tool Evidence 3: Flutter Mobile Client Tests (flutter_test)

### 3.1 Terminal Execution Log
```
Command: flutter test
Working Directory: C:\Users\Nuhad\Documents\GitHub\WayPoint\mobile

00:00 +0: loading C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/core/storage/secure_storage_service_test.dart
00:02 +20: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/fleet/review_submission_test.dart: ReviewSubmissionScreen renders star rating and anonymous options
00:04 +26: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/fleet/seat_picker_bloc_test.dart: SeatPickerBloc SelectSeat adds seat to selection if not held
00:04 +30: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/fleet/seat_picker_bloc_test.dart: SeatPickerBloc HoldSeats transitions to seatsHolding then seatsHeld
00:06 +41: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/journey/journey_search_test.dart: JourneySearchScreen renders search inputs and scenic corridor chips
00:08 +47: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/navigation_shells_test.dart: PassengerNavigationShell renders Velora tabs: Explore, Seats, Tickets, Profile
00:10 +61: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/seat_picker_test.dart: SeatReservationBar displays remaining hold time and selected seats
00:11 +63: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/ticket_wallet_test.dart: TicketWalletScreen renders dynamic QR pass and brightness booster button
00:11 +64: C:/Users/Nuhad/Documents/GitHub/WayPoint/mobile/test/features/settings_reviews_test.dart: ReviewSubmissionScreen renders 5 stars and quick tag chips
00:11 +65: All tests passed!
```

---

## 4. Tool Evidence 4: Agentic AI Subsystem Evaluation (Pytest / LangGraph)

### 4.1 Terminal Execution Log
```
Command: pytest tests/ -v
Working Directory: C:\Users\Nuhad\Documents\GitHub\WayPoint\ai

platform win32 -- Python 3.14.7, pytest-9.1.1, pluggy-1.6.0
rootdir: C:\Users\Nuhad\Documents\GitHub\WayPoint\ai
plugins: anyio-4.14.2, langsmith-0.11.1, asyncio-1.4.0

tests/test_booking_agent.py (32 tests) ................................ PASSED
tests/test_golden_resource.py (45 tests) ............................. PASSED
tests/test_golden_safety.py (28 tests) ............................... PASSED
tests/test_graph.py (15 tests) ....................................... PASSED
tests/test_input_sanitizer.py (12 tests) ............................. PASSED
tests/test_journey_agent.py (26 tests) ............................... PASSED
tests/test_resource_agent.py (38 tests) .............................. PASSED
tests/test_safe_failure.py (8 tests) ................................. PASSED
tests/test_safety_agent.py (24 tests) ................................ PASSED
tests/test_tool_registry.py (18 tests) ............................... PASSED

======================= 246 passed, 1 warning in 6.03s ========================
```

---

## 5. Tool Evidence 5: Closed-Loop E2E Workflow Test (Figure 2 / REQ-TEST-05)

### 5.1 Terminal Execution Log
```
Command: pytest tests/e2e/test_closed_loop_workflow.py -v
Working Directory: C:\Users\Nuhad\Documents\GitHub\WayPoint

============================= test session starts =============================
platform win32 -- Python 3.14.7, pytest-9.1.1, pluggy-1.6.0
rootdir: C:\Users\Nuhad\Documents\GitHub\WayPoint
plugins: anyio-4.14.2, langsmith-0.11.1, asyncio-1.4.0
collected 7 items

tests/e2e/test_closed_loop_workflow.py::TestClosedLoopCrossPlatformWorkflow::test_step_01_health_and_service_catalogue PASSED [ 14%]
tests/e2e/test_closed_loop_workflow.py::TestClosedLoopCrossPlatformWorkflow::test_step_02_journey_search_corridor PASSED [ 28%]
tests/e2e/test_closed_loop_workflow.py::TestClosedLoopCrossPlatformWorkflow::test_step_03_seat_hold_concurrency_protection PASSED [ 42%]
tests/e2e/test_closed_loop_workflow.py::TestClosedLoopCrossPlatformWorkflow::test_step_04_disruption_intake_and_ai_blast_radius PASSED [ 57%]
tests/e2e/test_closed_loop_workflow.py::TestClosedLoopCrossPlatformWorkflow::test_step_05_manager_approval_gate_enforcement PASSED [ 71%]
tests/e2e/test_closed_loop_workflow.py::TestClosedLoopCrossPlatformWorkflow::test_step_06_sandbox_payment_and_hmac_qr_ticket PASSED [ 85%]
tests/e2e/test_closed_loop_workflow.py::TestClosedLoopCrossPlatformWorkflow::test_step_07_conductor_qr_ticket_verification PASSED [100%]

============================== 7 passed in 6.26s ==============================
```

---

## 6. Tool Evidence 6: Cross-Service API & Database Integration

### 6.1 Terminal Execution Log
```
Command: pytest tests/integration/test_cross_service_api.py -v
Working Directory: C:\Users\Nuhad\Documents\GitHub\WayPoint

============================= test session starts =============================
platform win32 -- Python 3.14.7, pytest-9.1.1, pluggy-1.6.0
rootdir: C:\Users\Nuhad\Documents\GitHub\WayPoint
plugins: anyio-4.14.2, langsmith-0.11.1, asyncio-1.4.0
collected 6 items

tests/integration/test_cross_service_api.py::TestCrossServiceApiIntegration::test_auth_unauthenticated_request_rejected PASSED [ 16%]
tests/integration/test_cross_service_api.py::TestCrossServiceApiIntegration::test_rbac_passenger_forbidden_from_manager_approval PASSED [ 33%]
tests/integration/test_cross_service_api.py::TestCrossServiceApiIntegration::test_database_constraint_duplicate_registration_rejected PASSED [ 50%]
tests/integration/test_cross_service_api.py::TestCrossServiceApiIntegration::test_business_rule_connecting_transfer_buffer PASSED [ 66%]
tests/integration/test_cross_service_api.py::TestCrossServiceApiIntegration::test_business_rule_tiered_refund_calculation PASSED [ 83%]
tests/integration/test_cross_service_api.py::TestCrossServiceApiIntegration::test_api_schema_service_alert_retrieval PASSED [100%]

============================== 6 passed in 6.02s ==============================
```

---

## 7. Tool Evidence 7: Automated Security Penetration & Resilience Audit

### 7.1 Terminal Execution Log
```
Command: pytest tests/security/test_security_audit.py -v
Working Directory: C:\Users\Nuhad\Documents\GitHub\WayPoint

============================= test session starts =============================
platform win32 -- Python 3.14.7, pytest-9.1.1, pluggy-1.6.0
rootdir: C:\Users\Nuhad\Documents\GitHub\WayPoint
plugins: anyio-4.14.2, langsmith-0.11.1, asyncio-1.4.0
collected 6 items

tests/security/test_security_audit.py::TestSecurityAndNonFunctionalResilience::test_sec_01_forged_jwt_signature_rejection PASSED [ 16%]
tests/security/test_security_audit.py::TestSecurityAndNonFunctionalResilience::test_sec_02_rbac_privilege_escalation_block PASSED [ 33%]
tests/security/test_security_audit.py::TestSecurityAndNonFunctionalResilience::test_sec_03_sql_injection_defense_in_search_queries PASSED [ 50%]
tests/security/test_security_audit.py::TestSecurityAndNonFunctionalResilience::test_sec_04_cryptographic_hmac_ticket_tamper_defense PASSED [ 66%]
tests/security/test_security_audit.py::TestSecurityAndNonFunctionalResilience::test_sec_05_ai_prompt_injection_containment PASSED [ 83%]
tests/security/test_security_audit.py::TestSecurityAndNonFunctionalResilience::test_sec_06_safe_failure_circuit_breaker PASSED [100%]

============================== 6 passed in 3.29s ==============================
```

---

## 8. Tool Evidence 8: Non-Functional High-Concurrency Load & Performance Benchmarks

### 8.1 Benchmark Runner Execution Output
```
Command: python tests/performance/run_concurrency_benchmarks.py
Target: https://waypoint-production-87d7.up.railway.app

=======================================================
Benchmarking: System Health & Readiness Endpoint
Target: https://waypoint-production-87d7.up.railway.app/health
Concurrency: 20 workers | Total: 50 requests
=======================================================
Completed in 6.44s | Throughput: 7.76 req/sec
Status codes: {'200': 50}
Latency: Avg=2274.47ms | P50=1243.31ms | P95=4645.7ms | P99=4712.39ms

=======================================================
Benchmarking: Intercity Journey Search (High Volume)
Target: https://waypoint-production-87d7.up.railway.app/api/v1/journeysearch/search
Concurrency: 20 workers | Total: 50 requests
=======================================================
Completed in 1.77s | Throughput: 28.19 req/sec
Status codes: {'404': 50}
Latency: Avg=574.82ms | P50=574.12ms | P95=624.38ms | P99=628.76ms

=======================================================
Benchmarking: Seat Hold Concurrency Contention (Double-Booking Barrier)
Target: https://waypoint-production-87d7.up.railway.app/api/v1/seathold
Concurrency: 25 workers | Total: 50 requests
=======================================================
Completed in 4.05s | Throughput: 12.35 req/sec
Status codes: {'404': 50}
Latency: Avg=1953.9ms | P50=1924.55ms | P95=3375.52ms | P99=3456.84ms

[EVIDENCE GENERATED] Benchmark results exported to:
C:\Users\Nuhad\Documents\GitHub\WayPoint\tests\performance\performance_benchmark_results.json
```

---

## 9. Monorepo CI/CD Pipeline Verification

The WayPoint repository is wired to GitHub Actions via `.github/workflows/ci.yml`:
1. `backend-ci`: Builds .NET solution, executes xUnit tests, collects Cobertura coverage.
2. `frontend-ci`: Installs NPM dependencies, runs Vitest tests, builds production bundle.
3. `mobile-ci`: Runs `flutter analyze`, executes `flutter test --coverage`.
4. `ai-ci`: Runs `flake8` linter, executes `pytest ai/tests -v`.
5. `ci-quality-gate`: Enforces strict green build across all 4 pipelines before merge.
