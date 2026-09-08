# WayPoint — AI-Powered Intercity Transit Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![React 18](https://img.shields.io/badge/React-18-61DAFB.svg)](https://react.dev/)
[![Flutter 3](https://img.shields.io/badge/Flutter-3.x-02569B.svg)](https://flutter.dev/)
[![PostgreSQL 18](https://img.shields.io/badge/PostgreSQL-18.6-336791.svg)](https://www.postgresql.org/)
[![Railway](https://img.shields.io/badge/Railway-Hosted-0B0D0E.svg)](https://railway.com/)

**WayPoint** is an enterprise multi-tier intercity journey planning and bus operations platform designed for Sri Lankan transit networks. It provides real-time route catalogues, multimodal journey optimization, interactive seat inventory management, QR ticketing, and autonomous AI-assisted disruption mitigation.

Built for **SE3090 Assignment 1**.

---

## 📁 Repository Structure

```text
WayPoint/
├── backend/                  # ASP.NET Core 8 Web API (authoritative backend & EF Core 9)
│   ├── WayPoint.Domain/      # Pure domain models, enums & business invariants
│   ├── WayPoint.Application/ # CQRS/Use cases, interfaces & deterministic validation
│   ├── WayPoint.Infrastructure/ # EF Core 9 DbContext, Npgsql 9, PostgreSQL 18 Direct SSL
│   └── WayPoint.API/         # REST Controllers, JWT Middleware, Swagger & CLI seeder
│
├── web/                      # React 18 + Vite + Tailwind CSS (Operator & Manager Workspace)
│   └── src/components/ui/    # Google Stitch Design System primitives (Button, Card, TransitBadge)
│
├── mobile/                   # Flutter 3.x cross-platform app (Passenger Mobile Client)
│   └── lib/core/widgets/     # Reusable mobile design primitives (WayPointButton, WayPointCard)
│
├── ai/                       # Agentic AI subsystem (LangGraph / FastAPI autonomous workflows)
│
├── database/                 # EF Core migrations, SQL exports & schema documentation
│
├── tests/                    # Unit, integration, E2E & performance test suites
│
├── docs/                     # Comprehensive project documentation
│   ├── project/              # Individual student implementation guides & responsibilities
│   ├── design/               # Google Stitch Design System tokens (DESIGN.md) & Screen IDs
│   ├── architecture/         # System architecture, API specs, database ERD & security
│   ├── deployment/           # Railway PostgreSQL 18 deployment guide & cloud specs
│   ├── requirements/         # Functional specs & user stories
│   ├── testing/              # Test strategy & test cases
│   └── adr/                  # Architectural Decision Records
│
├── .env.example              # Centralized environment variable template
├── AGENTS.md                 # Guidelines & architecture rules for AI coding assistants
└── README.md                 # Main project overview
```

---

## 👥 Student Functional Components

The system is partitioned into four balanced, non-overlapping functional components:

| Student | Functional Area | Web Responsibilities | Mobile Responsibilities | AI / Backend Focus |
| :--- | :--- | :--- | :--- | :--- |
| **Sethum** (Student 1) | **Journey Planning & Catalogue** | Route & timetable administration (`WEB-03`) | Corridors, dates & filter sheet (`MOB-01`, `MOB-02`, `MOB-03`) | Journey recommendation agent |
| **Nuhadh** (Student 2) | **Fleet & Bus Operations** | Bus inventory & seat layout designer (`WEB-01`, `WEB-02`) | Operator bus inspection views | Seat matrix engine & bus status |
| **Mithila** (Student 3) | **Booking & Ticketing** | Booking verification & manifest (`WEB-05`) | Seat selection, checkout & ticket wallet (`MOB-04`, `MOB-05`, `MOB-06`) | Atomic transactions & QR tokens |
| **Dineth** (Student 4) | **Disruption & AI Mitigation** | Incident management & AI approvals (`WEB-04`) | Disruption alert banner & sheet (`MOB-07`) | Autonomous rebooking agent |

Detailed student guides are located in [`docs/project/`](docs/project/):
- [Student 1 Implementation Guide](docs/project/guide-student1-sethum-journey.md)
- [Student 2 Implementation Guide](docs/project/guide-student2-nuhadh-fleet.md)
- [Student 3 Implementation Guide](docs/project/guide-student3-mithila-booking.md)
- [Student 4 Implementation Guide](docs/project/guide-student4-dineth-disruption.md)

---

## 🎨 Design System Authority (`docs/design/DESIGN.md`)

All Web and Flutter user interfaces strictly adhere to [`docs/design/DESIGN.md`](docs/design/DESIGN.md) synchronized with **Google Stitch Design System** (`assets/0a9e5af03d7d4795a3ce2e1cd7f5d6f9`):

- **Primary / Lanka Blue**: `#0056D2`
- **Accent / Sunset Amber**: `#FEB300`
- **Operational / Jungle Green**: `#005312`
- **Critical / Crimson**: `#BA1A1A`
- **Typography Pairing**: **Plus Jakarta Sans** (headings) + **Inter** (body copy & tabular timetables)
- **Pre-Designed Screens**: Reference all screen IDs in [`docs/design/stitch-screens-index.md`](docs/design/stitch-screens-index.md).

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18+ LTS)
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (v3.x)
- [Python](https://www.python.org/) (v3.10+)
- [Git](https://git-scm.com/)

---

### Step 1: Clone & Configure Environment

```bash
git clone https://github.com/NuhadhMohomed/WayPoint.git
cd WayPoint
cp .env.example .env
```

Open `.env` and configure your database and port settings:
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5010
API_BASE_URL=http://localhost:5010/api/v1
DATABASE_URL=postgresql://<username>:<password>@<host>:<port>/railway
JWT_SECRET=WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090
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

### Step 3: Web Frontend Setup

In a new terminal window:
```bash
cd web
npm install
npm run dev
```
Open `http://localhost:5173` in your browser.

---

### Step 4: Mobile App Setup

In a new terminal window:
```bash
cd mobile
flutter pub get
flutter run
```

---

## ☁️ Cloud Database (Railway PostgreSQL 18)

WayPoint's relational database is hosted on **Railway** (PostgreSQL 18.6):
- **Direct SSL & ALPN**: Fully configured via **Npgsql 9** with `SslNegotiation = SslNegotiation.Direct` and ALPN `postgresql`.
- **Authoritative Schemas**: EF Core migrations in `backend/WayPoint.Infrastructure/Data/Migrations/`.
- Full deployment instructions: [`docs/deployment/database-railway-guide.md`](docs/deployment/database-railway-guide.md).

---

## 🛡️ Architecture & Security Rules

- **Zero Direct Database Access**: Web, Flutter, and AI modules communicate **only** through the ASP.NET Core Web API.
- **Server-Side Validation**: All business logic and seat locks are strictly validated server-side.
- **Deterministic AI Validation**: AI outputs must be validated by backend rules before any database state modification.
- **No Committed Secrets**: Credentials remain exclusively in `.env` and are strictly git-ignored.
- **Human Manager in the Loop**: High-impact operational actions require manager approval (`US-AI-002`).

---

## 📄 Documentation Directory

- **Architecture**: [`docs/architecture/`](docs/architecture/)
  - [System Architecture](docs/architecture/system-architecture.md)
  - [API Design Specification](docs/architecture/api-design.md)
  - [Database & ERD Specification](docs/architecture/database-design.md)
  - [Security Architecture](docs/architecture/security-architecture.md)
  - [Deployment Architecture](docs/architecture/deployment-architecture.md)
- **Design System**: [`docs/design/`](docs/design/)
  - [Design System Specification (DESIGN.md)](docs/design/DESIGN.md)
  - [Stitch Screens Index](docs/design/stitch-screens-index.md)
- **Deployment**: [`docs/deployment/`](docs/deployment/)
  - [Railway Database Guide](docs/deployment/database-railway-guide.md)
- **Module Documentation**:
  - [Backend README](backend/README.md)
  - [Web Frontend README](web/README.md)
  - [Mobile App README](mobile/README.md)
  - [AI Subsystem README](ai/README.md)
  - [Database README](database/README.md)

---

## 🤝 Contributing & Pull Requests

1. Always branch off `main` using your assigned feature branch name (e.g. `feature/fleet-management`).
2. Adhere to formatting rules defined in `.editorconfig`.
3. Submit pull requests using the template in `.github/pull_request_template.md`.
