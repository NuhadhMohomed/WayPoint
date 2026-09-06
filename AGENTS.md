# AGENTS.md

Welcome to the **WayPoint** repository. This document provides directives, guidelines, and rules for AI assistant agents working on this project.

## Repository Overview

WayPoint is structured as a modular multi-tier application:

- **`backend/`**: ASP.NET Core Web API (authoritative application layer).
- **`web/`**: React web application user interface.
- **`mobile/`**: Flutter cross-platform mobile client application.
- **`ai/`**: AI/ML models, prompts, agent workflows, and integration tools.
- **`database/`**: PostgreSQL database migrations (EF Core), schemas, and seed scripts.
- **`tests/`**: End-to-end (E2E), unit, integration, and performance test suites.
- **`docs/`**: Comprehensive project documentation (architecture, ADRs, design, testing, deployment).
- **`.github/`**: CI/CD workflows and GitHub issue/PR templates.

## Architecture Rules

- **ASP.NET Core Web API** is the authoritative application layer.
- **PostgreSQL** is the authoritative relational database.
- **React** communicates with the backend only through the ASP.NET Core API.
- **Flutter** communicates with the backend only through the ASP.NET Core API.
- Clients must **never** connect directly to PostgreSQL.
- External services must be accessed through the backend.

## Business Logic Rules

- Important business rules must be enforced server-side.
- Never trust client-side validation alone.
- Database transactions must protect seat booking operations.
- EF Core migrations must be used for database changes.

## Agentic AI Rules

- AI agents must not directly access PostgreSQL.
- AI agents must only use allow-listed tools.
- AI outputs must use structured schemas.
- AI recommendations must undergo deterministic backend validation.
- AI cannot directly confirm payment.
- AI cannot directly modify operational records.
- High-impact operational changes require manager approval.
- AI failures must result in safe failure.

## Security Rules

- Never commit secrets.
- Use environment variables (`.env.example` as reference).
- Never hard-code API keys.
- Use JWT authentication.
- Use role-based authorization.

## Development & Code Quality Rules

- Every feature requires tests.
- Database changes require migrations.
- API changes must update Swagger/OpenAPI.
- UI changes should include appropriate validation and error states.
- Do not introduce major frameworks without an Architectural Decision Record (ADR) in `docs/adr/`.
- Respect module boundaries and follow formatting rules defined in `.editorconfig`.
