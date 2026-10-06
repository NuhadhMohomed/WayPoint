# WayPoint Comprehensive Test Suite

This directory contains cross-cutting integration, end-to-end (E2E), and performance test suites for the WayPoint transit platform.

## Test Directory Structure

- **`tests/e2e/`**: System-level cross-component end-to-end tests validating complete user journeys from booking to boarding.
  - `test_closed_loop_workflow.py`: Closed-loop cross-platform scenario executing the complete 7-step journey (Figure 2 / `REQ-TEST-05`).
  ```bash
  pytest tests/e2e/test_closed_loop_workflow.py -v
  ```
- **`tests/integration/`**: Cross-service integration tests (ASP.NET Core Web API ↔ PostgreSQL ↔ Python AI Subsystem).
  - `test_cross_service_api.py`: Validates API contracts, RBAC authorization, and database integrity constraints.
  ```bash
  pytest tests/integration/test_cross_service_api.py -v
  ```
- **`tests/performance/`**: Load, stress, and concurrency test suites.
  - `seat_hold_concurrency_k6.js`: k6 load test script simulating 50 concurrent virtual users under peak booking traffic.
  - `run_concurrency_benchmarks.py`: Automated Python concurrency benchmark runner measuring throughput, latency distributions (P50, P90, P95, P99), and double-booking collision prevention.
  ```bash
  python tests/performance/run_concurrency_benchmarks.py
  ```
- **`tests/security/`**: Automated security audit and non-functional resilience test suite.
  - `test_security_audit.py`: Validates OWASP API Top 10 defenses, forged JWT rejection, HMAC-SHA256 QR ticket tampering, SQL injection immunity, prompt injection defusing, and safe-failure circuit breaking.
  ```bash
  pytest tests/security/test_security_audit.py -v
  ```

## Component-Specific Test Suites

Component-level test suites reside inside their respective application directories:

- **Backend (.NET 8 xUnit)**: `backend/WayPoint.Tests/` (82 unit, domain, and concurrency integration tests).
  ```bash
  dotnet test backend/WayPoint.sln --configuration Release
  ```
- **Mobile (Flutter Test / Dart 3)**: `mobile/test/` (65 unit, BLoC state transition, and widget tests).
  ```bash
  cd mobile && flutter test
  ```
- **Web Frontend (Vitest & RTL)**: `web/src/**/__tests__/` (36 component, form validation, and RBAC tests).
  ```bash
  cd web && npm test
  ```
- **AI Multi-Agent Subsystem (Pytest)**: `ai/tests/` (246 unit, LangGraph workflow, guardrail, and safe-failure tests).
  ```bash
  cd ai && pytest tests/ -v
  ```

## SE3110 Quality Assurance Documentation Suite

For formal academic and quality deliverables prepared for SE3110, refer to:
- [SE3110 Software Testing Report](../docs/testing/SE3110_Software_Testing_Report.md) (Test Plan, Scope, Group & Individual Contributions, Quality Sign-Off)
- [SE3110 Test Case Document](../docs/testing/SE3110_Test_Case_Document.md) (Exhaustive structured test case specifications across all 7 areas)
- [SE3110 Defect / Bug Report](../docs/testing/SE3110_Defect_Bug_Report.md) (8 discovered defects with root causes, code fixes, and retest evidence)
- [SE3110 Tool-Generated Evidence](../docs/testing/SE3110_Tool_Generated_Evidence.md) (Terminal outputs, coverage reports, performance data, and logs)
- [SE3110 Viva Preparation Guide](../docs/testing/SE3110_Viva_Preparation_Guide.md) (Individual viva demonstration scripts and technical defense for all 4 students)

