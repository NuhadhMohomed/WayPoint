# Student 4 Implementation Guide: Disruption, Rebooking & Approval

- **Assigned Student**: **Dineth** (Student 4)
- **Component**: **Component 4 — Disruption, Rebooking & Approval**
- **Core Domain Focus**: Service disruption event logging, passenger impact analysis, multi-agent rebooking evaluation, public service alerts, and the Transport Manager Approval Boundary.
- **Assigned Feature Branch**: `feature/disruption-approval`

---

## 1. Executive Component Overview

As the owner of **Component 4**, you manage critical incident recovery and governance. When a service experiences delays, vehicle breakdowns, or route closures, your work allows dispatchers to log disruptions, calculate passenger impact, generate rebooking proposals, enforce the mandatory human approval boundary for high-impact changes, and transactionally execute approved rebooking remedies.

### Assigned User Stories
- `US-PASS-006` (Disruption Rebooking Response)
- `US-OP-003` (Disruption Logging & Feasibility)
- `US-MGR-001` (Disruption Review & Before/After Impact Inspection)
- `US-MGR-002` (Manager Approval Execution)
- `US-MGR-003` (Agent Execution Summary & Observability Inspection)

---

## 2. Local Setup & Environment Checklist

1. **Environment Configuration**:
   ```bash
   cp .env.example .env
   ```
   - Ensure `DATABASE_URL` points to your active PostgreSQL instance.
   - Authoritative API base URL: `http://localhost:5010/api/v1` (`ASPNETCORE_URLS=http://localhost:5010`).
   - Web development server connects via `VITE_API_URL=http://localhost:5010/api/v1`.
   - Mobile app connects via `FLUTTER_API_URL=http://localhost:5010/api/v1`.
   - AI service endpoint configured via `AI_SERVICE_URL=http://localhost:8000`.
2. **Restore & Seed Database**:
   ```bash
   dotnet restore backend/WayPoint.sln
   dotnet run --project backend/WayPoint.API -- --seed
   ```
3. **Branch Workflow**:
   ```bash
   git checkout -b feature/disruption-approval
   ```

---

## 3. Design System & UI Contract (`docs/design/DESIGN.md`)

All UI screens must strictly comply with [`docs/design/DESIGN.md`](docs/design/DESIGN.md):
- **Brand Tokens**: Lanka Blue (`#0056D2`), Sunset Amber (`#FEB300`), Jungle Green (`#005312`), Surface (`#F8F9FA` / `#FFFFFF`).
- **Typography Pairing**: **Plus Jakarta Sans** (headings) and **Inter** (body, operational tables, and audit logs).
- **Reusable Primitives**:
  - Web: Use `Button`, `Card`, and `TransitBadge` in `web/src/components/ui/`.
  - Mobile: Use `WayPointButton`, `WayPointCard`, and `TransitBadge` in `mobile/lib/core/widgets/`.

---

## 4. Google Stitch UI Screen Specifications

Reference your assigned pre-designed screens in [`docs/design/stitch-screens-index.md`](docs/design/stitch-screens-index.md):

| Screen Code | Screen Title | Stitch Screen ID | Platform | Target File |
| :--- | :--- | :--- | :--- | :--- |
| **MOB-09** | Disruption Push Alert & Alternative Bus | `2bfd1cb2561245a99f17148299e4099d` | Mobile | `mobile/lib/features/disruption/screens/disruption_alert_screen.dart` |
| **WEB-07** | Disruption Incident Intake & Impact | `91532418794f46b089e31a1f8125be4c` | Web | `web/src/features/disruptions/DisruptionIntakePage.jsx` |
| **WEB-08** | Transport Manager Approval Workbench | `36ac1789e4ef4627b0effb0951e2d402` | Web | `web/src/features/disruptions/ManagerApprovalWorkbenchPage.jsx` |
| **WEB-09** | Public Service Alert Broadcast Center | `7b99b77b7518482c82ac9e795ca398ca` | Web | `web/src/features/disruptions/ServiceAlertBroadcastPage.jsx` |
| **WEB-10** | AI Multi-Agent Observability & Traces | `30769f8f61624dc694a42feedf4e9866` | Web | `web/src/features/disruptions/AiObservabilityPage.jsx` |
| **WEB-12** | System Admin, RBAC & Immutable Audit | `e835400b1aba4b8d9735d6c168edb061` | Web | `web/src/features/disruptions/AdminConsolePage.jsx` |

---

## 5. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Disruption` (`backend/WayPoint.Domain/Entities/Disruption/DisruptionEntities.cs`) and `WayPoint.Domain.Entities.Ai`:

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`DisruptionCase`** | `Id`, `DisruptedServiceId`, `Reason`, `Severity`, `AffectedPassengerCount`, `Status` | `Minor`, `Major`, `Critical` severity |
| **`RebookingProposal`** | `Id`, `DisruptionCaseId`, `ReplacementServiceId`, `ProposedByAgent`, `Status` | Status: `Proposed`, `PendingManagerApproval`, `Approved`, `Rejected`, `Executed` |
| **`ApprovalDecision`** | `Id`, `RebookingProposalId`, `ManagerId`, `Decision`, `Comments`, `DecidedAt` | Decision: `Approve`, `Reject`, `RequestRevision` |
| **`ServiceAlert`** | `Id`, `ServiceId`, `Title`, `Message`, `PostedAt` | Public transit notices dispatched to passengers |
| **`AiWorkflow`** & **`AiWorkflowStep`** | `Id`, `Objective`, `Status`, `AgentName`, `StepOrder` | Top-level workflow trace and individual step execution |
| **`AiToolCall`** | `Id`, `AiWorkflowStepId`, `ToolName`, `ArgumentsJson`, `ResultJson`, `DurationMs` | Allow-listed tool trace stored as `JSONB` (`ADR-004`) |
| **`AiValidationResult`** | `Id`, `AiWorkflowStepId`, `RuleName`, `Passed`, `ValidationDetails` | Deterministic backend safety validation logs |

---

## 6. Backend Implementation Blueprint (`backend/`)

### 6.1 Controllers to Implement
Create these controllers under `backend/WayPoint.API/Controllers/`:
1. `DisruptionController.cs` (`/api/v1/disruptions`):
   - `POST /api/v1/disruptions` (`[Authorize(Roles = "Admin,TransportManager,Operator")]` - Log new disruption case)
   - `GET /api/v1/disruptions` (List active disruption cases with severity and status)
   - `GET /api/v1/disruptions/{id}/impact` (Calculate affected passengers, bookings, and revenue at risk)
2. `RebookingController.cs` (`/api/v1/rebooking`):
   - `POST /api/v1/rebooking/generate-proposal` (Initiate rebooking proposal generation)
   - `POST /api/v1/rebooking/{id}/execute` (`[Authorize(Roles = "Admin,TransportManager")]` - Transactionally execute approved rebooking)
3. `ApprovalController.cs` (`/api/v1/approvals`):
   - `GET /api/v1/approvals/pending` (`[Authorize(Roles = "Admin,TransportManager")]` - Retrieve pending high-impact queue)
   - `POST /api/v1/approvals/{id}/decision` (`[Authorize(Roles = "Admin,TransportManager")]` - Submit formal Manager decision)
4. `AiWorkflowController.cs` (`/api/v1/ai/workflows`):
   - `GET /api/v1/ai/workflows/{id}` (Retrieve workflow execution steps, tool logs, and status)
5. `ServiceAlertController.cs` (`/api/v1/alerts`):
   - `GET /api/v1/alerts` (List active public service alerts)
   - `POST /api/v1/alerts` (`[Authorize(Roles = "Admin,Operator,TransportManager")]` - Broadcast notice)

### 6.2 Complex Business Operation (Beyond CRUD)
- **Operation**: *Multi-Agent Disruption Rebooking & Manager Approval Boundary Operation*.
- **Logic**:
  1. **Impact Severity Assessment (`BR-DISRUPT-001`)**:
     - Departure timetable shift $\le 15$ minutes $\rightarrow$ **Low Impact** (Can execute automatically).
     - Departure timetable shift $> 15$ minutes OR replacement bus downgrade OR service cancellation $\rightarrow$ **High Impact** (`BR-APPROVAL-001`).
  2. **Manager Approval Gate**:
     - If High Impact $\rightarrow$ Rebooking proposal transitions to `PendingManagerApproval` state. Automated execution is strictly blocked.
     - Creates an `ApprovalDecision` record linked to the `RebookingProposal`.
  3. **Transactional Rebooking Execution**:
     - When Transport Manager approves $\rightarrow$ Execute inside `IDbContextTransaction`:
       a. Transfer all affected bookings to replacement service.
       b. Re-issue updated QR tickets to passengers.
       c. Release old seat allocations and lock replacement seats.
       d. Create public and private `ServiceAlert` notifications.

---

## 7. Agentic AI Responsibilities (Student 4)

- **Assigned Agent**: **Validation & Safety Agent** and Multi-Agent Workflow Coordinator (`ADR-003`).
- **Domain Purpose**: Coordinates the 4 agents, validates proposals against business rules, classifies severity, enforces human approval boundaries, and ensures safe failure.
- **Allow-Listed Tools**:
  - `CreateRebookingProposal`
  - `CalculatePassengerImpact`
  - `RequestManagerApproval`
  - `ApplyApprovedOperationalChange`
- **Safety Rule**: High-impact operational changes CANNOT bypass human Transport Manager approval. AI failures must default to `SafeFailure` fallback without corrupting operational data.

---

## 8. Testing Requirements

1. **Unit Tests (`WayPoint.Tests/DisruptionTests.cs`)**:
   - Test disruption impact classification rules (ensuring timetable shift $>15$ min correctly triggers high-impact flag).
   - Test approval state machine transitions (`Proposed` $\rightarrow$ `PendingManagerApproval` $\rightarrow$ `Approved` / `Rejected`).
2. **Integration Tests**:
   - Test approval boundary: verify unauthorized execution of high-impact proposal returns HTTP 403 Forbidden without manager role.
   - Test transactional rollback during rebooking failure (ensuring original bookings remain intact if replacement service capacity check fails).
   - Test safe-failure fallback execution on simulated AI execution error.

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Approval Boundary Enforcement (`BR-APPROVAL-001`)**: Show the exact code branch where high-impact actions are halted in `PendingManagerApproval`.
- **Relational AI Audit Persistence (`ADR-004`)**: Explain why tool logs are stored in `AiToolCalls` with `JSONB` columns without saving raw hidden LLM reasoning (`REQ-DB-06`).
- **Transactional Integrity**: Walk through how `IDbContextTransaction` prevents partial rebookings during emergency schedule modifications.
