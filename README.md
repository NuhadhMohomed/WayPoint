# WayPoint — Headless AI-Powered Intercity Transit Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Python 3.11](https://img.shields.io/badge/Python-3.11-3776AB.svg)](https://www.python.org/)
[![PostgreSQL 18](https://img.shields.io/badge/PostgreSQL-18.6-336791.svg)](https://www.postgresql.org/)
[![Railway](https://img.shields.io/badge/Railway-Hosted-0B0D0E.svg)](https://railway.com/)
[![OpenAPI / Swagger](https://img.shields.io/badge/OpenAPI-3.0-85EA2D.svg)](http://localhost:5010/swagger)

**WayPoint** is an enterprise headless multi-tier intercity journey planning and bus operations platform designed for Sri Lankan transit networks. It provides real-time route catalogues, multimodal journey optimization, interactive seat inventory management with concurrency protection, cryptographic QR ticketing verification, and autonomous AI-assisted disruption mitigation.

Operating as a pure **headless API-first platform** ([ADR-006](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-006-headless-architecture.md)), WayPoint exposes authoritative RESTful endpoints and OpenAPI/Swagger specifications consumed by external transit systems, dispatchers, partner gateways, and autonomous agents.

Built for **SE3090 Assignment 1**.

---

## 📁 Repository Structure

```text
WayPoint/
├── backend/                  # ASP.NET Core 8 Web API (authoritative backend & EF Core 9)
│   ├── WayPoint.Domain/      # Pure domain models, enums & business invariants
│   ├── WayPoint.Application/ # CQRS/Use cases, DTOs & deterministic validation
│   ├── WayPoint.Infrastructure/ # EF Core 9 DbContext, Npgsql 9, PostgreSQL 18 Direct SSL
│   ├── WayPoint.API/         # REST Controllers, JWT Middleware, Swagger & CLI seeder
│   └── WayPoint.Tests/       # 77+ xUnit unit and integration tests
│
├── ai/                       # Agentic AI subsystem (LangGraph / FastAPI autonomous workflows)
│   ├── agents/               # Specialized transit agents (Planner, Analysis, Resource, Safety)
│   ├── guardrails/           # Deterministic validation and safety filters
│   ├── persistence/          # Workflow checkpointing and state tracking
│   └── tests/                # Pytest multi-agent workflow test suite
│
├── database/                 # EF Core migrations, SQL exports & schema documentation
│   ├── schema_baseline.sql   # Comprehensive PostgreSQL schema export
│   └── seed/                 # Seed scripts for Sri Lankan transit corridors & bus fleet
│
├── tests/                    # Unit, integration, E2E & performance test suites
│   ├── integration/          # Cross-service integration tests
│   └── performance/          # High-concurrency load test scenarios
│
├── docs/                     # Comprehensive project documentation
│   ├── project/              # Individual student implementation guides & responsibilities
│   ├── architecture/         # System architecture, API specs, database ERD & security
│   ├── deployment/           # Railway PostgreSQL 18 deployment guide & cloud specs
│   ├── requirements/         # Functional specs, business rules & traceability matrix
│   ├── testing/              # Test strategy & test cases
│   └── adr/                  # Architectural Decision Records (including Headless Architecture)
│
├── .env.example              # Centralized environment variable template
├── AGENTS.md                 # Guidelines & architecture rules for AI coding assistants
└── README.md                 # Main project overview
```

---

## 👥 Student Functional Components

The system is partitioned into four balanced, non-overlapping functional components operating on the authoritative backend and AI subsystem:

| Student | Functional Area | Domain & Application Services | Authoritative Endpoints | AI & Business Logic Focus |
| :--- | :--- | :--- | :--- | :--- |
| **Sethum** (Student 1) | **Journey Planning & Catalogue** | Route, stop & timetable management, connecting transfer window logic (`BR-TRANSFER-001`) | `GET/POST /api/v1/routes`<br>`GET /api/v1/services`<br>`POST /api/v1/journeys/search` | Journey recommendation agent & candidate scoring engine |
| **Nuhadh** (Student 2) | **Fleet & Bus Operations** | Bus inventory, dynamic seat layout generation, maintenance & driver rostering | `GET/POST /api/v1/buses`<br>`GET /api/v1/seat-layouts`<br>`GET/POST /api/v1/drivers` | Seat matrix layout engine & resource feasibility evaluation |
| **Mithila** (Student 3) | **Booking & Ticketing** | 10-minute hold concurrency (`FR-BOOKING-001`), tiered refunds (`BR-REFUND-001`), HMAC QR tokens | `POST /api/v1/bookings/hold`<br>`POST /api/v1/checkout`<br>`GET /api/v1/tickets/{id}` | Atomic transactions & payment sandbox verification |
| **Dineth** (Student 4) | **Disruption & AI Mitigation** | Incident intake, blast radius impact analysis, Transport Manager approval gate (`BR-APPROVAL-001`) | `POST /api/v1/disruptions`<br>`POST /api/v1/approvals/{id}`<br>`POST /api/v1/alerts/broadcast` | Autonomous multi-agent rebooking with LangGraph |

Detailed student guides are located in [`docs/project/`](docs/project/):
- [Student 1 Implementation Guide](docs/project/guide-student1-sethum-journey.md)
- [Student 2 Implementation Guide](docs/project/guide-student2-nuhadh-fleet.md)
- [Student 3 Implementation Guide](docs/project/guide-student3-mithila-booking.md)
- [Student 4 Implementation Guide](docs/project/guide-student4-dineth-disruption.md)

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python](https://www.python.org/) (v3.10+)
- [Git](https://git-scm.com/)

---

### Step 1: Clone & Configure Environment

```bash
git clone https://github.com/NuhadhMohomed/WayPoint.git
cd WayPoint
cp .env.example .env
```

Open `.env` and configure your database and API settings:
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5010
API_BASE_URL=http://localhost:5010/api/v1
DATABASE_URL=postgresql://<username>:<password>@<host>:<port>/railway
JWT_SECRET=WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090
AI_SERVICE_URL=http://localhost:8000
```

---

### Step 2: Backend Setup & Seed Database

```bash
# Restore solution packages
dotnet restore backend/WayPoint.sln

# Apply EF Core migrations and seed initial transit records
dotnet run --project backend/WayPoint.API -- --seed

# Run the API server
dotnet run --project backend/WayPoint.API
```
- API Base: `http://localhost:5010/api/v1`
- Swagger UI: `http://localhost:5010/swagger`

---

### Step 3: Run Backend Automated Tests

```bash
dotnet test backend/WayPoint.sln --configuration Release
```
Currently 77+ unit, integration, concurrency, and security tests pass with 0 failures.

---

### Step 4: AI Multi-Agent Subsystem Setup

In a new terminal window:
```bash
cd ai
python -m venv venv
source venv/bin/activate  # Or on Windows: .\venv\Scripts\Activate.ps1
pip install -r requirements.txt
pytest tests/ -v
python main.py
```
- AI Service Base: `http://localhost:8000`

---

## ☁️ Cloud Database (Railway PostgreSQL 18)

WayPoint's relational database is hosted on **Railway** (PostgreSQL 18.6):
- **Direct SSL & ALPN**: Fully configured via **Npgsql 9** with `SslNegotiation = SslNegotiation.Direct` and ALPN `postgresql`.
- **Authoritative Schemas**: EF Core migrations in `backend/WayPoint.Infrastructure/Data/Migrations/`.
- Full deployment instructions: [`docs/deployment/database-railway-guide.md`](docs/deployment/database-railway-guide.md).

---

## 🛡️ Architecture & Security Rules

- **Pure Headless Operation**: The ASP.NET Core Web API is the authoritative headless layer; all external clients, partner systems, and AI workflows communicate exclusively through authenticated REST endpoints.
- **Zero Direct Database Access**: External consumers and AI agents are strictly prohibited from connecting directly to PostgreSQL.
- **Server-Side Validation**: All domain business logic, seat locks, and refund rules are strictly validated server-side.
- **Deterministic AI Validation**: AI outputs must be validated by backend rules before any database state modification.
- **No Committed Secrets**: Credentials remain exclusively in `.env` and are strictly git-ignored.
- **Human Manager in the Loop**: High-impact operational actions require manager approval (`BR-APPROVAL-001`).

---

## 📄 Documentation Directory

- **Architecture**: [`docs/architecture/`](docs/architecture/)
  - [System Architecture](docs/architecture/system-architecture.md)
  - [API Design Specification](docs/architecture/api-design.md)
  - [Database & ERD Specification](docs/architecture/database-design.md)
  - [Security Architecture](docs/architecture/security-architecture.md)
  - [Deployment Architecture](docs/architecture/deployment-architecture.md)
- **Architectural Decision Records**: [`docs/adr/`](docs/adr/)
  - [ADR-001: React State Management (Superseded)](docs/adr/ADR-001-react-state-management.md)
  - [ADR-002: Flutter State Management (Superseded)](docs/adr/ADR-002-flutter-state-management.md)
  - [ADR-003: AI Orchestration](docs/adr/ADR-003-ai-orchestration.md)
  - [ADR-004: AI Workflow Persistence](docs/adr/ADR-004-ai-workflow-persistence.md)
  - [ADR-005: Cloud Deployment](docs/adr/ADR-005-cloud-deployment.md)
  - [ADR-006: Headless Architecture](docs/adr/ADR-006-headless-architecture.md)
- **Deployment**: [`docs/deployment/`](docs/deployment/)
  - [Railway Database Guide](docs/deployment/database-railway-guide.md)
- **Requirements & Specifications**: [`docs/requirements/`](docs/requirements/)
  - [Software Requirements Specification](docs/requirements/requirements.md)
  - [Business Rules](docs/requirements/business-rules.md)
  - [Traceability Matrix](docs/requirements/traceability-matrix.md)
- **Testing**: [`docs/testing/`](docs/testing/)
  - [Comprehensive Test Strategy](docs/testing/test-strategy.md)
- **Module Documentation**:
  - [Backend README](backend/README.md)
  - [AI Subsystem README](ai/README.md)
  - [Database README](database/README.md)

---

## 🤝 Contributing & Pull Requests

1. Always branch off `main` using your assigned feature branch name (e.g. `feature/fleet-management`).
2. Adhere to formatting rules defined in `.editorconfig`.
3. Submit pull requests using the template in `.github/pull_request_template.md`.
