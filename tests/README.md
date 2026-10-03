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
- **Web Frontend (Playwright E2E)**: `web/e2e/` (Browser-based automated tests for operator dashboard, dispatcher scheduler, and admin consoles).
  ```bash
  cd web
  npx playwright test
  ```
- **AI Multi-Agent Subsystem (Pytest)**: `ai/tests/` (Unit tests for LangGraph workflows, deterministic guardrails, and tool execution).
  ```bash
  cd ai
  pytest tests/ -v
  ```
