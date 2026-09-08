# WayPoint

A comprehensive multi-tier application platform powering web, mobile, backend, and AI integration.

## 📁 Repository Structure

```text
WayPoint/
├── backend/                  # ASP.NET Core Web API (authoritative backend)
├── web/                      # React web application (Operator & Manager Workspace)
├── mobile/                   # Flutter cross-platform mobile client (Passenger App)
├── ai/                       # AI/ML models, services, and agent workflows
├── database/                 # PostgreSQL schemas, migrations, and seed scripts
├── tests/                    # Integration, E2E, and cross-cutting test suites
│
├── docs/                     # Project documentation
│   ├── project/              # Project management & roadmap
│   ├── requirements/         # Product requirements & user stories
│   ├── architecture/         # System architecture & diagrams
│   ├── design/               # UI/UX design specifications & assets
│   ├── ai/                   # AI module specs and prompt engineering
│   ├── testing/              # Test strategy and execution plans
│   ├── deployment/           # Infrastructure & deployment guides
│   ├── adr/                  # Architectural Decision Records (ADRs)
│   └── reference/            # Assignment specification & project proposal
│
├── .github/                  # GitHub configurations
│   ├── workflows/            # CI/CD workflows
│   ├── ISSUE_TEMPLATE/       # Issue templates
│   └── pull_request_template.md # PR description template
│
├── .gitignore                # Git ignore configuration
├── .editorconfig             # Cross-editor formatting rules
├── .env.example              # Example environment variables
├── AGENTS.md                 # Guidelines for AI coding agents
└── README.md                 # Main project overview
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (v15+)
- [Node.js](https://nodejs.org/) (v18+ LTS, for React web app)
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (v3.x, for mobile app)
- [Python](https://www.python.org/) (v3.10+, for Agentic AI microservice in `ai/`)
- [Git](https://git-scm.com/)

### Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/NuhadhMohomed/WayPoint.git
   cd WayPoint
   ```

2. **Configure Environment Variables:**
   ```bash
   cp .env.example .env
   ```

3. **Explore Modules:**
   Check subdirectories (`backend/`, `web/`, `mobile/`, `ai/`) for module-specific README files and setup steps.

## 📄 Documentation

For detailed architectural diagrams, design guidelines, and deployment procedures, explore the [`docs/`](docs/) directory.

## 🤝 Contributing

Please review `.github/pull_request_template.md` before submitting pull requests.

