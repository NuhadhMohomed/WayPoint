# WayPoint Test Suite

> **Status: Active Development (WIP)**  
> Automated unit, component, and integration tests are being expanded continuously as features stabilize across backend, web, mobile, and AI services.

This directory contains cross-cutting end-to-end, integration, performance, and security test suites, complementing each module's local test suite.

---

## Cross-Cutting Suites (`tests/`)

| Directory | Files | Purpose |
| :--- | :--- | :--- |
| `tests/e2e/` | `test_closed_loop_workflow.py` | Closed-loop cross-platform scenario (health, search, seat hold, disruption, approval, payment, boarding) |
| `tests/integration/` | `test_cross_service_api.py` | Cross-service API contract, RBAC, and DB constraint checks |
| `tests/security/` | `test_security_audit.py` | JWT validation, RBAC boundaries, HMAC ticket tampering, SQL/prompt injection, and safe-failure checks |
| `tests/performance/` | `run_concurrency_benchmarks.py`, `seat_hold_concurrency_k6.js`, `performance_benchmark_results.json` | Concurrency benchmarking and k6 load test script for seat holds |

### Running Cross-Cutting Suites
Note: `e2e`, `integration`, and `security` HTTP tests target `WAYPOINT_API_URL` (point this to your local API, e.g. `http://localhost:5010`, when developing locally):

```bash
# Example targeting local backend
WAYPOINT_API_URL=http://localhost:5010 pytest tests/e2e/test_closed_loop_workflow.py -v
WAYPOINT_API_URL=http://localhost:5010 pytest tests/integration/test_cross_service_api.py -v
WAYPOINT_API_URL=http://localhost:5010 pytest tests/security/test_security_audit.py -v

# Performance benchmark runner
python tests/performance/run_concurrency_benchmarks.py
```

---

## Module-Level Test Suites

- **Backend (`backend/WayPoint.Tests/`)**:
  ```bash
  dotnet test backend/WayPoint.sln --configuration Release
  ```
- **Web (`web/src/**/__tests__/`)**:
  ```bash
  cd web && npm test
  ```
- **Mobile (`mobile/test/`)**:
  ```bash
  cd mobile && flutter test
  ```
- **AI Subsystem (`ai/tests/`)**:
  ```bash
  cd ai && pytest tests/ -v
  ```

A unified monorepo runner script is also available in `tools/run_all_tests.py` (WIP; requires all toolchains and target services configured).

---

## QA Documentation (`docs/testing/`)

Additional testing strategy, bug logs, and academic QA reports are maintained in [docs/testing/](../docs/testing/).
