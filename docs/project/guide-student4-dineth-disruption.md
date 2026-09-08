# Student 4 Implementation Guide: Disruption, Rebooking & Approval

- **Assigned Student**: **Dineth** (Student 4)
- **Component**: **Component 4 — Disruption, Rebooking & Approval**
- **Core Domain Focus**: Service disruption event logging, passenger impact analysis, rebooking proposal evaluation, public service alerts, and the Transport Manager Approval Boundary.

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

## 2. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Disruption` (`backend/WayPoint.Domain/Entities/Disruption/DisruptionEntities.cs`):

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`DisruptionCase`** | `Id`, `DisruptedServiceId`, `Reason`, `Severity`, `AffectedPassengerCount`, `Status` | `Minor`, `Major`, `Critical` severity |
| **`RebookingProposal`** | `Id`, `DisruptionCaseId`, `ReplacementServiceId`, `ProposedByAgent`, `Status` | Status: `Proposed`, `PendingManagerApproval`, `Approved`, `Rejected`, `Executed` |
| **`ApprovalDecision`** | `Id`, `RebookingProposalId`, `ManagerId`, `Decision`, `Comments`, `DecidedAt` | Decision: `Approve`, `Reject`, `RequestRevision` |
| **`ServiceAlert`** | `Id`, `ServiceId`, `Title`, `Message`, `PostedAt` | Public transit notices dispatched to passengers |

---

## 3. Backend Implementation Blueprint (`backend/`)

### 3.1 Controllers to Implement
Create these controllers under `backend/WayPoint.API/Controllers/`:
1. `DisruptionController.cs` (`/api/v1/disruptions`):
   - `POST /api/v1/disruptions` (`[Authorize(Roles = "Admin,TransportManager,Operator")]` - Log new disruption case)
   - `GET /api/v1/disruptions` (List active disruption cases with severity and status)
   - `GET /api/v1/disruptions/{id}/impact` (Calculate affected passengers, bookings, and revenue at risk)
2. `RebookingController.cs` (`/api/v1/rebooking`):
   - `POST /api/v1/rebooking/generate-proposal` (Construct replacement journey proposal for affected passengers)
   - `POST /api/v1/rebooking/{id}/execute` (`[Authorize(Roles = "Admin,TransportManager")]` - Transactionally execute approved rebooking)
3. `ApprovalController.cs` (`/api/v1/approvals`):
   - `GET /api/v1/approvals/pending` (`[Authorize(Roles = "Admin,TransportManager")]` - Retrieve pending high-impact approvals)
   - `POST /api/v1/approvals/{id}/decision` (`[Authorize(Roles = "Admin,TransportManager")]` - Submit formal Manager decision)
4. `ServiceAlertController.cs` (`/api/v1/alerts`):
   - `GET /api/v1/alerts` (List active public service disruption alerts)
   - `POST /api/v1/alerts` (`[Authorize(Roles = "Admin,Operator,TransportManager")]` - Broadcast notice to passengers)

### 3.2 Complex Business Operation (Beyond CRUD)
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

## 4. Frontend Web Implementation Blueprint (`web/`)

- **Folder Location**: `web/src/features/disruptions/`
- **API Client**: `disruptionApi.js` (wraps `/api/v1/disruptions`, `/api/v1/approvals`, `/api/v1/rebooking`)
- **Key Views to Build**:
  1. `DisruptionWorkbenchView.jsx`: Logging form for dispatchers to declare delays or breakdowns and view affected passenger lists.
  2. `ManagerApprovalWorkbenchView.jsx`: Dedicated Transport Manager screen showing:
     - Before vs. After operational impact comparison table.
     - Affected passenger count and estimated delay metrics.
     - Formal `Approve`, `Reject`, and `Request Revision` action buttons with mandatory comment field.
  3. `ServiceAlertBroadcastModal.jsx`: Interface to post transit alerts displayed on the public dashboard and passenger mobile app.

---

## 5. Mobile Flutter Implementation Blueprint (`mobile/`)

- **Folder Location**: `mobile/lib/features/disruption/`
- **Key Widgets & Screens**:
  1. `DisruptionBannerWidget.dart`: Prominent yellow/red alert banner displayed on home screen if passenger has an affected booking.
  2. `RebookingResponseScreen.dart`: Shows disruption details, proposed replacement bus/departure, and gives passenger two buttons:
     - **"Accept New Journey"** (Confirms transfer to new service).
     - **"Request Full 100% Refund"** (`BR-REFUND-002` - instant refund processing).
- **BLoC State Management**:
  - Events: `LoadDisruptionAlertsEvent`, `AcceptRebookingEvent`, `RequestDisruptionRefundEvent`.
  - States: `DisruptionsLoaded`, `RebookingAccepted`, `DisruptionRefundProcessed`.

---

## 6. Testing Requirements

1. **Unit Tests (`WayPoint.Tests/DisruptionTests.cs`)**:
   - Test disruption impact classification rules (ensuring timetable shift $>15$ min correctly triggers high-impact flag).
   - Test approval state machine transitions (`Proposed` $\rightarrow$ `PendingManagerApproval` $\rightarrow$ `Approved` / `Rejected`).
2. **Integration Tests**:
   - Test approval boundary: verify unauthorized execution of high-impact proposal returns HTTP 403 Forbidden without manager role.
   - Test transactional rollback during rebooking failure (ensuring original bookings remain intact if replacement service capacity check fails).

---

## 7. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Human Approval Boundary Enforcement**: Demonstrate how your backend blocks high-impact operational remedies until an authenticated Transport Manager executes the approval endpoint.
- **Transactional Rollback on Rebooking Failure**: Walk through your `IDbContextTransaction` ensuring that if seat 32 on the replacement bus is unavailable, all 31 previously transferred passengers are safely rolled back.
- **Affected Passenger Calculation**: Show the LINQ query aggregating ticketed passengers for a disrupted service departure.
