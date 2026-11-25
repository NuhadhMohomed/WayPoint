# WayPoint Web Application (React 18 / Vite / Tailwind CSS)

> **Status: Active Development (`v0.1.0` / WIP)**  
> The WayPoint operator and admin web portal is under active development. Core layouts, role-guarded routing, and feature pages are wired up to the ASP.NET Core API, with ongoing UI polish, full form validation, and live telemetry integration in progress.

---

## 1. Technology Stack (`package.json`)

- **Framework**: React `18.3.1` + Vite `5.4.6`
- **Styling**: Tailwind CSS `3.4.11` (`clsx`, `tailwind-merge`)
- **Client State**: Zustand `4.5.5` ([ADR-001](../docs/adr/ADR-001-react-state-management.md))
- **Server State & Caching**: TanStack Query `5.56.2`
- **HTTP & Routing**: Axios `1.7.7`, React Router DOM `6.26.2`
- **Icons**: Lucide React `0.441.0`
- **Testing**: Vitest `2.1.9`, `@testing-library/react` `16.3.3`, `jsdom`

---

## 2. Application Structure & Current Routes (`src/App.jsx`)

```text
web/src/
├── api/                    # Axios client (client.js) & TanStack Query config (queryClient.js)
├── components/             # ProtectedRoute & shared UI primitives (DataTable, Modal, Card, etc.)
├── layouts/                # DashboardLayout (navigation shell, command palette, offline banner)
├── pages/                  # LoginPage, OverviewPage, NotFoundPage, fleet/FleetReviewsDashboardPage
├── store/                  # Zustand authStore
└── features/
    ├── journey/            # JourneyHubLayout, RouteManagerPage, ServiceSchedulerPage, TouristCorridorsPage
    ├── fleet/              # FleetHubLayout, FleetMatrixBuilderPage, SeatLayoutDesignerPage, DriverRosteringPage
    ├── bookings/           # BookingManifestMonitorPage, OperatorDashboardPage
    ├── disruptions/        # DisruptionHubLayout, DisruptionIntakePage, ManagerApprovalWorkbenchPage,
    │                       # ServiceAlertBroadcastPage, AiObservabilityPage, AdminConsolePage
    └── admin/              # AdminHubLayout, AdminUsersPage, ChangeRoleModal, ProvisionUserModal
```

### Active Routes
- `/login` — Authentication page (`/register` currently redirects to `/login`)
- `/` — `OverviewPage`
- `/routes/*` — `catalog`, `scheduler`, `corridors`
- `/fleet/*` — `buses`, `layouts`, `drivers`, `reviews`
- `/bookings`, `/bookings/manifest`, `/bookings/operator`
- `/disruptions/*` — `intake`, `approvals`, `alerts`, `ai-traces`, `admin`
- `/admin/users` — User & role administration

---

## 3. Setup & Local Development

### Prerequisites
- Node.js (v18+ / CI uses Node 20)
- npm

### Run Locally
```bash
cd web
npm install
npm run dev
```
- **Local Dev Server**: `http://localhost:5173`
- Configure `VITE_API_URL` in `.env` if pointing to a non-default backend (defaults to `http://localhost:5010/api/v1` in `src/api/client.js`).

### Build & Preview
```bash
npm run build
npm run preview
```

---

## 4. Automated Testing

```bash
cd web
npm test
```

Current test files include:
- `src/api/__tests__/client.test.js`
- `src/components/ui/__tests__/Primitives.test.jsx`
- `src/pages/__tests__/AuthAndOverview.test.jsx`
- `src/features/journey/__tests__/RouteManager.test.jsx`
- `src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx`
- `src/features/bookings/__tests__/BookingManifest.test.jsx`
- `src/features/disruptions/__tests__/DisruptionHub.test.jsx`
- `src/features/admin/__tests__/AdminUsersPage.test.jsx`

---

## 5. In Progress / Known WIP Notes

- [ ] **Self-Service Registration**: `/register` currently redirects to `/login`; account provisioning is handled through `AdminUsersPage` or API registration.
- [ ] **Duplicate Manifest File Cleanup**: Consolidate legacy `features/fleet/BookingManifestMonitorPage.jsx` with the active `features/bookings/BookingManifestMonitorPage.jsx`.
- [ ] **Token Storage Hardening**: `src/api/client.js` currently reads from `authStore` with a `localStorage` fallback (`waypoint_token`); full in-memory/cookie hardening is pending.
- [ ] **Live AI Trace Polling & Error Polish**: Continuing refinements on `AiObservabilityPage` and end-to-end backend synchronization.
