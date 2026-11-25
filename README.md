# WayPoint — Integrated Full-Stack & Agentic AI Transit Platform

> **Status: Active Development (`v0.1.0` / Work in Progress)**  
> WayPoint is currently under active development. Core multi-tier foundations, domain APIs, web/mobile client slices, and the multi-agent AI workflow are implemented and testable locally, while several integrations, production hardening tasks, and end-to-end flows remain in progress.

[![Status](https://img.shields.io/badge/Status-In%20Development-orange.svg)](#development-status--roadmap)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![React 18](https://img.shields.io/badge/React-18.3-61DAFB.svg)](https://react.dev/)
[![Flutter 3.x](https://img.shields.io/badge/Flutter-3.x-02569B.svg)](https://flutter.dev/)
[![Python 3.11](https://img.shields.io/badge/Python-3.11-3776AB.svg)](https://www.python.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-EF%20Core%209-336791.svg)](https://www.postgresql.org/)

**WayPoint** is a modular, multi-tier intercity bus transit management and journey planning platform designed for Sri Lankan transit corridors (SE3090). It brings together journey search, dynamic bus seat layouts and temporary seat holds, sandboxed checkout and QR ticketing, disruption management, and an advisory LangGraph multi-agent AI microservice.

---

## Development Status & Roadmap

WayPoint is **still being actively developed**. Below is a snapshot of what is currently built versus what is still in progress:

### Implemented Foundations (Available Locally)
- **Authoritative ASP.NET Core 8 API (`backend/`)**: Clean Architecture solution (`Domain`, `Application`, `Infrastructure`, `API`, `Tests`) with 22 REST controllers, JWT authentication, role-based authorization policies, EF Core 9 + Npgsql 9 PostgreSQL integration, CLI database seeding (`--seed`), and `/health` + Swagger/OpenAPI endpoints.
- **React 18 Web Dashboard (`web/`)**: Operator, Transport Manager, and Admin portal built with Vite 5, Tailwind CSS 3.4, Zustand, TanStack Query v5, and React Router v6 across journey, fleet, booking manifest, disruption/AI observability, and user admin modules.
- **Flutter Mobile Client (`mobile/`)**: Cross-platform mobile app (`flutter_bloc`, `dio`, `flutter_secure_storage`, `qr_flutter`, `mobile_scanner`) with onboarding, auth gate, guest exploration, journey search/comparison, interactive seat picker, sandbox checkout, ticket wallet, disruption alerts, and conductor scanner/manifest shells.
- **Agentic AI Microservice (`ai/`)**: FastAPI + LangGraph workflow (`planner → journey → resource → booking → safety`) with 10 allow-listed HTTP tools, input sanitization, deterministic output guardrails, safe-failure wrappers, and workflow telemetry logging.
- **Automated Test Suites**: Unit, widget/component, and integration tests across `backend/WayPoint.Tests/`, `web/`, `mobile/`, `ai/tests/`, and `tests/`, plus GitHub Actions CI/CD workflows (`.github/workflows/`).

### Still in Progress / Planned
- [ ] **Live Payment Gateway Integration**: Checkout currently uses a simulated sandbox payment endpoint (`/api/v1/payments/confirm-sandbox-charge`) rather than a live payment processor.
- [ ] **Role & Conductor Flow Alignment**: Aligning mobile `ConductorNavigationShell` role checks (`conductor`, `transitmanager`, `driver`) with backend `UserRoleType` (`Passenger`, `Operator`, `TransportManager`, `Admin`) and adding dedicated conductor seed accounts.
- [ ] **Background Job Workers & Realtime Push**: Automated background expiration sweeps for 10-minute seat holds and real-time WebSocket/push notifications for disruption alerts.
- [ ] **Full AI Tool & Observability Wiring**: Hardening end-to-end live LLM tool execution against seeded schedules, prompt tuning, and live trace streaming in the web AI Observability console.
- [ ] **Mobile Flow Polish**: Replacing remaining sample/default bindings (such as the direct bottom-tab `SeatPickerScreen` default `serviceId`) and completing registration/profile flows across web and mobile.
- [ ] **Production Deployment & Hardening**: Finalizing cloud environment parity (Dockerfiles, `render.yaml`, Railway/Render deployment pipelines, and live E2E verification).

---

## System Architecture

```text
┌─────────────────────────────────┐       ┌─────────────────────────────────┐
│     React 18 Web Application    │       │     Flutter Mobile Client       │
│  (Vite 5 / Tailwind / Zustand)  │       │   (Dart 3 / flutter_bloc / Dio) │
│  Operators, Managers & Admins   │       │     Passengers & Conductors     │
└────────────────┬────────────────┘       └────────────────┬────────────────┘
                 │                                         │
                 │ HTTP / REST / JWT Bearer Auth           │
                 ▼                                         ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core 8 Web API                               │
│                   Authoritative Application Tier                          │
│      Clean Architecture · Business Rules · EF Core 9 · Swagger/OpenAPI    │
└───────────────────────┬───────────────────────────┬───────────────────────┘
                        │                           │
            Npgsql 9    ▼                           ▼  HTTP / REST (Port 8000)
    ┌───────────────────────────┐       ┌───────────────────────────────────┐
    │    PostgreSQL Database    │       │     LangGraph AI Subsystem        │
    │  Authoritative Relational │       │   FastAPI / Python 3.11           │
    │  Store · EF Migrations    │       │   Conditional 5-Node Graph        │
    └───────────────────────────┘       │   10 Allow-Listed Tools (httpx)   │
                                        └─────────────────┬─────────────────┘
                                                          │ HTTPS
                                                          ▼
                                                ┌───────────────────┐
                                                │   Google Gemini   │
                                                │ (gemini-3.5-flash)│
                                                └───────────────────┘
```

### Core Architectural Boundaries
- **Authoritative Backend**: ASP.NET Core Web API enforces all business rules, seat hold expirations, concurrency, and RBAC policies server-side.
- **Authoritative Database**: PostgreSQL is accessed exclusively by the backend via EF Core 9; web, mobile, and AI clients never connect directly to PostgreSQL.
- **Agentic AI Guardrails**: The Python AI service interacts with system data strictly through 10 allow-listed HTTP tools against the backend API. It cannot confirm payments directly or apply high-impact operational changes without Transport Manager approval.

---

## Repository Structure

```text
WayPoint/
├── backend/                  # ASP.NET Core 8 Web API (Clean Architecture + EF Core 9)
│   ├── WayPoint.Domain/      # Domain entities, common base entity & enums
│   ├── WayPoint.Application/ # Service interfaces, feature DTOs & contracts
│   ├── WayPoint.Infrastructure/ # EF Core DbContext, migrations, seeder & service impls
│   ├── WayPoint.API/         # 22 REST controllers, JWT auth, Swagger & CLI seeder
│   └── WayPoint.Tests/       # xUnit unit & integration tests
│
├── web/                      # React 18 Web Dashboard (Vite 5 / Tailwind / Zustand / TanStack Query)
│   ├── src/features/journey/ # Route catalog, service scheduler & tourist corridors
│   ├── src/features/fleet/   # Fleet matrix, seat layout designer & driver rostering
│   ├── src/features/bookings/# Operator dashboard & booking manifest monitor
│   ├── src/features/disruptions/ # Disruption intake, manager approvals, alerts & AI traces
│   ├── src/features/admin/   # User administration & role management
│   └── src/pages/            # Login, Overview, FleetReviewsDashboard & 404 pages
│
├── mobile/                   # Flutter Cross-Platform Mobile Client (Dart 3 / flutter_bloc)
│   ├── lib/features/auth/    # Sign-in / registration & AuthCubit
│   ├── lib/features/onboarding/ # First-run onboarding flow
│   ├── lib/features/navigation/ # AuthGate, splash & Passenger/Conductor shells
│   ├── lib/features/journey/ # Journey search, comparison, filters & AI prompt/insights
│   ├── lib/features/booking/ # Seat picker, sandbox checkout, QR wallet & booking history
│   ├── lib/features/fleet/   # Conductor scanner, manifest, reviews & shared BLoCs
│   ├── lib/features/disruption/ # Passenger disruption alerts & rebooking consent
│   ├── lib/features/settings/# Passenger settings & preferences
│   └── test/                 # Flutter unit, BLoC & widget tests
│
├── ai/                       # Agentic AI Microservice (FastAPI / LangGraph / Gemini)
│   ├── agents/               # StateGraph & 5 nodes (planner, journey, resource, booking, safety)
│   ├── tools/                # 10 allow-listed tool wrappers, HTTP client & registry
│   ├── guardrails/           # Input sanitizer, output validator & safe-failure wrappers
│   ├── persistence/          # WorkflowLogger for backend audit persistence
│   ├── prompts/              # System & planner prompts
│   ├── schemas/              # Pydantic request/response, tool & workflow schemas
│   └── tests/                # Pytest agent, guardrail, tool & persistence tests
│
├── database/                 # PostgreSQL baseline schema export & SQL reference seed data
├── tests/                    # Cross-cutting e2e, integration, performance & security suites
├── tools/                    # Monorepo helper scripts (run_all_tests.py, PDF generator)
├── docs/                     # Architecture specs, ADRs, requirements, plans & QA reports
├── .github/                  # CI/CD GitHub Actions workflows, CODEOWNERS & templates
├── Dockerfile                # Production container definition for the .NET 8 Web API
├── render.yaml               # Static site configuration template for the React web app
├── .env.example              # Environment variable template
└── AGENTS.md                 # Repository engineering & AI safety rules
```

---

## Team Ownership

| Component | Owner | Primary Focus |
| :--- | :--- | :--- |
| **Journey Planning & Route Catalogue** | **Sethum** (Student 1) | Routes, stops, timetables, tourist corridors, candidate search & Journey Analysis Agent |
| **Fleet, Seat & Resource Feasibility** | **Nuhadh** (Student 2) | Bus inventory, 2D seat layout designer, driver rostering, reviews & Resource Feasibility Agent |
| **Booking, Ticketing & Passenger Options** | **Mithila** (Student 3) | Seat holds, sandbox payment checkout, QR tickets, refunds & Booking & Policy Agent |
| **Disruption, Rebooking & Approval** | **Dineth** (Student 4) | Disruption logging, rebooking proposals, manager approvals, alerts & Validation & Safety Agent |

---

## Seeded Local Test Accounts

Running the backend seeder (`--seed`) creates the four core `UserRoleType` roles (`Admin`, `TransportManager`, `Operator`, `Passenger`) and default test accounts, all initialized with the password **`Password123!`**:

| Role | Email | Password | Intended Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@waypoint.lk` | `Password123!` | Full administrative control & user governance |
| **TransportManager** | `manager@waypoint.lk` | `Password123!` | Disruption approval workbench & AI workflow oversight |
| **Operator** | `operator@waypoint.lk` | `Password123!` | Fleet, seat layouts, driver rosters, routes & disruptions |
| **Passenger** | `passenger@waypoint.lk` | `Password123!` | Journey search, seat selection, checkout & ticket wallet |

---

## Local Development Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/) (local instance or managed `DATABASE_URL`)
- [Node.js](https://nodejs.org/) (v18+ / CI uses v20) & npm
- [Flutter SDK](https://flutter.dev/) (Dart `>=3.0.0 <4.0.0`)
- [Python](https://www.python.org/) (3.10+ / CI uses 3.11)

### 1. Configure Environment
```bash
git clone https://github.com/NuhadhMohomed/WayPoint.git
cd WayPoint
cp .env.example .env
```
Update `.env` with your local PostgreSQL `DATABASE_URL`, `JWT_SECRET`, and `GEMINI_API_KEY`.

### 2. Backend API & Database (`backend/`)
```bash
dotnet restore backend/WayPoint.sln
dotnet run --project backend/WayPoint.API -- --seed
dotnet run --project backend/WayPoint.API
```
- **API Base**: `http://localhost:5010/api/v1`
- **Swagger UI**: `http://localhost:5010/swagger`
- **Health Check**: `http://localhost:5010/health`

### 3. React Web Dashboard (`web/`)
```bash
cd web
npm install
npm run dev
```
- **Web Dev Server**: `http://localhost:5173`

### 4. Flutter Mobile App (`mobile/`)
```bash
cd mobile
flutter pub get
flutter run
```
By default, Android emulators point to `http://10.0.2.2:5010/api/v1` (override via `--dart-define=API_BASE_URL=...`).

### 5. Agentic AI Microservice (`ai/`)
```bash
cd ai
python -m venv venv
# Windows: .\venv\Scripts\Activate.ps1
# Linux/macOS: source venv/bin/activate
pip install -r requirements.txt
python main.py
```
- **AI Service**: `http://localhost:8000`
- **FastAPI Docs**: `http://localhost:8000/docs`

*(Optional)* Run `uvicorn main:app --reload --port 8000` inside `ai/` for hot-reload during development.

---

## Running Tests

Each subsystem has its own test suite (see [tests/README.md](tests/README.md) for cross-service suites and `tools/run_all_tests.py`):

```bash
# Backend (.NET 8 xUnit)
dotnet test backend/WayPoint.sln --configuration Release

# Web (Vitest + React Testing Library)
cd web && npm test

# Mobile (Flutter Test)
cd mobile && flutter test

# AI Microservice (Pytest)
cd ai && pytest tests/ -v
```

---

## Documentation & Subsystem READMEs

- **Subsystem Guides**: [Backend](backend/README.md) · [Web](web/README.md) · [Mobile](mobile/README.md) · [AI](ai/README.md) · [Database](database/README.md) · [Tests](tests/README.md)
- **Architecture & ADRs**: [docs/architecture/](docs/architecture/) · [docs/adr/](docs/adr/)
- **Requirements & Testing Docs**: [docs/requirements/](docs/requirements/) · [docs/testing/](docs/testing/)
- **Deployment Notes (WIP)**: [Dockerfile](Dockerfile), [ai/Dockerfile](ai/Dockerfile), [render.yaml](render.yaml), and [docs/deployment/](docs/deployment/)
