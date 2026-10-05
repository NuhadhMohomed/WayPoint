# Student 4 Implementation Guide: Disruption, Rebooking & Approval

- **Assigned Student**: **Dineth** (Student 4)
- **Component**: **Component 4 — Disruption, Rebooking & Approval**
- **Core Domain Focus**: Disruption incident intake, passenger impact analysis, multi-agent AI rebooking, alerts, and Transport Manager approval boundary enforcement.
- **Assigned Feature Branch**: `feature/disruption-approval`
- **Architecture**: **Headless API-First Architecture** ([ADR-006](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/adr/ADR-006-headless-architecture.md))

---

## 1. Executive Component Overview

As the owner of **Component 4**, you are responsible for the critical operational resilience of WayPoint. Your work ingests disruption events, triggers multi-agent AI rebooking analysis, calculates passenger impact blast-radii, strictly halts high-impact changes behind the human Transport Manager approval boundary (`BR-APPROVAL-001`), and executes approved remedies transactionally.

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
   - Swagger Documentation: `http://localhost:5010/swagger`.
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

## 3. Headless API Contract & OpenAPI Specification

All endpoints must be thoroughly annotated for OpenAPI/Swagger documentation (`/swagger`):
- **Approval Gate**: Endpoints enforcing `[Authorize(Roles = "TransportManager,Admin")]` before applying rebooking decisions.
- **JSONB Observability**: Exposes structured agent traces (`AiWorkflow`, `AiWorkflowStep`, `AiToolCall`) without requiring UI dashboards.
- **Alert Broadcast**: Structured transit alert dispatch contracts for informing affected passengers and operators.

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

1. **Unit Tests (`backend/WayPoint.Tests/DisruptionTests.cs`)**:
   - Test disruption impact classification rules (ensuring timetable shift $>15$ min correctly triggers high-impact flag).
   - Test approval state machine transitions (`Proposed` $\rightarrow$ `PendingManagerApproval` $\rightarrow$ `Approved` / `Rejected`).
2. **Integration Tests**:
   - Test approval boundary: verify unauthorized execution of high-impact proposal returns HTTP 403 Forbidden without manager role.
   - Test transactional rollback during rebooking failure (ensuring original bookings remain intact if replacement service capacity check fails).
   - Test safe-failure fallback execution on simulated AI execution error.
3. **AI Tests (`ai/tests/`)**:
   - Run safety agent guardrail and golden test cases: `pytest ai/tests/test_safety_agent.py ai/tests/test_golden_safety.py -v`

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Approval Boundary Enforcement (`BR-APPROVAL-001`)**: Show the exact code branch where high-impact actions are halted in `PendingManagerApproval`.
- **Relational AI Audit Persistence (`ADR-004`)**: Explain why tool logs are stored in `AiToolCalls` with `JSONB` columns without saving raw hidden LLM reasoning (`REQ-DB-06`).
- **Transactional Integrity**: Walk through how `IDbContextTransaction` prevents partial rebookings during emergency schedule modifications.
