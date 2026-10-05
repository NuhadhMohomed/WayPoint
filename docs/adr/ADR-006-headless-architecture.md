# ADR-006: Transition to Headless API-First Architecture

## Title
ADR-006: Decommissioning of Frontend UI Layers and Adoption of Headless API-First Architecture

## Status
`Accepted` (Supersedes [ADR-001](ADR-001-react-state-management.md) and [ADR-002](ADR-002-flutter-state-management.md))

---

## Context
WayPoint was originally envisioned with three client tiers:
1. A React Single Page Application (`web/`) for operators and transport managers.
2. A Flutter cross-platform mobile application (`mobile/`) for passengers and conductors.
3. An ASP.NET Core Web API with PostgreSQL and Python AI Multi-Agent subsystems.

As part of the system architectural refinement:
- The system core value proposition resides in the robust ASP.NET Core business domain, high-concurrency seat reservation locking, multi-agent AI disruption triage, and strict transactional consistency.
- Maintaining separate React web and Flutter mobile visual rendering layers created superficial coupling, UI asset drift, and redundant display logic without adding domain value.
- Transitioning to a pure **Headless API-First Architecture** consolidates all business invariants, permission checks, scheduling algorithms, and AI agent execution into authoritative, testable, and contract-governed REST API endpoints.

---

## Decision
We decommission all frontend presentation layers (`web/` and `mobile/`) and establish WayPoint strictly as a **Headless REST API & Agentic AI Platform**:
1. **Decommission Frontends**: Remove `web/` and `mobile/` source trees, Google Stitch UI design files (`docs/design/`), and browser screenshot automation tools.
2. **Authoritative Application Layer**: ASP.NET Core Web API serves as the sole, authoritative interface for all business operations, passenger workflows, and administrative management.
3. **Contract-Driven Integration**: All consumer capabilities are governed by OpenAPI 3.0 / Swagger specifications and strongly typed Data Transfer Objects (DTOs).
4. **Decoupled Client Ecosystem**: Any future external consumers (third-party transit aggregators, mobile apps, web dashboards, IoT validators) interface exclusively through authenticated HTTP/HTTPS JSON endpoints.

---

## Consequences

### Positive
- **Single Source of Truth**: Business rules, validation logic, and authorization policies exist solely on the server tier, preventing client-side drift or security bypasses.
- **Maximized Engineering Focus**: 100% of testing and implementation effort is concentrated on high-reliability backend systems, EF Core transactional concurrency, and multi-agent AI orchestration.
- **Contract Clarity**: Interactive OpenAPI/Swagger UI (`/swagger`) and machine-readable JSON contracts (`/swagger/v1/swagger.json`) serve as executable specifications.
- **Lower Deployment & Maintenance Overhead**: Eliminates separate node/vite and mobile build chains, reducing CI/CD execution time and cloud attack surface.

### Negative / Trade-Offs
- Visual demonstration relies on API client tooling (Swagger UI, Postman, curl) rather than graphical browser/mobile displays.
- Requirements previously targeting visual layout (`FR-FE-xx`) are mapped to their corresponding API endpoint contracts, response schemas, and telemetry streams.

---

## Compliance & Traceability
- **Superseded Decisions**: [ADR-001](ADR-001-react-state-management.md), [ADR-002](ADR-002-flutter-state-management.md).
- **Related Decisions**: [ADR-003](ADR-003-ai-orchestration.md) (Multi-Agent Subsystem), [ADR-004](ADR-004-ai-workflow-persistence.md) (Workflow Persistence), [ADR-005](ADR-005-cloud-deployment.md) (Cloud Infrastructure).
- **API Documentation**: Interactive Swagger interface at `/swagger`.
