# AGENTS.md

Welcome to the **WayPoint** repository. This document provides directives, guidelines, and context for AI assistant agents working on this project.

## Repository Overview

WayPoint is structured as a modular multi-tier application:

- **`backend/`**: Server-side APIs, application services, and business logic.
- **`web/`**: Web application user interface (frontend).
- **`mobile/`**: Cross-platform or native mobile client applications.
- **`ai/`**: AI/ML models, prompts, agent workflows, and data processing scripts.
- **`database/`**: Database migrations, schemas, seed scripts, and ORM configs.
- **`tests/`**: End-to-end (E2E), integration, and performance test suites.
- **`docs/`**: Comprehensive project documentation (architecture, ADRs, design, testing, deployment).
- **`.github/`**: CI/CD workflows and GitHub issue/PR templates.

## Agent Guidelines

1. **Modular Consistency**: Respect boundaries between backend, web, mobile, and AI modules.
2. **Documentation**: Maintain architectural records in `docs/adr/` when making major structural decisions.
3. **Environment Isolation**: Never commit sensitive secrets or keys. Use `.env.example` as reference.
4. **Code Quality**: Follow formatting rules defined in `.editorconfig` and lint rules per project.
