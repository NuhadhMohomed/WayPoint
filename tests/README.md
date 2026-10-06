# WayPoint Comprehensive Test Suite

This directory contains cross-cutting integration, end-to-end (E2E), and performance test suites for the WayPoint transit platform.

## Test Directory Structure

- **`tests/e2e/`**: System-level cross-component end-to-end tests validating complete user journeys from booking to boarding.
- **`tests/integration/`**: Cross-service integration tests (ASP.NET Core Web API ↔ PostgreSQL ↔ Python AI Subsystem).
- **`tests/performance/`**: Load, stress, and concurrency test suites (e.g., k6 scripts for 10-minute hold concurrency under high traffic).

## Component-Specific Test Suites

Component-level test suites reside inside their respective application directories:

- **Backend (.NET xUnit)**: `backend/WayPoint.Tests/` (71+ unit and integration tests covering domain entities, EF Core concurrency, business rules, and security).
  ```bash
  cd backend
  dotnet test
  ```
- **Mobile (Flutter Test)**: `mobile/test/` (Unit and widget tests covering passenger booking, disruption alerts, seat picker, and reviews).
  ```bash
  cd mobile
  flutter test
  ```
- **Web Frontend (Vitest & RTL)**: `web/src/**/__tests__/` (Component and integration tests for route manager, seat layout designer, booking manifest, and disruption workbench).
  ```bash
  cd web
  npm test
  ```
- **AI Multi-Agent Subsystem (Pytest)**: `ai/tests/` (Unit tests for LangGraph workflows, deterministic guardrails, and tool execution).
  ```bash
  cd ai
  pytest tests/ -v
  ```

For the complete testing pyramid, closed-loop E2E workflow specifications, and requirement traceability (`REQ-TEST-01` to `REQ-TEST-08`), refer to [docs/testing/test-strategy.md](../docs/testing/test-strategy.md).
