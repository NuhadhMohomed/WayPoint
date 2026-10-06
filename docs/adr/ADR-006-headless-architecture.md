# ADR-006: Superseded — Historical Transition to Headless Architecture

## Title
ADR-006: Historical Exploration of Headless Architecture (Superseded & Deprecated)

## Status
`Superseded / Deprecated` (Superseded by [SPEC-2026-10-05-FRONTEND-RECONSTRUCTION](../superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md) and SE3090 Assignment 1 Full-Stack Requirements; [ADR-001](ADR-001-react-state-management.md) and [ADR-002](ADR-002-flutter-state-management.md) are **Active & Accepted**)

---

## Context
During interim sprint planning, the engineering team investigated a headless API-first operational model where the ASP.NET Core Web API served as the authoritative application layer and client interfaces were initially decoupled.

However, the **SE3090 Assignment 1 Specification and Marking Scheme** strictly mandates an **Integrated Full-Stack System** with direct individual and group evaluation across client tiers:
1. **React Web Application (`REQ-TECH-04`)**: 10 individual marks per student for responsive functional components, state management, protected routing, and operator dashboards.
2. **Flutter Mobile Application (`REQ-TECH-05`)**: 10 individual marks per student for cross-platform mobile widgets, BLoC/Cubit state management, secure storage, QR scanner device features, and passenger workflows.
3. **Integrated-System Rule (`REQ-ASSIGN-04`)**: Disconnected prototypes are strictly prohibited; React and Flutter applications MUST directly consume the shared ASP.NET Core Web API, PostgreSQL database, JWT identity, and business rules.

---

## Decision
We formally **supersede and deprecate** ADR-006. The full multi-tier presentation layer is active and integrated:
1. **Reactivate ADR-001**: React Web Application using React 18, Vite, Tailwind CSS v3 (Sovereign UI design tokens), TanStack Query v5, and Zustand.
2. **Reactivate ADR-002**: Flutter Mobile Application using Flutter 3.x, Dart 3, Velora Transit design tokens, Flutter BLoC/Cubit, and Dio HTTP client.
3. **Full-Stack Parity**: All 4 student components own vertical slices spanning ASP.NET Core Web API, PostgreSQL, React Web pages, Flutter Mobile screens, Agentic AI agents, and automated test suites.

---

## Consequences

### Positive
- Fully satisfies the SE3090 Assignment 1 evaluation rubric, protecting the 20 marks allocated to React and Flutter individual contributions.
- Delivers an end-to-end user experience for transport operators (React) and passengers/conductors (Flutter).
- Enables live demonstration of the end-to-end cross-platform workflow (`REQ-TEST-05`): Disruption detected → Multi-Agent AI Triage → Transport Manager review and approval in React Web App → Transactional reassignment in ASP.NET Core & PostgreSQL → In-app notification and QR ticket update in Flutter Mobile App.

### Negative / Trade-Offs
- Requires maintaining client-side build pipelines, testing suites (Vitest for React, Flutter test for mobile), and cloud hosting configurations.

---

## Compliance & Traceability
- **Reinstated Decisions**: [ADR-001](ADR-001-react-state-management.md) (React State Management), [ADR-002](ADR-002-flutter-state-management.md) (Flutter State Management).
- **Related Decisions**: [ADR-003](ADR-003-ai-orchestration.md) (Multi-Agent Subsystem), [ADR-004](ADR-004-ai-workflow-persistence.md) (Workflow Persistence), [ADR-005](ADR-005-cloud-deployment.md) (Cloud Infrastructure).
- **Design Specification**: [SPEC-2026-10-05-FRONTEND-RECONSTRUCTION](../superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md).

