# Student 4 Implementation Guide: Disruption, Rebooking & Approval

- **Assigned Student**: **Dineth** (Student 4)
- **Component**: **Component 4 — Disruption, Rebooking & Approval**
- **Core Domain Focus**: Disruption incident intake, passenger impact analysis, multi-agent AI rebooking, alerts, and Transport Manager approval boundary enforcement.
- **Assigned Feature Branch**: `feature/disruption-approval`
- **Architecture**: **Integrated Full-Stack Architecture** ([SPEC-2026-10-05-FRONTEND-RECONSTRUCTION](../superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md))

---

## 1. Executive Component Overview

As the owner of **Component 4**, you are responsible for the critical operational resilience of WayPoint:
1. **ASP.NET Core Web API**: Disruption logging, passenger blast-radius impact calculation, Transport Manager approval gate (`BR-APPROVAL-001`), transactional rebooking execution, and public service alert broadcasting.
2. **PostgreSQL Relational DB**: Schemas and EF Core migrations for `DisruptionCases`, `RebookingProposals`, `ApprovalDecisions`, `ServiceAlerts`, `AiWorkflows`, `AiWorkflowSteps`, `AiToolCalls`, and `AiValidationResults`.
3. **React Web Application (`web/src/features/disruptions/`)**: Incident intake workbench, Transport Manager approval workbench with before/after impact comparison, AI observability dashboard, and service alert broadcaster (`DisruptionIntakePage`, `ManagerApprovalWorkbenchPage`, `AiObservabilityPage`, `ServiceAlertBroadcastPage`).
4. **Flutter Mobile Application (`mobile/lib/features/disruption/` & `fleet/`)**: Passenger in-app disruption alert banner and rebooking response screen (`DisruptionAlertScreen`), and conductor mobile camera QR ticket scanner with live boarding manifest (`ConductorScannerScreen`, `ConductorManifestScreen`).
5. **Agentic AI**: Specialized **Validation & Safety Agent** (`ai/agents/safety_agent.py`) and LangGraph orchestration graph coordinator (`ai/agents/graph.py`).

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
   - Authoritative API base URL: `http://localhost:5010/api/v1`.
   - Swagger Documentation: `http://localhost:5010/swagger`.
2. **Run Backend API**:
   ```bash
   dotnet restore backend/WayPoint.sln
   dotnet run --project backend/WayPoint.API -- --seed
   ```
3. **Run React Web Application**:
   ```bash
   cd web && npm install && npm run dev
   ```
4. **Run Flutter Mobile Application**:
   ```bash
   cd mobile && flutter pub get && flutter run
   ```

---

## 3. Client Presentation Tier Implementations

### 3.1 React Web Pages (`web/src/features/disruptions/`)
- **`DisruptionIntakePage.jsx`**: Incident intake form, affected service tagging, candidate remedy review, and blast-radius impact analysis.
- **`ManagerApprovalWorkbenchPage.jsx`**: Dedicated Transport Manager approval portal with before/after operational comparison, affected passenger metrics, and Approve / Reject / Request-Revision action controls (`BR-APPROVAL-001`).
- **`AiObservabilityPage.jsx`**: Execution timeline dashboard rendering agent execution logs, tool invocation traces, execution latencies, and validation outcomes.
- **`ServiceAlertBroadcastPage.jsx`**: Public and in-app service alert broadcaster.
- State: TanStack Query v5 hooks (`useDisruptions`, `useApprovals`, `useAiWorkflows`), Zustand `disruptionStore`, Vitest component tests.

### 3.2 Flutter Mobile Screens (`mobile/`)
- **`DisruptionAlertScreen.dart`**: Prominent in-app alert banner and remediation screen alerting affected passengers and offering one-tap rebooking acceptance or instant refund requests.
- **`ConductorScannerScreen.dart`**: Camera-based QR ticket scanner utilizing `mobile_scanner`, verifying cryptographic HMAC-SHA256 signatures, with haptic feedback on boarding.
- **`ConductorManifestScreen.dart`**: Mobile passenger manifest with real-time boarding verification checklist.
- State: `DisruptionBloc` and `ScannerBloc` with Dio HTTP client, unit & widget tests.

---

---

## 4. Authoritative Request & Response DTO Specifications

### 4.1 Disruption Intake Request DTO (`CreateDisruptionDto`)
```json
{
  "serviceId": "e1a90c12-3456-789a-bcde-f0123456789a",
  "reason": "Engine Breakdown on Southern Expressway",
  "severity": "Major",
  "estimatedDelayMinutes": 45
}
```

### 4.2 Manager Approval Decision DTO (`ApprovalDecisionRequestDto`)
```json
{
  "rebookingProposalId": "prop-5512-ab77",
  "decision": "Approve",
  "managerSignature": "KAMAL-MGR-SIG-7712",
  "comments": "Approved replacement bus dispatch NC-4589 for affected passengers."
}
```

### 4.3 AI Observability Trace Response DTO (`AiWorkflowTraceDto`)
```json
{
  "workflowId": "wf-8812-cc44",
  "objective": "Resolve disruption on Colombo-Galle service SRV-01",
  "status": "PendingManagerApproval",
  "steps": [
    {
      "stepOrder": 1,
      "agentName": "ResourceAgent",
      "tool": "CheckReplacementResources",
      "status": "Success",
      "durationMs": 142
    },
    {
      "stepOrder": 2,
      "agentName": "SafetyAgent",
      "tool": "RequestManagerApproval",
      "status": "Paused",
      "durationMs": 28
    }
  ]
}
```

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
Controllers reside under `backend/WayPoint.API/Controllers/`:
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

1. **Backend Unit & Integration Tests (`backend/WayPoint.Tests/DisruptionTests.cs`)**:
   - Test disruption impact classification rules (ensuring timetable shift $>15$ min correctly triggers high-impact flag).
   - Test approval state machine transitions (`Proposed` $\rightarrow$ `PendingManagerApproval` $\rightarrow$ `Approved` / `Rejected`).
   - Test approval boundary: verify unauthorized execution of high-impact proposal returns HTTP 403 Forbidden without manager role.
   - Test transactional rollback during rebooking failure (ensuring original bookings remain intact if replacement service capacity check fails).
   - Test safe-failure fallback execution on simulated AI execution error.
   ```bash
   dotnet test backend/WayPoint.sln --filter "FullyQualifiedName~Disruption|FullyQualifiedName~Approval"
   ```
2. **React Web Tests (`web/src/features/disruptions/__tests__/`)**:
   - Vitest component tests for `ManagerApprovalWorkbenchPage` and `DisruptionIntakePage`.
   ```bash
   cd web && npm test
   ```
3. **Flutter Mobile Tests (`mobile/test/features/disruption/`)**:
   - Widget tests for `DisruptionAlertScreen` and `ConductorScannerScreen`.
   ```bash
   cd mobile && flutter test test/features/disruption/
   ```
4. **AI Tests (`ai/tests/`)**:
   - Run safety agent guardrail and golden test cases:
   ```bash
   cd ai && pytest tests/test_safety_agent.py tests/test_golden_safety.py -v
   ```
5. **Cross-Platform End-to-End Workflow Test (`REQ-TEST-05`)**:
   - Closed-loop verification across Flutter mobile booking, LangGraph AI triage, React manager approval, and transactional notification update.

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Approval Boundary Enforcement (`BR-APPROVAL-001`)**: Show the exact code branch where high-impact actions are halted in `PendingManagerApproval`.
- **Relational AI Audit Persistence (`ADR-004`)**: Explain why tool logs are stored in `AiToolCalls` with `JSONB` columns without saving raw hidden LLM reasoning (`REQ-DB-06`).
- **Transactional Integrity**: Walk through how `IDbContextTransaction` prevents partial rebookings during emergency schedule modifications.
