# WayPoint Requirements Traceability Matrix

This document provides complete end-to-end traceability connecting **SE3090 Assignment Requirements** to **WayPoint System Requirements**, **System Features / Components**, **Planned Implementations**, **Test Evidence**, and **Report Evidence**.

---

## Requirements Traceability Matrix Table

| Assignment Req ID | WayPoint Req ID | Feature / Component | Planned Implementation | Test Evidence | Report Evidence |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`REQ-ASSIGN-01`** (Group Size & Ownership) | `REQ-INDIV-01`, `REQ-INDIV-02` | Group & Component Structure | 4 students owning vertical slices: Journey Planning, Fleet/Resource, Booking, Disruption. | Git commit history by author, PR ownership. | Section 3 of Consolidated Report, Individual Reports. |
| **`REQ-ASSIGN-04`** (Integrated System Rule) | `FR-AUTH-003`, `REQ-BE-01` | Shared Platform Architecture | React and Flutter consuming identical ASP.NET Core API & PostgreSQL DB. | Integration E2E tests, API integration tests. | Architecture diagram, System Design report. |
| **`REQ-ASSIGN-05`** (Level 1 Viva Policy) | `REQ-INDIV-04` | Viva Defense & Code Ownership | Students explain, test, modify, debug owned code live without AI tools. | Embedded viva questions, live debugging demo. | Individual reflection, Signed declaration. |
| **`REQ-TECH-01`** (ASP.NET Core Web API) | `REQ-BE-01`, `REQ-BE-02` | Backend API Layer | C# ASP.NET Core Web API controllers, DTOs, async services, Swagger/OpenAPI. | xUnit API Controller integration tests. | Technical Report Section 5 (API Design). |
| **`REQ-TECH-02`** (PostgreSQL) | `REQ-DB-01`, `REQ-DB-02` | Relational Data Store | PostgreSQL database with EF Core migrations, indexes, constraints. | Database migration & transaction integration tests. | Database Report, ER Diagrams. |
| **`REQ-TECH-03`** (EF Core) | `REQ-DB-02`, `REQ-DB-03` | Data Access & Transactions | `Npgsql.EntityFrameworkCore.PostgreSQL`, DbContext transactions for seat holds. | Concurrent seat hold unit & transaction tests. | Database Report Section 6. |
| **`REQ-TECH-04`** (React Web App) | `FR-FE-01` to `06` | Operator Workspace | Functional React components, Hooks, React Router, state management. | React Testing Library component & route tests. | React Web Report Section 7. |
| **`REQ-TECH-05`** (Flutter Mobile App) | `FR-FE-07` to `13` | Passenger Application | Flutter & Dart widgets, secure token storage, QR scanning device feature. | Flutter widget, navigation & integration tests. | Flutter Mobile Report Section 8. |
| **`REQ-TECH-06`** (Agentic AI Subsystem) | `FR-AI-001` to `008` | Multi-Agent AI Subsystem | Level 4 multi-agent orchestration, 10 allow-listed tools, persisted state. | Golden test cases, schema validation tests. | Agentic AI Evaluation Report (5–8 pgs). |
| **`REQ-FE-01`** (Operator Dashboard) | `FR-FE-01` | React Dashboard Widgets | Occupancy, revenue, upcoming departure analytics widgets. | Component render & mock data tests. | Technical Report (React UI). |
| **`REQ-FE-04`** (Manager Approval Workbench) | `FR-DISRUPTION-003`, `FR-APPROVAL-001` | Manager Approval Workbench | UI displaying before/after impact, passenger metrics, approval controls. | Protected route & state transition tests. | Technical Report (Approval Workbench). |
| **`REQ-FE-10`** (Interactive Seat Map) | `FR-FLEET-003`, `FR-BOOKING-001` | Flutter Seat Picker | Visual seat template renderer, status indicators, hold timer. | Widget render & state update tests. | Technical Report (Flutter Screens). |
| **`REQ-FE-11`** (QR E-Ticket Wallet) | `FR-BOOKING-003`, `FR-FE-11` | Digital Wallet & QR Code | QR code generation, storage, and scanning verification. | Scanner integration & verification tests. | Technical Report (E-Ticket Module). |
| **`REQ-BE-03`** (JWT Auth & RBAC) | `FR-AUTH-002`, `FR-AUTH-003` | Security Middleware | JWT bearer authentication, claims-based role policies (`[Authorize]`). | Auth middleware unit & HTTP 401/403 tests. | Security Considerations Section. |
| **`REQ-DB-03`** (Seat Hold Transactions) | `FR-BOOKING-001`, `BR-HOLD-001` | Transactional Seat Lock | `IDbContextTransaction` protecting seat status and `SeatHold` locks. | Concurrency load tests (duplicate holds). | Database & Technical Report. |
| **`REQ-DB-05`** (AI State Persistence) | `FR-AI-001`, `FR-AI-007` | AI Persistence Tables | Tables `AiWorkflow`, `AiWorkflowStep`, `AiToolCall`, `AiValidationResult`. | DB persistence unit tests. | Agentic AI & Database Report. |
| **`REQ-AI-02`** (Planning & Delegation) | `FR-AI-001`, `REQ-MULTI-01` | Planner Agent | Planner agent decomposing objectives into ordered multi-step execution plans. | Plan structure assertion tests. | Agentic AI Evaluation Report. |
| **`REQ-AI-03`** (10 Allow-Listed Tools) | `FR-AI-002` | Tool Runner Service | 10 backend tool wrappers validating DTO inputs and executing business logic. | Tool DTO validation unit tests. | Agentic AI Evaluation Report. |
| **`REQ-AI-06`** (Manager Approval Boundary) | `FR-DISRUPTION-003`, `BR-APPROVAL-001` | Human Approval Gate | Safety agent gating high-impact changes in `PendingManagerApproval` state. | Approval boundary enforcement tests. | Agentic AI Evaluation Report. |
| **`REQ-AI-08`** (Prompt Injection & Safe Failure) | `FR-AI-004`, `FR-AI-008` | AI Resilience & Guardrails | Input sanitization, timeouts, retry caps, safe-failure state fallback. | Prompt injection & error recovery tests. | Agentic AI Evaluation Report. |
| **`REQ-INDIV-01`** (Vertical Ownership) | `REQ-INDIV-01` to `04` | Individual Component Ownership | Student 1 (Journey), Student 2 (Fleet), Student 3 (Booking), Student 4 (Disruption). | Git commit history by student name. | Individual Report Sections (1 per student). |
| **`REQ-API-01`** (4 Endpoints/Component) | `REQ-API-01`, `REQ-API-02` | REST Controllers | ≥4 endpoints per component + ≥1 complex business operation beyond CRUD. | Swagger/OpenAPI documentation. | Technical Report API Section. |
| **`REQ-TEST-05`** (Cross-Platform E2E Test) | `REQ-TEST-05` | End-to-End Workflow Test | Flutter search → API → DB → AI workflow → React manager approval → API → Flutter status. | E2E integration test suite execution. | Software Testing Report (6–10 pgs). |
| **`REQ-TEST-06`** (Agent Evaluation Suite) | `REQ-TEST-06`, `REQ-TEST-07` | AI Evaluation Suite | Golden test cases, plan assertions, tool selection checks, safe-failure tests. | Automated AI evaluation test results. | Agentic AI Evaluation Report. |
| **`REQ-GIT-01`** (GitHub Monorepo & CI) | `REQ-GIT-01` to `04`, `REQ-CICD-01` | GitHub Actions CI | `.github/workflows/ci.yml` restoring, building, testing backend on push/PR to main. | Passing CI build logs & PR checks. | Software Testing & Git Report. |
| **`REQ-DEP-01`** (Cloud API & Health) | `REQ-DEP-01` to `06` | Cloud Infrastructure | ASP.NET Core API deployed on cloud host with working `/health` & Swagger URL. | Live URL verification & health endpoint checks. | Deployment Report (3–5 pgs). |
| **`REQ-DOC-01`** (Consolidated Report) | `REQ-DOC-01` to `05` | Project Documentation | Single PDF (`SE3090_GroupNumber.pdf`), Group report, Individual reports, 3–6 ADRs. | Submitted PDF report file. | Course Web Submission. |
| **`REQ-DOC-04`** (Demo Video Link) | `REQ-DOC-04` | 10-Minute Demo Video | Public/unlisted 10-minute video link showcasing end-to-end integrated system. | Working video URL in report. | Group Report Section. |

---

## Requirements Needing Team Confirmation

The following technical and design items require explicit team confirmation prior to implementation:

1. **Exact Seat Hold Timeout**: Confirm whether the temporary seat hold lock duration is 10 minutes or 15 minutes.
2. **Connecting Transfer Buffer Duration**: Confirm minimum transfer window for connecting intercity services (proposed: 20 minutes).
3. **Disruption Severity Thresholds**: Define exact quantitative criteria for a "Major Timetable Change" requiring manager approval (e.g., departure time shift > 15 minutes).
4. **Third-Party Payment Sandbox Vendor**: Select between Stripe Test Mode, PayHere Sandbox, or custom ASP.NET Core Mock Payment Gateway.
5. **Agentic AI Orchestration Framework**: Confirm final choice for `ADR-003` (Microsoft Semantic Kernel in C# vs Python LangGraph microservice).
6. **Cloud Hosting Platform Provider**: Confirm final provider choice for `ADR-005` (Render vs Azure App Service / Railway).
