# WayPoint Project & SE3090 Assignment Analysis

This document provides a complete, authoritative analysis of the **WayPoint Project Proposal** (`docs/referance/WayPoint_Revised_Project_Proposal.pdf`) and the official **SE3090 Assignment 1 Specification and Marking Scheme** (`docs/referance/2026-S1-SE3090-Assignment 1 - Integrated Full-Stack and Agentic AI Application Development-SpecificationWithMarkingScheme.pdf`).

---

## 1. Executive Summary & Context

- **Academic Module**: SE3090 – Software Engineering Frameworks (Year 3, Semester 1, 2026).
- **Weighting**: 25% of final module mark (scaled from 100 raw evaluation marks).
- **Duration**: 9 weeks (31 July 2026 – 30 September 2026).
- **Domain**: Sri Lankan intercity and tourist-corridor journey planning, seat booking, and transport operations (e.g., Colombo–Ella, Colombo–Kandy, Colombo–Galle, Colombo–Nuwara Eliya, Colombo–Sigiriya, Colombo–Trincomalee).
- **Core Product Purpose**: Move beyond simple bus booking to provide a multi-modal journey planning engine, seat reservation platform, and disruption management system powered by controlled Agentic AI workflows.

---

## 2. Assignment Requirements (`REQ-ASSIGN-xx`)

| ID | Requirement | Description & Specification Source |
| :--- | :--- | :--- |
| `REQ-ASSIGN-01` | **Group Size** | Standard group size is exactly four (4) students. Each student must own one primary business component. |
| `REQ-ASSIGN-02` | **Submission Deadline** | Wednesday, 30 September 2026 at 11:50 PM via Course Web by the nominated group leader. |
| `REQ-ASSIGN-03` | **Evaluation Model** | Single final evaluation combining a **10-minute live demonstration** and a **20-minute viva/technical examination** (100 raw marks = 30 group + 70 individual). |
| `REQ-ASSIGN-04` | **Integrated-System Rule** | Disconnected prototypes are strictly prohibited. React and Flutter applications MUST consume the same ASP.NET Core Web API, PostgreSQL database, user identities, permissions, and server-side business rules. |
| `REQ-ASSIGN-05` | **Level 1 Viva Policy** | Development permits Level 4 (Full AI) with disclosure, but the final demonstration and viva are strictly **Level 1 (No AI)**. Students cannot use AI tools during the viva and must be able to explain, test, modify, and debug submitted work. |
| `REQ-ASSIGN-06` | **Access Period** | The repository, demonstration video link, deployed API/Web app, and database must remain active and accessible to evaluators until at least **Wednesday, 21 October 2026** (3 weeks post-submission). |

---

## 3. Mandatory Technologies (`REQ-TECH-xx`)

| ID | Layer / Category | Mandatory Technology Choice | Constraints & Notes |
| :--- | :--- | :--- | :--- |
| `REQ-TECH-01` | **Backend API** | C# and ASP.NET Core Web API | Public authoritative backend for all clients. |
| `REQ-TECH-02` | **Database** | PostgreSQL | Authoritative relational data store. |
| `REQ-TECH-03` | **Data Access** | Entity Framework (EF) Core | EF Core PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`), migrations, and transactions. |
| `REQ-TECH-04` | **Web Application** | React | Functional components, React Hooks, React Router, and justified state management. |
| `REQ-TECH-05` | **Mobile Application** | Flutter & Dart | Cross-platform mobile app, justified state management, runnable Android APK output. |
| `REQ-TECH-06` | **AI Subsystem** | Agentic AI | Level 4 multi-agent orchestration integrated strictly via ASP.NET Core. |
| `REQ-TECH-07` | **Version Control** | Git & GitHub | Monorepo created from day 1 with GitHub Actions CI workflow. |

---

## 4. Mandatory Frontend Requirements (`REQ-FE-xx`)

### 4.1 React Web Application (Operator & Manager Workspace)
- `REQ-FE-01` (**Role-Based Dashboard**): Summary widgets covering route/service occupancy, revenue, upcoming departures, failed payments, disruptions, and journey planning analytics.
- `REQ-FE-02` (**Business Data Management**): Full CRUD interfaces with validation, search, filtering, sorting, and pagination for routes, stops, tourist destinations, services, buses, drivers, seat layouts, amenities, and fare rules.
- `REQ-FE-03` (**Disruption Workbench**): Workspace displaying candidate alternatives, resource feasibility evidence, and passenger impact analysis.
- `REQ-FE-04` (**Manager Approval Workbench**): Dedicated UI for Transport Managers to inspect before/after operational impacts, affected passenger metrics, validation summaries, and execute approve/reject/request-revision actions.
- `REQ-FE-05` (**AI Agent Monitoring**): Real-time/historical execution summaries showing workflow states, tool invocation traces, validation outputs, step timings, error logs, retries, and final safe-failure outcomes.
- `REQ-FE-06` (**UI Quality & Routing**): Protected routes, role-based navigation, responsive layouts, and explicit visual states for loading, empty data, success, and error handling.

### 4.2 Flutter Mobile Application (Passenger Application)
- `REQ-FE-07` (**Authentication & Security**): Registration, login, logout, secure JWT token storage (e.g., `flutter_secure_storage`), and protected screen routes.
- `REQ-FE-08` (**Journey Search & Filtering**): Search by origin, destination, travel date/time, passenger count, with preference filters (arrival deadline, direct service, budget, bus amenities, boarding point).
- `REQ-FE-09` (**Journey Comparison Cards**): Display candidate options comparing departure, arrival, duration, total fare, directness, boarding point, amenities, and real-time seat availability.
- `REQ-FE-10` (**Interactive Seat Layout**): Visual bus seat template rendering, seat status indicators (available, held, booked), and interactive seat selection.
- `REQ-FE-11` (**Ticketing & Wallet**): Payment sandbox checkout integration, digital e-ticket wallet, and QR code generation/scanning for ticket verification.
- `REQ-FE-12` (**Disruption Rebooking & History**): Booking history view, cancellation/refund eligibility checking, push/in-app service alerts, and disruption rebooking responses.
- `REQ-FE-13` (**Meaningful Device Feature**): Implementation of QR scanning, GPS/Map guidance for boarding points, date/time pickers, and push/local notifications.

---

## 5. Backend Requirements (`REQ-BE-xx`)

- `REQ-BE-01` (**Architecture & Abstractions**): Clean architecture using Controllers, DTOs, Application/Service layer, data access abstraction (EF Core Repositories/DbContext), and Dependency Injection.
- `REQ-BE-02` (**REST API Standards**): Proper RESTful routes, HTTP methods (`GET`, `POST`, `PUT`, `DELETE`, `PATCH`), standard HTTP status codes, request/response models, and asynchronous (`async`/`await`) operations.
- `REQ-BE-03` (**Authentication & Security**): JWT bearer token authentication, Role-Based Access Control (`[Authorize(Roles = "...")]`), password hashing (BCrypt/Argon2), DTO input validation, and CORS policies.
- `REQ-BE-04` (**Quality & Infrastructure**): Global exception handling middleware, structured logging (e.g., Serilog), API documentation via Swagger/OpenAPI, and zero hardcoded credentials.
- `REQ-BE-05` (**Agentic AI Integration Endpoints**): Dedicated backend endpoints to trigger AI agent workflows, retrieve active workflow status, inspect execution traces, and process manager approval/rejection decisions.
- `REQ-BE-06` (**Third-Party Service Proxying**): All external third-party API calls (e.g., Payment Sandbox, Map/Location API) MUST pass through ASP.NET Core. Clients never communicate directly with external providers.

---

## 6. PostgreSQL / Database Requirements (`REQ-DB-xx`)

- `REQ-DB-01` (**Data Modeling**): Normalized relational database design with clear primary keys, foreign keys, cascade rules, unique constraints, and search/seat lookup indexes.
- `REQ-DB-02` (**Migrations & Seeding**): Schema management strictly using Entity Framework Core migrations and initial seed scripts for routes, stops, buses, seat templates, fare rules, and test accounts.
- `REQ-DB-03` (**Transactional Integrity**): Database transactions (`IDbContextTransaction`) protecting seat hold reservations, payment confirmation, and operational reassignments against race conditions.
- `REQ-DB-04` (**Auditability**): Standardized audit timestamps (`CreatedAt`, `UpdatedAt`) on entities.
- `REQ-DB-05` (**AI State Persistence**): Structured database tables (`AiWorkflow`, `AiWorkflowStep`, `AiToolCall`, `AiValidationResult`) to persist workflow execution history, tool inputs/outputs, validation outcomes, and approval states.
- `REQ-DB-06` (**Sensitive Data Rule**): Database MUST NOT store hidden AI model reasoning text, raw passwords, unhashed tokens, or unencrypted payment card credentials.

---

## 7. Agentic AI Requirements (`REQ-AI-xx`)

- `REQ-AI-01` (**Level 4 Workflow**): The AI subsystem must solve a multi-step domain objective. Generic chatbots, single-prompt generators, or simple Q&A tools do NOT satisfy the requirement.
- `REQ-AI-02` (**Planning & Delegation**): System analyzes user/disruption objectives, generates a structured execution plan, and delegates tasks to distinct agent roles.
- `REQ-AI-03` (**10 Allow-Listed Tools**):
  1. `SearchRoutes` / `SearchServices`
  2. `GetBoardingPoints` / `GetTimetable`
  3. `CheckTransferFeasibility`
  4. `CheckSeatAvailability` / `CheckReplacementResources`
  5. `CalculateFareDifference` / `CheckCancellationPolicy`
  6. `CreateRebookingProposal`
  7. `CalculatePassengerImpact`
  8. `RequestManagerApproval`
  9. `ApplyApprovedOperationalChange`
  10. `SendPassengerNotification`
- `REQ-AI-04` (**Tool Input/Output Controls**): Strict DTO validation on all tool parameters, structured JSON returns, least-privilege execution, and zero direct access to PostgreSQL, shell, or raw API keys.
- `REQ-AI-05` (**Deterministic Server-Side Validation**): Backend code must validate AI outputs against business rules (timetable consistency, transfer windows, seat maps, fare rules) before accepting recommendations or triggering approval states.
- `REQ-AI-06` (**Human Approval Boundary**): High-impact operational changes (ticketed service cancellation, major timetable shifts, disruptive bus/driver reassignment) MUST pause execution in `PendingManagerApproval` state until authorized by a Transport Manager.
- `REQ-AI-07` (**Observability & Audit Traces**): Complete persistence and UI display of workflow IDs, execution steps, tool call logs, execution timings, retry counts, errors, and manager decisions.
- `REQ-AI-08` (**Prompt Injection Resistance & Safe Failure**): System must sanitize prompt inputs, apply timeouts and retry caps, handle LLM errors gracefully, and default to a safe-failure state without corrupting operational records.

---

## 8. Multi-Agent Requirements (`REQ-MULTI-xx`)

- `REQ-MULTI-01` (**Four Distinct Agents**): The standard 4-student group must implement at least four (4) distinct specialized agents with unique contracts:
  1. **Planner / Coordinator Agent**: Analyzes overall objective, creates ordered multi-step execution plans, and delegates work.
  2. **Journey Analysis Agent**: Evaluates route structures, stops, timetables, transfer points, and service compatibility.
  3. **Resource & Booking Agent**: Checks fleet status, replacement buses, driver availability, seat layouts, fares, and booking policies.
  4. **Validation & Safety Agent**: Enforces business rules, calculates passenger impact metrics, classifies severity, and gates human approval.
- `REQ-MULTI-02` (**Distinct Agent Definition Rule**): An agent counts as distinct ONLY if it has an identifiable responsibility, defined input/output contract, controlled tool permissions, and visible execution participation. Renaming prompts or duplicating logic does NOT count as a separate agent.

---

## 9. Individual Contribution Requirements (`REQ-INDIV-xx`)

- `REQ-INDIV-01` (**Vertical Component Ownership**): Each student MUST take primary ownership of one business component across the complete tech stack:

| Student | Component Title | Domain Scope |
| :--- | :--- | :--- |
| **Student 1** | Journey Planning & Route Catalogue | Routes, stops, tourist destinations, timetables, service search, journey candidate generation, Journey Planner Agent. |
| **Student 2** | Fleet, Seat & Resource Feasibility | Buses, seat layouts, driver assignments, maintenance records, resource availability, Resource Feasibility Agent. |
| **Student 3** | Booking, Ticketing & Passenger Options | Seat holds, payment processing, QR tickets, cancellations, refunds, Booking Options Agent. |
| **Student 4** | Disruption, Rebooking & Approval | Service alerts, disruption records, passenger impact, rebooking proposals, approval workflow, Validation & Safety Agent. |

- `REQ-INDIV-02` (**No Specialized Roles**): Project-manager-only, testing-only, or documentation-only roles are strictly prohibited. Every student must write backend, database, React, Flutter, AI, and test code.
- `REQ-INDIV-03` (**70 Individual Marks Allocation**):
  - ASP.NET Core API (10 marks)
  - PostgreSQL Integration & Data Modeling (10 marks)
  - React Web Application (10 marks)
  - Flutter Mobile Application (10 marks)
  - Individual Agentic AI Contribution (12 marks)
  - API Integration, Security & Cross-Platform (10 marks)
  - Testing, CI & Git Workflow (8 marks)
- `REQ-INDIV-04` (**Viva Ownership Defense**): Students must independently explain, test, modify, and debug their code during the viva. Code that a student cannot defend will receive zero or reduced marks.

---

## 10. API Requirements (`REQ-API-xx`)

- `REQ-API-01` (**Endpoint Quantity Rule**): Each student-owned component MUST expose **at least four (4) meaningful API endpoints**.
- `REQ-API-02` (**Business Operation Beyond CRUD Rule**): Each component MUST implement **at least one (1) business-specific complex operation beyond standard CRUD** (e.g., Journey Candidate Generation, Replacement Resource Feasibility, Transactional Seat Hold & Payment Confirmation, Disruption Rebooking & Manager Approval).
- `REQ-API-03` (**REST Conventions**): Consistent URI resource naming (`/api/v1/routes`, `/api/v1/bookings`), query filtering parameters, and standardized JSON responses.

---

## 11. Testing Requirements (`REQ-TEST-xx`)

- `REQ-TEST-01` (**Backend Testing**): Unit tests, service-layer tests, validation tests, auth/RBAC tests, and controller integration tests (using xUnit/NUnit/MSTest).
- `REQ-TEST-02` (**Database Testing**): PostgreSQL integration tests verifying schema constraints, EF Core migrations, transactional behavior, and concurrent seat hold isolation.
- `REQ-TEST-03` (**React Testing**): Component rendering, form validation, protected route navigation, API integration, and error state tests.
- `REQ-TEST-04` (**Flutter Testing**): Unit tests, widget tests, navigation routing tests, form validation, and API client integration tests.
- `REQ-TEST-05` (**End-to-End (E2E) Workflow Test**): At least one complete cross-platform test trace: Flutter search → ASP.NET Core API → PostgreSQL → Agentic AI execution → React manager approval → ASP.NET Core transaction → Flutter status update.
- `REQ-TEST-06` (**Agent Evaluation Suite**): Golden test cases, structured plan assertions, agent participation checks, tool selection validation, schema compliance, approval boundary enforcement, prompt injection resistance, failure recovery, and safe-failure tests.
- `REQ-TEST-07` (**LLM-as-a-Judge Rule**): LLM-as-a-judge may be used ONLY as supporting evidence. Core agent evaluation MUST rely on rule-based assertions, schema validation, golden test cases, and deterministic validators.
- `REQ-TEST-08` (**Performance Testing**): Load and performance benchmarks measuring concurrent requests, response latency, database query times, and AI workflow execution latency.

---

## 12. Git & GitHub Requirements (`REQ-GIT-xx`)

- `REQ-GIT-01` (**Early Repository Creation**): Single GitHub repository established from the beginning of the project.
- `REQ-GIT-02` (**Collaboration Workflow**): Feature branching (`feature/name`), descriptive commit messages, GitHub Issues, Pull Requests (PRs), code reviews, and GitHub Project Board management.
- `REQ-GIT-03` (**Regular Technical Commits**): Clear, continuous commit history across all 4 students throughout the 9-week lifecycle.
- `REQ-GIT-04` (**Anti-Fabrication Rule**): Artificial commit activity, bulk uploads on submission day, or unexplained copied code will be rejected as evidence and penalized.

---

## 13. CI/CD Requirements (`REQ-CICD-xx`)

- `REQ-CICD-01` (**GitHub Actions Pipeline**): Mandatory GitHub Actions CI workflow (`.github/workflows/ci.yml`) configured to restore dependencies, build, and run all backend automated tests on every push and pull request to `main`.
- `REQ-CICD-02` (**Frontend & Mobile CI (Optional/Recommended)**): Additional automated build/lint pipelines for React and Flutter analysis.

---

## 14. Deployment Requirements (`REQ-DEP-xx`)

- `REQ-DEP-01` (**ASP.NET Core Web API Cloud Deployment**): Deployed to a cloud platform (e.g., Azure App Service, Render, Railway) exposing a working `/health` endpoint and live Swagger UI URL.
- `REQ-DEP-02` (**PostgreSQL Managed Cloud DB**): Deployed securely on a cloud database service with applied EF Core migrations and restricted access credentials.
- `REQ-DEP-03` (**React Live Web App**): Deployed to a cloud host (e.g., Vercel, Netlify, Render) configured to interact with the deployed cloud API.
- `REQ-DEP-04` (**Flutter Android APK**): Compiled, runnable Android APK (`.apk`) file submitted with clear installation and testing instructions.
- `REQ-DEP-05` (**Agentic AI Subsystem Deployment**): Deployed to cloud or documented for local execution with clear setup order, model requirements, and environment variables.
- `REQ-DEP-06` (**No-Cost Services Rule**): All deployments must be achievable using institution-provided or free-tier cloud services. Paid subscriptions are not required.

---

## 15. Documentation & Report Requirements (`REQ-DOC-xx`)

- `REQ-DOC-01` (**Single Consolidated PDF Report**): One combined PDF report submitted via Course Web by the group leader, adhering to naming convention `SE3090_GroupNumber.pdf` (e.g., `SE3090_G07.pdf`).
- `REQ-DOC-02` (**Group Report Sections**):
  - Project Overview & Scope
  - System Architecture & ER Diagrams
  - Technical Report (10–15 pages guidance)
  - Software Testing Report (6–10 pages guidance)
  - Agentic AI Evaluation Report (5–8 pages guidance)
  - Performance Report (3–5 pages guidance)
  - Deployment Report (3–5 pages guidance)
  - Architectural Decision Records (ADRs) (3–6 pages guidance)
  - Security Considerations & Group AI Usage Declaration
- `REQ-DOC-03` (**Individual Report Sections**):
  - Contribution statement & owned component breakdown.
  - Key commit, PR, and test evidence.
  - Challenges and technical learning.
  - Individual AI Usage Log (date, tool/model, task, prompt output, user edits, verification method).
  - Approximately **1-page Individual AI Reflection** (marked under Documentation & Deployment).
  - Signed declaration of originality.
- `REQ-DOC-04` (**10-Minute Demonstration Video**): Public or unlisted video link (e.g., YouTube/Vimeo/Drive) demonstrating the complete integrated system without requiring permission access.
- `REQ-DOC-05` (**Architecture Decision Records (ADRs)**): At least 3 to 6 formal ADRs written in `docs/adr/`, covering at minimum:
  1. React State Management Framework
  2. Flutter State Management Framework
  3. Agentic AI Framework & Orchestration Method
  4. AI Workflow State Persistence Strategy in PostgreSQL
  5. Cloud Deployment Platform & Infrastructure Strategy

---

## 16. Security Requirements (`REQ-SEC-xx`)

- `REQ-SEC-01` (**Authentication & Authorization**): JWT authentication with claim-based role authorization protecting administrative and passenger APIs.
- `REQ-SEC-02` (**Credential Protection**): Zero secrets, private keys, or API tokens committed to Git. All credentials loaded strictly from environment variables (`.env`).
- `REQ-SEC-03` (**Password Security**): Secure password hashing algorithms (BCrypt, Argon2, PBKDF2).
- `REQ-SEC-04` (**Input Validation & Sanitization**): Server-side DTO validation for all API endpoints and AI tool inputs to prevent SQL injection and cross-site scripting (XSS).
- `REQ-SEC-05` (**AI Security & Guardrails**): Prompt-injection resistance, output schema validation, execution timeouts, retry caps, and strict role permissions for tool execution.
- `REQ-SEC-06` (**Audit Logging**): Complete audit trail for high-impact manager approvals and operational state changes.

---

## 17. Marking-Scheme-Sensitive Requirements (`REQ-MARK-xx`)

| Criterion | Group / Individual | Marks | Key Compliance Requirement |
| :--- | :--- | :--- | :--- |
| **Component Design & Business Logic** | Group | **10** | Fully functional business components supporting an end-to-end workflow. |
| **Integrated Architecture & AI Orchestration** | Group | **10** | Complete integrated system, 4 distinct agents, persisted state, allow-listed tools, deterministic validation, safe failure, human approval. |
| **Documentation & Deployment** | Group | **10** | Complete consolidated report, ADRs, working cloud deployment, working APK, 10-min demo video link. |
| **ASP.NET Core RESTful API** | Individual | **10** | REST conventions, DTOs, async, validation, auth, viva defense & live code modification. |
| **PostgreSQL & Data Modeling** | Individual | **10** | Schema design, relationships, EF Core migrations, indexes, transaction isolation, viva defense. |
| **React Web Application** | Individual | **10** | Functional components, Hooks, state management, protected routes, responsive UI, viva defense. |
| **Flutter Mobile Application** | Individual | **10** | Reusable widgets, state management, secure storage, device feature, responsive screens, viva defense. |
| **Individual Agentic AI Contribution** | Individual | **12** | Distinct agent role, defined input/output contract, tool permissions, validation, error handling, tests, viva defense. |
| **API Integration, Security & Cross-Platform** | Individual | **10** | Shared API usage, JWT handling, RBAC, cross-platform workflow, security controls, viva defense. |
| **Testing, CI & Git Workflow** | Individual | **8** | Test coverage across layers, passing GitHub Actions CI, clear Git commit history & PR reviews. |
| **Total Marks** | | **100** | **Scaled to 25% of final SE3090 module grade.** |

---

## 18. Risks of Failing to Satisfy the Assignment (`REQ-RISK-xx`)

1. **Disconnected Prototype Risk (`REQ-RISK-01`)**: Building standalone React or Flutter apps that do not consume the shared ASP.NET Core API results in heavy group mark deduction.
2. **Missing Component / Specialized Role Risk (`REQ-RISK-02`)**: Assigning a team member a "management-only" or "docs-only" role results in immediate failure for that student (0/70 individual marks).
3. **Generic Chatbot Risk (`REQ-RISK-03`)**: Implementing a simple single-prompt chatbot instead of a multi-agent workflow results in 2/10 group marks and 2/12 individual AI marks.
4. **Viva Inability Risk (`REQ-RISK-04`)**: Failing to explain, test, modify, or debug submitted code during the live viva results in 0 to 4 marks per individual criterion.
5. **Level 1 Viva Violation (`REQ-RISK-05`)**: Using ChatGPT, Copilot, or external AI during the live viva causes immediate penalty or disqualification.
6. **Hardcoded Secrets Risk (`REQ-RISK-06`)**: Committing API keys or DB passwords to GitHub violates academic integrity and security rules.
7. **Fabricated Commit History (`REQ-RISK-07`)**: Bulk pushing commits on the final day without PR history leads to rejection of Git evidence.

---

## 19. Requirement Gaps & Contradictions

### 19.1 Gaps (Assignment Specification vs Proposal)
1. **Demonstration Video Link**: The Assignment Specification explicitly mandates a 10-minute demonstration video link in the consolidated report. The proposal mentions expected demonstration steps but did not include video production specs.
2. **Required Access Window**: Specification requires system accessibility through 21 October 2026. Proposal did not specify post-submission hosting timelines.
3. **Individual AI Usage Logs & Reflections**: Specification requires an explicit log table (date, tool, task, output, edits, verification) and a 1-page reflection per student. The proposal did not mention individual reflection documents.

### 19.2 Contradictions / Clarifications
- **AI Framework Selection**: Proposal Figure 1 mentions "Ollama local model", whereas the Assignment Specification gives full implementation flexibility (LangGraph, Microsoft Agent Framework, LlamaIndex, Google ADK, custom C#). **Resolution**: The team must choose and document the exact orchestration stack in ADR #3.

---

## 20. Implementation Gate (Pre-Coding Requirements Checklist)

The team MUST resolve and document the following items in project documentation BEFORE writing production application code:

- [ ] **Gate 1 — Component Ownership Assignment**: Assign Students 1 through 4 to the 4 business components (Journey Planning, Fleet/Resource, Booking/Ticketing, Disruption/Approval).
- [ ] **Gate 2 — Architectural Decision Records (ADRs)**: Draft and approve preliminary ADRs in `docs/adr/`:
  - `ADR-01`: React State Management (e.g., Zustand vs Redux Toolkit).
  - `ADR-02`: Flutter State Management (e.g., Bloc vs Riverpod).
  - `ADR-03`: Agentic AI Framework & LLM Orchestration (e.g., Semantic Kernel vs LangChain/LangGraph microservice).
  - `ADR-04`: AI Workflow State Schema & Persistence in PostgreSQL.
  - `ADR-05`: Cloud Hosting Platform (e.g., Render vs Azure App Service).
- [ ] **Gate 3 — Database Schema & ERD Approval**: Finalize relational schema for Routes, Stops, Buses, SeatLayouts, Services, Bookings, DisruptionCases, and `AiWorkflow` tables in `docs/architecture/`.
- [ ] **Gate 4 — API Contract Specification**: Define Swagger/OpenAPI endpoints and DTO schemas for all 16+ API endpoints.
- [ ] **Gate 5 — Allow-Listed Tool Contracts**: Define exact JSON schemas for the 10 allow-listed Agentic AI tools.
- [ ] **Gate 6 — Business Rule Thresholds**: Define connecting transfer buffer times (e.g., 20 mins) and major timetable change criteria (>15 min shift).

---

## 21. Recommended Next Documentation Steps

1. Create **`docs/architecture/system-architecture.md`** detailing component boundaries, DB ERD, and API contracts.
2. Create initial **ADR documents** in `docs/adr/` (`0001-react-state-management.md`, `0002-flutter-state-management.md`, `0003-agentic-ai-framework.md`, `0004-ai-state-persistence.md`, `0005-cloud-deployment.md`).
3. Finalize team member component assignments in **`AGENTS.md`** and **`README.md`**.
