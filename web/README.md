# WayPoint Web Application (React 18 / Vite / Tailwind)

The authoritative web client interface for **WayPoint** (**SE3090 Assignment 1**). Designed for bus operators, transit dispatchers, transport managers, and system administrators.

---

## 1. Overview & Technology Stack

The web application is built with modern frontend best practices:
- **Framework**: React 18.3 (Single Page Application via Vite 5)
- **Styling**: Tailwind CSS 3.4 with custom transit design tokens
- **State Management**: Zustand 4.5 ([ADR-001](../docs/adr/ADR-001-react-state-management.md)) for client session/auth state
- **Server Cache & Async State**: TanStack Query (React Query v5) with optimistic updates
- **Routing**: React Router v6 with role-based route protection
- **Icons**: Lucide React
- **Testing**: Vitest 2.1 & React Testing Library

---

## 2. Feature Modules & Student Responsibilities

The web application is partitioned into feature slices matching the student functional areas:

```text
web/src/features/
├── journey/          # Student 1 (Sethum): Route Manager, Service Scheduler, Tourist Corridors
├── fleet/            # Student 2 (Nuhadh): Fleet Matrix, Dynamic Seat Layout Designer, Driver Rostering
├── bookings/         # Student 3 (Mithila): Operator Dashboard, Real-Time Manifest Monitor (Hold Countdown)
├── disruptions/      # Student 4 (Dineth): Disruption Intake, Manager Approval Workbench, AI Observability
└── admin/            # Shared / Dineth: Admin Console & User Role Management
```

### Detailed Student Pages & Capabilities

| Student Owner | Feature Directory | Page Components | Key Capabilities |
| :--- | :--- | :--- | :--- |
| **Sethum** (Student 1) | `features/journey/` | `RouteManagerPage`<br>`ServiceSchedulerPage`<br>`TouristCorridorsPage` | Visual route segment builder, intermediate stop sequencing, connecting transfer buffer indicator (`BR-TRANSFER-001`), timetable search |
| **Nuhadh** (Student 2) | `features/fleet/` | `FleetMatrixBuilderPage`<br>`SeatLayoutDesignerPage`<br>`DriverRosteringPage`<br>`FleetReviewsDashboardPage` | Interactive 2+2 and 1+2 seat layout generator, bus fleet inventory, driver rest constraint verification ($>8$ hrs), passenger review ratings |
| **Mithila** (Student 3) | `features/bookings/` | `OperatorDashboardPage`<br>`BookingManifestMonitorPage` | Live passenger booking manifest, real-time 10-minute hold countdown timer (`BR-HOLD-001`), ticket status badges, cancellation refunds |
| **Dineth** (Student 4) | `features/disruptions/` | `DisruptionIntakePage`<br>`ManagerApprovalWorkbenchPage`<br>`AiObservabilityPage`<br>`ServiceAlertBroadcastPage` | Incident blast radius intake, Transport Manager sign-off button gate (`BR-APPROVAL-001`), LangGraph execution step & tool latency viewer |

---

## 3. Setup & Development

### 3.1 Prerequisites
- Node.js (v18.0+)
- npm (v9.0+)

### 3.2 Installation & Startup
```bash
cd web

# Install dependencies
npm install

# Run Vite development server
npm run dev
```

The application runs locally at `http://localhost:5173`.

### 3.3 Production Build
```bash
npm run build
npm run preview
```

The compiled static distribution resides in `web/dist/` ready for cloud hosting (Render static site).

---

## 4. Automated Testing

The web suite utilizes **Vitest** and **React Testing Library**:

```bash
cd web

# Run Vitest test suite once
npm test

# Run tests in watch mode
npx vitest
```

### Automated Test Coverage
- `RouteManager.test.jsx`: Route catalog filters, stop creation, timetable interactions.
- `SeatLayoutDesigner.test.jsx`: Bus seat grid generation, row/column configuration.
- `BookingManifest.test.jsx`: Passenger manifest rendering, hold status countdown.
- `DisruptionHub.test.jsx`: Disruption reporting form, manager approval gate.
- `AuthAndOverview.test.jsx` & `AdminUsersPage.test.jsx`: Login flows, token handling, RBAC route guards.

---

## 5. Security & Authentication Architecture

- **Token Storage**: JWT access tokens are held in-memory via Zustand `authStore` to eliminate XSS token theft risks.
- **Axios Interceptor**: Automatically attaches `Authorization: Bearer <token>` to all requests to the ASP.NET Core API.
- **Role Route Protection**: Route guards enforce role permissions (`Admin`, `TransportManager`, `Operator`). Unauthorized attempts redirect gracefully with security alerts.
