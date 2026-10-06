# WayPoint — Integrated Full-Stack & Agentic AI Transit Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![React 18](https://img.shields.io/badge/React-18.3-61DAFB.svg)](https://react.dev/)
[![Flutter 3.x](https://img.shields.io/badge/Flutter-3.x-02569B.svg)](https://flutter.dev/)
[![Python 3.11](https://img.shields.io/badge/Python-3.11-3776AB.svg)](https://www.python.org/)
[![PostgreSQL 18](https://img.shields.io/badge/PostgreSQL-18.6-336791.svg)](https://www.postgresql.org/)
[![Railway](https://img.shields.io/badge/Railway-Hosted-0B0D0E.svg)](https://railway.com/)
[![Render](https://img.shields.io/badge/Render-Deployed-46E3B7.svg)](https://render.com/)

**WayPoint** is an enterprise-grade, integrated multi-tier transit management and journey planning platform built for Sri Lankan intercity bus networks. It unifies high-concurrency seat reservation, real-time fleet operations, dynamic timetable scheduling, cryptographic QR ticket validation, and autonomous multi-agent AI disruption mitigation into a seamless full-stack architecture.

Developed for **SE3090 — Integrated Full-Stack and Agentic AI Application Development (Assignment 1)**.

---

## 🌐 Live Cloud Deployments

| Component | Platform | Live URL / Artifact |
| :--- | :--- | :--- |
| **React Web Application** | Render | [https://waypoint-web.onrender.com](https://waypoint-web.onrender.com) |
| **ASP.NET Core REST API** | Railway | [https://waypoint-api-production.up.railway.app](https://waypoint-api-production.up.railway.app) |
| **OpenAPI / Swagger UI** | Railway | [https://waypoint-api-production.up.railway.app/swagger](https://waypoint-api-production.up.railway.app/swagger) |
| **Managed Cloud Database** | Railway | PostgreSQL 18.6 with Direct SSL ALPN (`proxy.rlwy.net`) |
| **Flutter Mobile App** | Android | Distributable APK: `mobile/build/app/outputs/flutter-apk/app-release.apk` |

---

## 🏗️ Integrated Architecture Overview

WayPoint strictly adheres to the official SE3090 system design (Figures 1 & 2 of the assignment specification):

```
┌─────────────────────────────────┐       ┌─────────────────────────────────┐
│     React 18 Web Application    │       │     Flutter Mobile Client       │
│  (Vite / Tailwind / Zustand)   │       │   (Dart 3 / flutter_bloc / Dio) │
│  Operators, Managers & Admins   │       │     Passengers & Conductors     │
└────────────────┬────────────────┘       └────────────────┬────────────────┘
                 │                                         │
                 │ HTTPS / REST / JWT Authentication       │
                 ▼                                         ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core 8 Web API                               │
│                   Authoritative Application Tier                          │
│     CQRS · Business Rule Enforcement · Atomic Concurrency · EF Core 9     │
└───────────────────────┬───────────────────────────┬───────────────────────┘
                        │                           │
          Direct SSL    ▼                           ▼  HTTP / REST (Port 8000)
    ┌───────────────────────────┐       ┌───────────────────────────────────┐
    │  PostgreSQL 18 Database   │       │     LangGraph AI Subsystem        │
    │  Authoritative Relational │       │   FastAPI / Python 3.11           │
    │  Store · JSONB Persistence│       │   Autonomous 5-Node Agent Flow    │
    └───────────────────────────┘       │   10 Allow-Listed Tools (httpx)   │
                                        └─────────────────┬─────────────────┘
                                                          │ HTTPS / Prompts
                                                          ▼
                                                ┌───────────────────┐
                                                │ Google Gemini 1.5 │
                                                └───────────────────┘
```

- **Authoritative Backend**: ASP.NET Core Web API enforces all business invariants, seat hold timers, and payment validations server-side.
- **Authoritative Database**: PostgreSQL 18 holds all relational schemas and workflow execution telemetry via EF Core 9.
- **Client Presentation Tiers**: React 18 Web App and Flutter Mobile Client communicate strictly through the backend API.
- **Agentic AI Boundaries**: LangGraph multi-agent pipeline interacts with transit data strictly via **10 allow-listed tools** over HTTP. AI cannot access the database directly, confirmed payments, or apply operational modifications without human manager sign-off.

---

## 📁 Repository Structure

```text
WayPoint/
├── backend/                  # ASP.NET Core 8 Web API (authoritative backend & EF Core 9)
│   ├── WayPoint.Domain/      # Domain entities, value objects, business invariants & enums
│   ├── WayPoint.Application/ # CQRS commands, queries, DTOs & business rule validation
│   ├── WayPoint.Infrastructure/ # EF Core 9 DbContext, Npgsql 9, PostgreSQL 18 Direct SSL
│   ├── WayPoint.API/         # REST Controllers, JWT Middleware, Swagger UI & CLI Seeder
│   └── WayPoint.Tests/       # 77+ xUnit automated unit, integration & concurrency tests
│
├── web/                      # React 18 Web Application (Vite / Tailwind CSS / Zustand)
│   ├── src/features/journey/ # Student 1: Route manager, timetable & corridor pages
│   ├── src/features/fleet/   # Student 2: Bus fleet matrix, seat designer & driver rostering
│   ├── src/features/bookings/# Student 3: Operator dashboard & booking manifest monitor
│   ├── src/features/disruptions/# Student 4: Disruption intake, approval workbench & alerts
│   ├── src/features/admin/   # User administration & role management
│   └── src/**/__tests__/     # Vitest & React Testing Library automated test suite
│
├── mobile/                   # Flutter Mobile Client Application (Dart 3 / flutter_bloc)
│   ├── lib/features/journey/ # Student 1: Journey search & timetable comparison screens
│   ├── lib/features/fleet/   # Student 2: Interactive seat picker & fleet review screens
│   ├── lib/features/booking/ # Student 3: Payment checkout, ticket wallet & history screens
│   ├── lib/features/disruptions/# Student 4: Passenger disruption alerts & conductor scanner
│   └── test/features/        # Flutter widget, BLoC, and flow automated test suites
│
├── ai/                       # Agentic AI Subsystem (LangGraph / FastAPI autonomous workflows)
│   ├── agents/               # 5 specialized agent nodes (Planner, Journey, Resource, Policy, Safety)
│   ├── tools/                # 10 mandatory allow-listed tool definitions & HTTP clients
│   ├── guardrails/           # Deterministic validators (bus capacity, driver rest, fare arithmetic)
│   └── tests/                # Pytest multi-agent workflow test suite & evaluation benchmarks
│
├── database/                 # PostgreSQL schemas, migrations & seed SQL scripts
│   ├── schema_baseline.sql   # Comprehensive PostgreSQL schema export
│   └── seed/                 # Seed scripts for Sri Lankan transit corridors & bus fleet
│
├── tests/                    # Cross-cutting integration, E2E & performance test suites
│   ├── e2e/                  # Closed-loop cross-platform test scenarios (REQ-TEST-05)
│   ├── integration/          # Multi-tier cross-service integration tests
│   └── performance/          # High-concurrency seat hold load tests
│
├── docs/                     # Comprehensive project documentation
│   ├── project/              # Individual student guides, responsibilities & marking matrix
│   ├── architecture/         # High-level architecture, API specs, ERD & security guides
│   ├── deployment/           # Full-stack cloud deployment & Railway PostgreSQL guide
│   ├── requirements/         # SRS, business rules catalog & traceability matrix
│   ├── testing/              # Comprehensive test strategy & QA verification guide
│   ├── ai/                   # AI architecture & evaluation reports for each student agent
│   └── adr/                  # Architectural Decision Records (ADR-001 through ADR-006)
│
├── .env.example              # Centralized environment variable template
├── AGENTS.md                 # Guidelines & architecture rules for AI coding assistants
└── README.md                 # Main project overview and grading documentation
```

---

## 👥 Student Functional Responsibilities & Technical Ownership

In full compliance with the SE3090 marking scheme (70 individual marks per student across 6 technical layers):

| Student & Component | ASP.NET Core & Database (20 M) | React Web Application (10 M) | Flutter Mobile Client (10 M) | Agentic AI Subsystem (12 M) | Testing & Security (18 M) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Sethum** (Student 1)<br/>*Journey Planning & Timetable Catalogue* | `RoutesController`, `TimetableServicesController`, route segments, connecting transfer logic (`BR-TRANSFER-001`) | `RouteManagerPage`<br>`ServiceSchedulerPage`<br>`TouristCorridorsPage` | `JourneySearchScreen`<br>`JourneyComparisonScreen`<br>Stop filters & timeline | **Journey Analysis Agent**<br>Tools: `SearchRoutes`, `GetBoardingPoints`, `CheckTransferFeasibility` | Route unit tests, Vitest route manager, Flutter search tests |
| **Nuhadh** (Student 2)<br/>*Fleet, Dynamic Seats & Operations* | `BusesController`, `SeatLayoutsController`, `DriversController`, seat matrix derivation (`BR-SEAT-001`) | `FleetMatrixBuilderPage`<br>`SeatLayoutDesignerPage`<br>`DriverRosteringPage`<br>`FleetReviewsDashboardPage` | `SeatPickerScreen`<br>`ReviewSubmissionScreen`<br>Seat state color coding | **Resource Feasibility Agent**<br>Tools: `CheckSeatAvailability`<br>Deterministic rest validator | Seat layout generator tests, Vitest designer, Flutter seat picker tests |
| **Mithila** (Student 3)<br/>*Booking, Hold Concurrency & Ticketing* | `BookingsController`, `CheckoutController`, `TicketsController`, 10-min hold lock (`BR-HOLD-001`), refund rules | `OperatorDashboardPage`<br>`BookingManifestMonitorPage`<br>Real-time hold countdown | `PaymentCheckoutScreen`<br>`TicketWalletScreen`<br>`BookingHistoryScreen`<br>HMAC QR rendering | **Booking & Policy Agent**<br>Tools: `CalculateFareDifference`, `SendPassengerNotification` | EF Core concurrency tests, hold expiration tests, Vitest manifest tests |
| **Dineth** (Student 4)<br/>*Disruption Mitigation & AI Safety* | `DisruptionsController`, `ApprovalsController`, `AlertsController`, Manager gate (`BR-APPROVAL-001`) | `DisruptionIntakePage`<br>`ManagerApprovalWorkbenchPage`<br>`AiObservabilityPage`<br>`ServiceAlertBroadcastPage` | `DisruptionAlertScreen`<br>`ConductorScannerScreen`<br>`ConductorManifestScreen`<br>Camera QR scanner | **Validation & Safety Agent**<br>Tools: `CreateRebookingProposal`, `CalculatePassengerImpact`, `RequestManagerApproval` | LangGraph integration tests, Vitest disruption tests, Flutter conductor tests |

### Comprehensive Student Implementation Guides
- 📘 [Student 1 (Sethum) Implementation Guide](docs/project/guide-student1-sethum-journey.md)
- 📗 [Student 2 (Nuhadh) Implementation Guide](docs/project/guide-student2-nuhadh-fleet.md)
- 📙 [Student 3 (Mithila) Implementation Guide](docs/project/guide-student3-mithila-booking.md)
- 📕 [Student 4 (Dineth) Implementation Guide](docs/project/guide-student4-dineth-disruption.md)
- 📊 [Full Team Responsibilities & Matrix](docs/project/team-responsibilities.md)

---

## 🔑 Test Accounts & Role-Based Access Control

The database seeder pre-provisions standard test accounts for evaluating role-based workflows:

| Role | Email Address | Password | Permitted Features & Capabilities |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@waypoint.lk` | `Admin@123` | Full administrative control, user role management, system settings |
| **Transport Manager** | `manager@waypoint.lk` | `Manager@123` | Operational change approval workbench (`BR-APPROVAL-001`), AI observability |
| **Bus Operator** | `operator@waypoint.lk` | `Operator@123` | Fleet management, seat layout designer, driver rosters, disruption intake |
| **Conductor** | `conductor@waypoint.lk` | `Conductor@123` | Mobile passenger manifest, QR camera scanning & boarding validation |
| **Passenger** | `passenger@waypoint.lk` | `Passenger@123` | Journey search, seat selection, checkout, QR ticket wallet, reviews |

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18+) & npm
- [Flutter SDK](https://flutter.dev/) (v3.24+) & Dart 3
- [Python](https://www.python.org/) (v3.11+)
- [Git](https://git-scm.com/)

---

### Step 1: Clone & Configure Environment

```bash
git clone https://github.com/NuhadhMohomed/WayPoint.git
cd WayPoint
cp .env.example .env
```

Configure your local or cloud database credentials inside `.env`:
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5010
API_BASE_URL=http://localhost:5010/api/v1
DATABASE_URL=postgresql://postgres:password@localhost:5432/waypoint
JWT_SECRET=WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090
AI_SERVICE_URL=http://localhost:8000
```

---

### Step 2: Authoritative Backend & Database Setup

```bash
# Restore solution packages
dotnet restore backend/WayPoint.sln

# Apply EF Core migrations and seed initial Sri Lankan transit network
dotnet run --project backend/WayPoint.API -- --seed

# Launch ASP.NET Core API server
dotnet run --project backend/WayPoint.API
```
- **API Base**: `http://localhost:5010/api/v1`
- **Swagger Documentation**: `http://localhost:5010/swagger`

---

### Step 3: React Web Frontend Setup

In a new terminal:
```bash
cd web
npm install
npm run dev
```
- **Web App URL**: `http://localhost:5173`

---

### Step 4: Flutter Mobile Client Setup

In a new terminal:
```bash
cd mobile
flutter pub get
flutter run
```

To compile an Android release APK:
```bash
flutter build apk --release --dart-define=API_BASE_URL=http://localhost:5010/api/v1
```

---

### Step 5: Agentic AI Subsystem Setup

In a new terminal:
```bash
cd ai
python -m venv venv

# Windows:
.\venv\Scripts\Activate.ps1
# Linux/macOS:
source venv/bin/activate

pip install -r requirements.txt
python main.py
```
- **AI Microservice**: `http://localhost:8000`

---

## 🧪 Comprehensive Automated Testing Suites

WayPoint implements a complete automated testing pyramid covering all tiers:

```bash
# 1. Authoritative Backend Unit & Concurrency Tests (.NET 8 xUnit)
dotnet test backend/WayPoint.sln --configuration Release

# 2. React Web Frontend Tests (Vitest & React Testing Library)
cd web && npm test

# 3. Flutter Mobile Client Tests (flutter_test & bloc_test)
cd mobile && flutter test

# 4. Agentic AI Evaluation & Guardrail Benchmarks (pytest)
cd ai && pytest tests/ -v
```

Complete testing specifications, coverage matrices, and E2E closed-loop procedures are detailed in [docs/testing/test-strategy.md](docs/testing/test-strategy.md).

---

## 📑 Architectural Decision Records (ADRs)

Key architectural decisions are recorded in [docs/adr/](docs/adr/):

- [ADR-001: React State Management with Zustand](docs/adr/ADR-001-react-state-management.md) *(Active)*
- [ADR-002: Flutter State Management with flutter_bloc](docs/adr/ADR-002-flutter-state-management.md) *(Active)*
- [ADR-003: AI Orchestration with LangGraph Multi-Agent Workflows](docs/adr/ADR-003-ai-orchestration.md) *(Active)*
- [ADR-004: AI Workflow & Tool Audit Persistence in PostgreSQL](docs/adr/ADR-004-ai-workflow-persistence.md) *(Active)*
- [ADR-005: Full-Stack Cloud Deployment Topology](docs/adr/ADR-005-cloud-deployment.md) *(Active)*
- [ADR-006: Headless Architecture](docs/adr/ADR-006-headless-architecture.md) *(Superseded / Deprecated by SE3090 Integrated Full-Stack Standard)*

---

## 📊 SE3090 Marking Rubric Alignment (100 Marks)

| Category | Component / Evaluation Area | Allocation | Verification Reference in WayPoint |
| :--- | :--- | :--- | :--- |
| **Group (30 Marks)** | **Component Design & Business Logic** | 10 Marks | Modular domain design, 10-minute hold concurrency, tiered refunds |
| | **Integrated Architecture & AI Orchestration** | 10 Marks | Multi-tier stack (Fig. 1 & 2), 5-node LangGraph flow, allow-listed tools |
| | **Documentation & Deployment** | 10 Marks | Railway backend, Render web app, Android APK, complete `docs/` suite |
| **Individual (70 Marks)** | **ASP.NET Core REST API** | 10 Marks | Authoritative controllers, CQRS handlers, validation pipelines per student |
| | **PostgreSQL & Data Modeling** | 10 Marks | Normalized schemas, indexes, foreign keys, EF Core migrations per student |
| | **React Web Application** | 10 Marks | Dedicated pages in `web/src/features/`, responsive UI, error/empty states |
| | **Flutter Mobile Application** | 10 Marks | Dedicated screens in `mobile/lib/features/`, BLoC states, responsive layout |
| | **Individual Agentic AI Component** | 12 Marks | Specialized agent node, allow-listed tools, deterministic validation |
| | **API Integration & Security** | 10 Marks | JWT authentication, role guards, token secure storage, safe error states |
| | **Testing, CI & Git Workflow** | 8 Marks | Unit/widget/Vitest tests per student, clean Git commits, GitHub PRs |

---

## 📚 Complete Repository Documentation

- **Architecture & System Design**:
  - [System Architecture Specification](docs/architecture/system-architecture.md)
  - [REST API Specification](docs/architecture/api-design.md)
  - [Database & ERD Specification](docs/architecture/database-design.md)
  - [Security & Authentication Architecture](docs/architecture/security-architecture.md)
  - [Deployment Topology & Infrastructure](docs/architecture/deployment-architecture.md)
- **Deployment Guides**:
  - [Full-Stack Cloud Deployment Guide](docs/deployment/full-stack-deployment-guide.md)
  - [Railway Managed PostgreSQL 18 Guide](docs/deployment/database-railway-guide.md)
- **Requirements & Business Rules**:
  - [Software Requirements Specification](docs/requirements/requirements.md)
  - [Business Rules Catalog](docs/requirements/business-rules.md)
  - [Requirements Traceability Matrix](docs/requirements/traceability-matrix.md)
  - [Use Case Specifications](docs/requirements/use-cases.md)
- **Agentic AI Architecture Reports**:
  - [Student 1: Journey Analysis Agent](docs/ai/journey-analysis-agent.md)
  - [Student 2: Resource Feasibility Agent](docs/ai/resource-feasibility-agent.md)
  - [Student 3: Booking & Policy Agent](docs/ai/booking-policy-agent.md)
  - [Student 4: Validation & Safety Agent](docs/ai/validation-safety-agent.md)
- **Testing & Quality Assurance**:
  - [Comprehensive Test Strategy & QA Guide](docs/testing/test-strategy.md)
