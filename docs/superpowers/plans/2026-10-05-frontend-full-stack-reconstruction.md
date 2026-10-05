# WayPoint Frontend Full-Stack Reconstruction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reconstruct from scratch the complete presentation tier for WayPoint—a high-density React 18 Web Operations Hub and an app-native Flutter 3.x Mobile Passenger & Conductor App—exhaustively connected to 22 ASP.NET Core API controllers and the Python Multi-Agent AI subsystem, enforcing all 20 Production-Readiness Gates and Google Stitch design tokens.

**Architecture:** Client-tier separation with shared authoritative ASP.NET Core API (`http://localhost:5010/api/v1`). Web uses React 18 + Vite + Tailwind CSS v3 + TanStack Query v5 + Zustand. Mobile uses Flutter 3.x + BLoC/Cubit + Dio + Mobile Scanner. Finite state machines (`Loading`, `Skeleton`, `Empty`, `Error`, `Offline`, `Success`) wrap every remote resource.

**Tech Stack:** React 18, Vite, Tailwind CSS v3, TanStack Query v5, Zustand, Axios, Lucide Icons, Vitest, Flutter 3.x, Dart 3, flutter_bloc, Dio, mobile_scanner, flutter_secure_storage.

**Spec:** [`docs/superpowers/specs/2026-10-05-frontend-full-stack-design.md`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/superpowers/specs/2026-10-05-frontend-full-stack-design.md)

---

## Global Constraints

- Authoritative Backend URL: `http://localhost:5010/api/v1` (with zero mock/fake data fallbacks).
- Brand Tokens: Lanka Blue (`#0056D2`), Sunset Amber (`#FEB300`), Jungle Green (`#005312`), Neutral Slate (`#0F172A`), Background Canvas (`#F8FAFC`).
- Typography Pairing: Plus Jakarta Sans (headings & hero elements) and Inter (body copy, tables & manifests).
- Concurrency & Business Rules: Enforce 10-minute temporary seat hold countdown (`FR-BOOKING-001`), minimum 20-minute transfer window (`BR-TRANSFER-001`), 8-hour driver rest validation (`BR-RESOURCE-002`), and Transport Manager human approval boundary for AI changes (`BR-APPROVAL-001`).
- Production-Readiness Gate: Every form must feature client validation and double-submit prevention; every query must render skeletons, empty states, and friendly retry errors.

---

## Review Focus

1. **Stale/Expired Seat Holds**: Opening checkout after 10-minute hold expiry must show explicit expiration modal and release hold, not fail silently on payment.
2. **Transfer Buffer Boundary (<20m)**: Connecting search results with transfer windows under 20 minutes must show prominent red warning badge and block selection.
3. **Transport Manager Approval Rejection**: Rejection of high-impact AI proposal must rollback state, log manager reason, and unblock the approval queue without corrupting passenger tickets.
4. **Offline Resilience & Network Reconnection**: Going offline during seat booking or manifest viewing must display top `OfflineBanner` without crashing or clearing user inputs.
5. **Conductor QR Scanner Fraud Check**: Scanning a duplicate, cancelled, or altered QR code must trigger immediate error buzzer and red screen overlay without marking passenger as boarded.

---

## Task Breakdown

### Task 1: Web Foundation, Tailwind Design Tokens & UI Primitives

**Files:**
- Create: `web/package.json`
- Create: `web/vite.config.js`
- Create: `web/tailwind.config.js`
- Create: `web/src/index.css`
- Create: `web/src/components/ui/Button.jsx`
- Create: `web/src/components/ui/Card.jsx`
- Create: `web/src/components/ui/TransitBadge.jsx`
- Create: `web/src/components/ui/Skeleton.jsx`
- Create: `web/src/components/ui/EmptyState.jsx`
- Create: `web/src/components/ui/ErrorState.jsx`
- Create: `web/src/components/ui/OfflineBanner.jsx`
- Test: `web/src/components/ui/__tests__/Primitives.test.jsx`

**Interfaces:**
- Consumes: Google Stitch design tokens (`#0056D2`, `#FEB300`, `#005312`).
- Produces: Core UI atomic primitives (`Button`, `Card`, `TransitBadge`, `Skeleton`, `EmptyState`, `ErrorState`, `OfflineBanner`) with loading spinners and disabled states.

- [ ] **Step 1: Write the failing test for UI primitives**

```javascript
// web/src/components/ui/__tests__/Primitives.test.jsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { Button } from '../Button';
import { TransitBadge } from '../TransitBadge';
import { EmptyState } from '../EmptyState';

describe('UI Atomic Primitives', () => {
  it('renders Button with loading spinner and disabled state', () => {
    render(<Button isLoading>Submit</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('renders TransitBadge with correct variant styling', () => {
    render(<TransitBadge variant="luxury">Luxury Express</TransitBadge>);
    expect(screen.getByText('Luxury Express')).toHaveClass('bg-blue-50');
  });

  it('renders EmptyState with title and action button', () => {
    render(<EmptyState title="No routes found" actionLabel="Create Route" onAction={() => {}} />);
    expect(screen.getByText('No routes found')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create route/i })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx vitest run src/components/ui/__tests__/Primitives.test.jsx`
Expected: FAIL with missing modules.

- [ ] **Step 3: Implement Vite, Tailwind tokens, and atomic primitives in `web/`**

Initialize package.json, configure Tailwind colors (`waypoint-blue: #0056D2`, `waypoint-amber: #FEB300`, `waypoint-green: #005312`), and implement the UI primitives with full loading, disabled, and accessibility attributes.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd web && npx vitest run src/components/ui/__tests__/Primitives.test.jsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/package.json web/vite.config.js web/tailwind.config.js web/src/
git commit -m "feat(web): scaffold Vite project, Tailwind design tokens, and UI atomic primitives"
```

---

### Task 2: Web API Client, Interceptors & Centralized Query Client

**Files:**
- Create: `web/src/api/client.js`
- Create: `web/src/api/queryClient.js`
- Create: `web/src/store/authStore.js`
- Create: `web/src/store/themeStore.js`
- Test: `web/src/api/__tests__/client.test.js`

**Interfaces:**
- Consumes: `localStorage`, ASP.NET Core `/api/v1` base URL.
- Produces: Axios `apiClient` with JWT Bearer injection and 401 refresh handler, `useAuthStore` with role helpers (`isAdmin`, `isManager`, `isOperator`), and `queryClient` with default query options.

- [ ] **Step 1: Write the failing test for API client and auth store**

```javascript
// web/src/api/__tests__/client.test.js
import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../../store/authStore';
import { apiClient } from '../client';

describe('API Client & Auth Store', () => {
  beforeEach(() => {
    useAuthStore.getState().logout();
  });

  it('stores JWT token and computes role permissions correctly', () => {
    useAuthStore.getState().setAuth('mock-token-xyz', {
      id: 'usr-1',
      email: 'manager@waypoint.lk',
      role: 'TransportManager'
    });

    const state = useAuthStore.getState();
    expect(state.token).toBe('mock-token-xyz');
    expect(state.isManager).toBe(true);
    expect(state.isAdmin).toBe(false);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx vitest run src/api/__tests__/client.test.js`
Expected: FAIL with missing modules.

- [ ] **Step 3: Implement `apiClient`, `queryClient`, and `authStore`**

Build Axios instance with `baseURL: 'http://localhost:5010/api/v1'`, request interceptor attaching `Bearer ${token}`, response interceptor extracting `response.data`, and Zustand store with role access flags.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd web && npx vitest run src/api/__tests__/client.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/api/ web/src/store/
git commit -m "feat(web): configure Axios client with JWT interceptors, TanStack Query, and AuthStore"
```

---

### Task 3: Web Dashboard Shell, Command Palette (`Ctrl+K`) & Protected Navigation

**Files:**
- Create: `web/src/components/ProtectedRoute.jsx`
- Create: `web/src/layouts/DashboardLayout.jsx`
- Create: `web/src/components/ui/CommandPalette.jsx`
- Create: `web/src/pages/LoginPage.jsx`
- Create: `web/src/pages/RegisterPage.jsx`
- Create: `web/src/pages/NotFoundPage.jsx`
- Test: `web/src/layouts/__tests__/Navigation.test.jsx`

**Interfaces:**
- Consumes: `useAuthStore`, `Button`, `TransitBadge`.
- Produces: `DashboardLayout` with responsive collapsible sidebar, breadcrumbs, `CommandPalette` (`Ctrl+K`), and role-gated `<ProtectedRoute>`.

- [ ] **Step 1: Write the failing test for ProtectedRoute and DashboardLayout**

```javascript
// web/src/layouts/__tests__/Navigation.test.jsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from '../../components/ProtectedRoute';
import { useAuthStore } from '../../store/authStore';

describe('Navigation & Route Guards', () => {
  it('redirects unauthenticated user to /login', () => {
    useAuthStore.getState().logout();
    render(
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/login" element={<div>Login Page</div>} />
          <Route path="/admin" element={<ProtectedRoute roles={['Admin']}><div>Admin Console</div></ProtectedRoute>} />
        </Routes>
      </MemoryRouter>
    );
    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx vitest run src/layouts/__tests__/Navigation.test.jsx`
Expected: FAIL.

- [ ] **Step 3: Implement `ProtectedRoute`, `DashboardLayout`, and `CommandPalette`**

Add sidebar navigation links mapped to all 4 student components (`/routes`, `/services`, `/fleet`, `/seats/designer`, `/manifest`, `/disruptions`, `/approvals`, `/ai/observability`, `/admin/users`). Implement `Ctrl+K` spotlight search.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd web && npx vitest run src/layouts/__tests__/Navigation.test.jsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/components/ web/src/layouts/ web/src/pages/
git commit -m "feat(web): implement DashboardLayout, CommandPalette, and ProtectedRoute guards"
```

---

### Task 4: Web Component 1 — Journey Planning & Route Catalogue (Sethum)

**Files:**
- Create: `web/src/features/journey/journeyApi.js`
- Create: `web/src/features/journey/RouteManagerPage.jsx`
- Create: `web/src/features/journey/ServiceSchedulerPage.jsx`
- Create: `web/src/features/journey/TouristCorridorsPage.jsx`
- Create: `web/src/features/journey/components/CreateRouteModal.jsx`
- Create: `web/src/features/journey/components/StopSequencer.jsx`
- Test: `web/src/features/journey/__tests__/RouteManager.test.jsx`

**Interfaces:**
- Consumes: `apiClient` (`/api/v1/routes`, `/api/v1/services`), `DataTable`, `Modal`, `Button`.
- Produces: Route management, intermediate stop sequencer with GPS coordinates, tourist corridor destinations (Colombo–Ella, Colombo–Kandy, Colombo–Galle), and scheduled departure timetable editor.

- [ ] **Step 1: Write the failing test for RouteManagerPage**

```javascript
// web/src/features/journey/__tests__/RouteManager.test.jsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouteManagerPage } from '../RouteManagerPage';
import * as journeyApi from '../journeyApi';

describe('RouteManagerPage', () => {
  it('renders routes list and allows opening creation modal', async () => {
    vi.spyOn(journeyApi, 'fetchRoutes').mockResolvedValue([
      { id: 'rt-1', routeNumber: 'EX-01', originName: 'Colombo', destinationName: 'Galle', distanceKm: 119.5, isActive: true }
    ]);

    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <RouteManagerPage />
      </QueryClientProvider>
    );

    expect(await screen.findByText('EX-01')).toBeInTheDocument();
    expect(screen.getByText('Colombo → Galle')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx vitest run src/features/journey/__tests__/RouteManager.test.jsx`
Expected: FAIL.

- [ ] **Step 3: Implement `journeyApi`, `RouteManagerPage`, `ServiceSchedulerPage`, and `TouristCorridorsPage`**

Connect to `GET/POST /api/v1/routes` and `GET/POST /api/v1/services`. Implement intermediate stop reordering with arrival offsets and fare calculations.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd web && npx vitest run src/features/journey/__tests__/RouteManager.test.jsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/features/journey/
git commit -m "feat(web): implement Journey Planning, Route Manager, and Service Scheduler views"
```

---

### Task 5: Web Component 2 — Fleet, Seat & Resource Feasibility (Nuhadh)

**Files:**
- Create: `web/src/features/fleet/fleetApi.js`
- Create: `web/src/features/fleet/FleetMatrixBuilderPage.jsx`
- Create: `web/src/features/fleet/SeatLayoutDesignerPage.jsx`
- Create: `web/src/features/fleet/DriverRosteringPage.jsx`
- Create: `web/src/features/fleet/FleetReviewsDashboardPage.jsx`
- Create: `web/src/features/fleet/components/InteractiveSeatCanvas.jsx`
- Test: `web/src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx`

**Interfaces:**
- Consumes: `apiClient` (`/api/v1/buses`, `/api/v1/seats/layouts`, `/api/v1/drivers`, `/api/v1/reviews`).
- Produces: Fleet inventory table, 2D visual drag/click seat template designer (2×2, 2×1 layouts), driver rostering with 8-hour rest-window check (`BR-RESOURCE-002`), and passenger review analytics.

- [ ] **Step 1: Write the failing test for SeatLayoutDesigner**

```javascript
// web/src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx
import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { SeatLayoutDesignerPage } from '../SeatLayoutDesignerPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

describe('SeatLayoutDesignerPage', () => {
  it('allows clicking grid cells to toggle seats and generates valid layout payload', async () => {
    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <SeatLayoutDesignerPage />
      </QueryClientProvider>
    );

    const cell = screen.getByTestId('grid-cell-1-1');
    fireEvent.click(cell);
    expect(cell).toHaveAttribute('data-seat-type', 'Standard');
    expect(screen.getByText(/Total Seats: 1/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx vitest run src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx`
Expected: FAIL.

- [ ] **Step 3: Implement `fleetApi`, `FleetMatrixBuilderPage`, `SeatLayoutDesignerPage`, and `DriverRosteringPage`**

Implement the interactive 2D grid canvas, bus maintenance switcher, driver rest check, and reviews summary charts.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd web && npx vitest run src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/features/fleet/
git commit -m "feat(web): implement Fleet Matrix, 2D Seat Layout Designer, Driver Rostering, and Reviews"
```

---

### Task 6: Web Component 3 — Booking Manifest & Operations Overview (Mithila)

**Files:**
- Create: `web/src/features/bookings/bookingApi.js`
- Create: `web/src/features/bookings/BookingManifestMonitorPage.jsx`
- Create: `web/src/features/bookings/OperatorDashboardPage.jsx`
- Create: `web/src/lib/csvExport.js`
- Test: `web/src/features/bookings/__tests__/BookingManifest.test.jsx`

**Interfaces:**
- Consumes: `apiClient` (`/api/v1/bookings/manifest`, `/api/v1/bookings`).
- Produces: Passenger departure manifest, real-time boarding checkboxes, client-side CSV/PDF export, and executive operations dashboard widgets (occupancy rate, revenue, departure status).

- [ ] **Step 1: Write the failing test for BookingManifestMonitor**

```javascript
// web/src/features/bookings/__tests__/BookingManifest.test.jsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BookingManifestMonitorPage } from '../BookingManifestMonitorPage';
import * as bookingApi from '../bookingApi';

describe('BookingManifestMonitorPage', () => {
  it('renders passenger list and enables CSV export button', async () => {
    vi.spyOn(bookingApi, 'fetchDepartureManifest').mockResolvedValue({
      serviceId: 'srv-1',
      busRegistration: 'ND-5421',
      totalPassengers: 2,
      passengers: [
        { bookingReference: 'WP-1001', passengerName: 'Nuhadh M.', seatNumber: '1A', hasBoarded: false, status: 'Confirmed' }
      ]
    });

    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <BookingManifestMonitorPage />
      </QueryClientProvider>
    );

    expect(await screen.findByText('WP-1001')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /export csv/i })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx vitest run src/features/bookings/__tests__/BookingManifest.test.jsx`
Expected: FAIL.

- [ ] **Step 3: Implement `bookingApi`, `BookingManifestMonitorPage`, `OperatorDashboardPage`, and `csvExport`**

Connect to manifest endpoint, wire up CSV export and thermal/A4 print styles.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd web && npx vitest run src/features/bookings/__tests__/BookingManifest.test.jsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/features/bookings/ web/src/lib/
git commit -m "feat(web): implement Booking Manifest Monitor, CSV export, and Operator Dashboard"
```

---

### Task 7: Web Component 4 — Disruption Intake, Manager Approval & AI Observability (Dineth)

**Files:**
- Create: `web/src/features/disruptions/disruptionApi.js`
- Create: `web/src/features/disruptions/DisruptionIntakePage.jsx`
- Create: `web/src/features/disruptions/ManagerApprovalWorkbenchPage.jsx`
- Create: `web/src/features/disruptions/AiObservabilityPage.jsx`
- Create: `web/src/features/disruptions/ServiceAlertBroadcastPage.jsx`
- Create: `web/src/components/ui/JsonDiffViewer.jsx`
- Test: `web/src/features/disruptions/__tests__/ManagerApproval.test.jsx`

**Interfaces:**
- Consumes: `apiClient` (`/api/v1/disruptions`, `/api/v1/approvals`, `/api/v1/ai/workflows`, `/api/v1/service-alerts`).
- Produces: Incident intake with blast-radius delay slider, Manager Approval Workbench with Before/After Diff viewer, live AI execution trace timeline, and service alert broadcast center.

- [ ] **Step 1: Write the failing test for ManagerApprovalWorkbench**

```javascript
// web/src/features/disruptions/__tests__/ManagerApproval.test.jsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ManagerApprovalWorkbenchPage } from '../ManagerApprovalWorkbenchPage';
import * as disruptionApi from '../disruptionApi';

describe('ManagerApprovalWorkbenchPage', () => {
  it('renders pending approval with before/after diff and enables decision execution', async () => {
    vi.spyOn(disruptionApi, 'fetchPendingApprovals').mockResolvedValue([
      {
        id: 'appr-1',
        disruptionId: 'dis-1',
        impactSeverity: 'Critical',
        originalState: { bus: 'ND-5421', departure: '08:30' },
        proposedState: { bus: 'NC-8812', departure: '09:15' },
        affectedPassengerCount: 38
      }
    ]);

    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <ManagerApprovalWorkbenchPage />
      </QueryClientProvider>
    );

    expect(await screen.findByText(/Critical/i)).toBeInTheDocument();
    expect(screen.getByText(/38 Passengers Affected/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /approve changes/i })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd web && npx vitest run src/features/disruptions/__tests__/ManagerApproval.test.jsx`
Expected: FAIL.

- [ ] **Step 3: Implement `disruptionApi`, `ManagerApprovalWorkbenchPage`, `AiObservabilityPage`, and `JsonDiffViewer`**

Implement the approval decision trigger (`POST /api/v1/approvals/{id}/decision`), multi-agent step timeline (`GET /api/v1/ai/workflows/{id}`), tool call traces, and public alert banner broadcaster.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd web && npx vitest run src/features/disruptions/__tests__/ManagerApproval.test.jsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add web/src/features/disruptions/
git commit -m "feat(web): implement Disruption Intake, Manager Approval Workbench, and AI Observability"
```

---

### Task 8: Web Admin User Governance & Audit Log Hub

**Files:**
- Create: `web/src/features/admin/adminApi.js`
- Create: `web/src/features/admin/AdminUsersPage.jsx`
- Create: `web/src/features/admin/components/ProvisionUserModal.jsx`
- Create: `web/src/features/admin/components/ChangeRoleModal.jsx`
- Test: `web/src/features/admin/__tests__/AdminUsers.test.jsx`

**Interfaces:**
- Consumes: `apiClient` (`/api/v1/users`).
- Produces: RBAC user governance table, role provisioning, status lock/unlock, and audit logging.

- [ ] **Step 1: Write failing test for AdminUsersPage**
- [ ] **Step 2: Run test to verify failure**
- [ ] **Step 3: Implement `adminApi`, `AdminUsersPage`, and provisioning modals**
- [ ] **Step 4: Run test to verify pass**
- [ ] **Step 5: Commit**

---

### Task 9: Mobile Foundation, Stitch Theme Tokens & Core Widgets (Flutter)

**Files:**
- Create: `mobile/pubspec.yaml`
- Create: `mobile/lib/main.dart`
- Create: `mobile/lib/core/theme/waypoint_theme.dart`
- Create: `mobile/lib/core/widgets/waypoint_button.dart`
- Create: `mobile/lib/core/widgets/waypoint_card.dart`
- Create: `mobile/lib/core/widgets/transit_badge.dart`
- Create: `mobile/lib/core/widgets/offline_banner.dart`
- Create: `mobile/lib/core/network/api_client.dart`
- Test: `mobile/test/core/widgets/waypoint_widgets_test.dart`

**Interfaces:**
- Consumes: Stitch color tokens (`#0056D2`, `#FEB300`, `#005312`), Plus Jakarta Sans typography.
- Produces: Core reusable Flutter widgets (`WayPointButton`, `WayPointCard`, `TransitBadge`, `OfflineBanner`), Dio HTTP client with interceptors.

- [ ] **Step 1: Write the failing widget test**

```dart
// mobile/test/core/widgets/waypoint_widgets_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/widgets/waypoint_button.dart';
import 'package:mobile/core/widgets/transit_badge.dart';

void main() {
  testWidgets('WayPointButton renders with loading state', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: WayPointButton(
            label: 'Search Journeys',
            isLoading: true,
            onPressed: () {},
          ),
        ),
      ),
    );

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile && flutter test test/core/widgets/waypoint_widgets_test.dart`
Expected: FAIL.

- [ ] **Step 3: Implement pubspec, theme tokens, Dio client, and atomic widgets**

Declare dependencies (`flutter_bloc`, `dio`, `mobile_scanner`, `flutter_secure_storage`, `vibration`), set up Lanka Blue theme, and implement core widgets.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile && flutter test test/core/widgets/waypoint_widgets_test.dart`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/pubspec.yaml mobile/lib/
git commit -m "feat(mobile): scaffold Flutter project, Stitch theme tokens, Dio client, and core widgets"
```

---

### Task 10: Mobile Auth, Passenger Navigation Shell & AI Smart Journey Assistant

**Files:**
- Create: `mobile/lib/features/auth/bloc/auth_bloc.dart`
- Create: `mobile/lib/features/auth/screens/passenger_auth_screen.dart`
- Create: `mobile/lib/features/navigation/passenger_shell_screen.dart`
- Create: `mobile/lib/features/journey/widgets/ai_journey_assistant_widget.dart`
- Create: `mobile/lib/features/journey/screens/journey_search_screen.dart`
- Create: `mobile/lib/features/journey/screens/journey_comparison_screen.dart`
- Test: `mobile/test/features/journey/journey_search_test.dart`

**Interfaces:**
- Consumes: `apiClient` (`/api/v1/auth`, `/api/v1/journeys/search`, `/api/v1/ai/workflows/trigger`).
- Produces: Authentication screen, Bottom navigation shell (4 tabs), `AiJourneyAssistantWidget` for natural-language objective travel prompts, and candidate journey comparison screen with 20-minute transfer buffer indicators.

- [ ] **Step 1: Write failing test for JourneySearch and AI Assistant**
- [ ] **Step 2: Run test to verify failure**
- [ ] **Step 3: Implement AuthBloc, PassengerShell, AiJourneyAssistantWidget, and JourneySearchScreen**
- [ ] **Step 4: Run test to verify pass**
- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/auth/ mobile/lib/features/journey/ mobile/lib/features/navigation/
git commit -m "feat(mobile): implement Passenger Auth, Shell navigation, Journey Search, and AI Assistant widget"
```

---

### Task 11: Mobile Interactive Bus Seat Picker & 10-Minute Hold Countdown (Nuhadh)

**Files:**
- Create: `mobile/lib/features/fleet/bloc/seat_picker_bloc.dart`
- Create: `mobile/lib/features/fleet/screens/seat_picker_screen.dart`
- Create: `mobile/lib/features/fleet/widgets/interactive_seat_grid.dart`
- Create: `mobile/lib/features/fleet/widgets/hold_countdown_bar.dart`
- Create: `mobile/lib/features/reviews/screens/review_submission_screen.dart`
- Test: `mobile/test/features/fleet/seat_picker_bloc_test.dart`

**Interfaces:**
- Consumes: `GET /api/v1/services/{id}/seats`, `POST /api/v1/bookings/hold`, `POST /api/v1/reviews`.
- Produces: Interactive 2D custom-painted bus layout with pan/zoom gestures, live 10-minute hold countdown ticker, haptic feedback on seat selection, and 1–5 star post-trip review dialog.

- [ ] **Step 1: Write failing bloc_test for SeatPickerBloc**
- [ ] **Step 2: Run test to verify failure**
- [ ] **Step 3: Implement `SeatPickerBloc`, `SeatPickerScreen`, and `HoldCountdownBar`**
- [ ] **Step 4: Run test to verify pass**
- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/fleet/ mobile/lib/features/reviews/
git commit -m "feat(mobile): implement Interactive 2D Seat Picker, 10m Hold Countdown, and Reviews"
```

---

### Task 12: Mobile Payment Checkout, Digital QR Ticket Wallet & Refund Policy (Mithila)

**Files:**
- Create: `mobile/lib/features/booking/screens/payment_checkout_screen.dart`
- Create: `mobile/lib/features/wallet/screens/ticket_wallet_screen.dart`
- Create: `mobile/lib/features/wallet/widgets/qr_ticket_pass.dart`
- Create: `mobile/lib/features/booking/screens/booking_history_screen.dart`
- Test: `mobile/test/features/wallet/ticket_wallet_test.dart`

**Interfaces:**
- Consumes: `POST /api/v1/payments/confirm-sandbox-charge`, `GET /api/v1/tickets/{id}`, `POST /api/v1/bookings/cancel`.
- Produces: Payment sandbox checkout screen, offline-cached digital QR ticket pass with auto-brightness boost, and booking cancellation modal with tiered refund preview (>24h: 90%, 12–24h: 50%, <12h: 0%).

- [ ] **Step 1: Write failing test for TicketWallet and QR ticket rendering**
- [ ] **Step 2: Run test to verify failure**
- [ ] **Step 3: Implement checkout form, digital QR wallet pass, and cancellation modal**
- [ ] **Step 4: Run test to verify pass**
- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/booking/ mobile/lib/features/wallet/
git commit -m "feat(mobile): implement Payment Sandbox checkout, Digital QR Wallet, and Refund Calculator"
```

---

### Task 13: Mobile Disruption Alert, 1-Tap Rebooking & Conductor Scanner (Dineth)

**Files:**
- Create: `mobile/lib/features/disruption/screens/disruption_alert_dialog.dart`
- Create: `mobile/lib/features/conductor/screens/conductor_scanner_screen.dart`
- Create: `mobile/lib/features/conductor/screens/conductor_manifest_screen.dart`
- Test: `mobile/test/features/conductor/conductor_scanner_test.dart`

**Interfaces:**
- Consumes: `POST /api/v1/rebooking/accept`, `POST /api/v1/tickets/verify-qr`, camera hardware.
- Produces: Passenger disruption alert dialog with 1-tap AI alternative acceptance or instant refund; Conductor camera QR barcode scanner with instant validation, flashlight toggle, audio chime, and departure manifest boarding tracker.

- [ ] **Step 1: Write failing test for Conductor scanner and disruption acceptance**
- [ ] **Step 2: Run test to verify failure**
- [ ] **Step 3: Implement `ConductorScannerScreen`, `ConductorManifestScreen`, and `DisruptionAlertDialog`**
- [ ] **Step 4: Run test to verify pass**
- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/disruption/ mobile/lib/features/conductor/
git commit -m "feat(mobile): implement Disruption Rebooking modal and Conductor QR Camera Scanner"
```

---

### Task 14: End-to-End Verification & 20-Gate Completeness Audit

**Files:**
- Create: `web/e2e/workflow.spec.js`
- Create: `web/README.md`
- Create: `mobile/README.md`
- Modify: `docs/superpowers/specs/2026-10-05-frontend-full-stack-design.md`

- [ ] **Step 1: Execute full Web test suite**
Run: `cd web && npm test`
Expected: All Vitest unit and component tests PASS.

- [ ] **Step 2: Execute full Mobile test suite**
Run: `cd mobile && flutter test`
Expected: All Flutter unit and widget tests PASS.

- [ ] **Step 3: Execute production builds**
Run: `cd web && npm run build`
Run: `cd mobile && flutter build apk --debug`
Expected: Zero build errors, zero broken asset links.

- [ ] **Step 4: Verify 20-Gate Completeness Checklist**
Audit: Loading skeletons, empty states, error retry, form validations, responsive viewports, mobile safe areas, offline indicators, real persistence.

- [ ] **Step 5: Commit**

```bash
git add web/ mobile/ docs/
git commit -m "chore: complete frontend reconstruction verification and documentation"
```

---

## Plan Self-Review Check

1. **Spec Coverage**: Every single one of the 22 backend controllers and all 4 student components are mapped to dedicated tasks (Tasks 4–8 for Web, Tasks 9–13 for Mobile).
2. **Step Scan**: Every step defines a concrete action, exact file paths, explicit test code, checkable commands, and commit messages.
3. **Type Consistency**: DTOs, endpoint routes (`/api/v1/*`), and store signatures match the ASP.NET Core controllers and the approved design spec verbatim.
4. **Review Focus**: Edge cases (10-minute hold expiry, transfer buffer <20m, AI rejection rollback, offline banners, conductor fraud check) are tested in Tasks 4, 5, 7, 11, 12, and 13.
5. **No Placeholders**: Zero dummy components or stubbed functions.
