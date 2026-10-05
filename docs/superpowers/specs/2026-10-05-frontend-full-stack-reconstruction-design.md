# WayPoint Frontend Full-Stack Reconstruction Design Specification

- **Document Identifier**: `SPEC-2026-10-05-FRONTEND-RECONSTRUCTION`
- **Status**: `Approved / In Implementation`
- **Authors**: Nuhadh (Student 2), Sethum (Student 1), Mithila (Student 3), Dineth (Student 4)
- **Supersedes**: ADR-006 (Restores full multi-tier UI architecture required by SE3090 Assignment 1 marking scheme)

---

## 1. Executive Summary & Purpose

This specification governs the ground-up reconstruction of the **WayPoint** frontend presentation tiers:
1. **React Web Application (`web/`)**: High-density operational workspace for Transport Operators, Dispatchers, Fleet Managers, and Platform Administrators.
2. **Flutter Mobile Application (`mobile/`)**: Native cross-platform application for Passengers (journey discovery, interactive seat booking, ticket wallet, disruption rebooking) and Bus Conductors (cryptographic QR boarding pass scanner and live passenger manifest).

Both client tiers connect directly to the authoritative **ASP.NET Core Web API** (`backend/`) and consume the multi-agent **Python AI Subsystem** (`ai/`) through contract-governed REST API endpoints and telemetry streams.

---

## 2. The 20-Rule Production-Readiness & Completeness Gate

Every screen, feature, and workflow implemented across `web/` and `mobile/` MUST strictly satisfy all 20 rules of the Production-Readiness Gate:

1. **Never Implement Only the Happy Path**: Explicitly handle Loading, Skeletons, Empty, First-Use, No-Results, Error, Retry, Offline, Reconnecting, Permission-Denied, Unauthorized, Session-Expired, 404, Processing, Success, Failure, Disabled/Read-Only, Destructive-Action Confirmation, and Unsaved-Changes.
2. **Handle Failure As Carefully As Success**: No raw stack traces or ugly technical errors; provide actionable recovery steps and retry with backoff.
3. **Build Real Forms**: Input limits, required/optional indicators, client-side + server-side validation, inline messages, password toggle, double-submit prevention.
4. **Security is Not Optional**: Strict JWT rotation, role-based route guards, input sanitization, secure storage, zero hardcoded secrets.
5. **Make Web UI Actually Responsive**: Fluid layouts across mobile (<640px), tablet (640-1024px), laptop, and ultrawide (1600px+) without horizontal scroll.
6. **Make Mobile UI Actually Mobile**: Safe areas, Android/iOS back navigation, pull-to-refresh, haptics, lifecycle resume, orientation tolerance.
7. **Accessibility Must Be Considered**: High-contrast ratios, visible focus indicators, semantic landmarks, aria attributes, minimum 48px touch targets.
8. **Data Must Be Correct**: No hardcoded mocks masquerading as real data; full synchronization with PostgreSQL via EF Core models.
9. **Search, Lists, Tables & Files Must Be Production-Ready**: Debounced search, sorting, filtering, clear search button, pagination, export capability.
10. **Performance Matters**: Request deduplication, TanStack Query caching, list virtualization, memoized renders, zero memory leaks.
11. **Navigation Must Survive Real Users**: Direct deep-linking, browser back/forward, route guards, persistent session restoration.
12. **UX Must Communicate**: Instant feedback on hover, tap, submit, and error with purposeful animations.
13. **Do Not Accept Placeholder-Quality UX**: Zero dead buttons, placeholder copy, fake hrefs, or inconsistent styling tokens.
14. **Test Like a Hostile User**: Rapid multi-clicking, empty submits, emojis, huge numbers, offline disconnection, and tab switching.
15. **Verify Backend + Frontend Together**: Complete trace: UI → API → Database / AI → Response → UI State.
16. **Test Build and Deployment**: Linting, type checking, unit tests, widget tests, and production bundling.
17. **Review the Whole System**: Eliminate dead code, unused imports, duplicate helpers, and ensure clean architecture.
18. **Document Important Behavior**: Clear component contracts, environment configs, and testing guides.
19. **Enforce the 20-Point Completion Checklist**: Verify all checks before declaring any task complete.
20. **Optimize for Real Human Utility**: The system must be predictable, resilient, and delightful for operators, passengers, and conductors.

---

## 3. Technology Stack & Design System Tokens

### 3.1 React Web Application (`web/`)
- **Core Framework**: React 18 with Vite 6 (JavaScript/JSX with JSDoc types & strict PropTypes)
- **Styling**: Tailwind CSS v3 with **Sovereign UI** design tokens (Google Stitch project `3726083092669162278` / `assets/d6c41fc8669b4bee88a2ead79be051d5`)
  - Primary: `#4F46E5` (Indigo)
  - Surface: `#F8FAFC` (Canvas), `#FFFFFF` (Panels), `#0F172A` (Text)
  - Semantic: `#10B981` (Active/Success), `#F59E0B` (Warning/Pending), `#F43F5E` (Danger/Disrupted)
  - Typography: `Geist` (Interface & Headers), `JetBrains Mono` (UUIDs, Coordinates, Fares, Status Tags)
- **Server State & Caching**: TanStack Query v5 (`@tanstack/react-query`) with query key factories
- **Client State**: Zustand (`authStore`, `themeStore`, `disruptionStore`, `manifestStore`)
- **Navigation**: React Router v6 with protected role-based layouts (`DashboardLayout`, `AdminHubLayout`)
- **Icons**: Lucide React + custom SVG transit markers
- **Testing**: Vitest + React Testing Library + MSW

### 3.2 Flutter Mobile Application (`mobile/`)
- **Framework**: Flutter 3.x / Dart 3
- **Styling**: **Velora Transit** design tokens (Google Stitch project `15831387990617273223`)
  - Primary: `#0D5C46` (Deep Jade), Secondary: `#2BB673` (Fresh Emerald), Accent: `#E07A5F` (Terracotta)
  - Canvas: `#FBF9F4` (Warm Alabaster), Surface: `#FFFFFF`, Slate: `#1C2526`
  - Typography: `Plus Jakarta Sans` (Display/Headings), `Space Grotesk` (Tabular metadata, seat numbers)
- **State Management**: Flutter BLoC / Cubit (`AuthCubit`, `JourneySearchBloc`, `SeatPickerBloc`, `TicketWalletBloc`, `DisruptionBloc`, `ScannerBloc`)
- **Networking**: Dio with JWT authentication interceptor and retry policies
- **Secure Storage**: `flutter_secure_storage` (encrypted tokens) & `shared_preferences` (offline tickets)
- **Device Capabilities**: `mobile_scanner` (camera QR scanning), `qr_flutter` (HMAC QR generation), `vibration` (haptics)
- **Testing**: `flutter_test` + `bloc_test` + `mocktail`

---

## 4. Component Scope & Backend Endpoint Parity

### Component 1: Journey Planning & Route Catalogue (Student 1 - Sethum)
- **Web**:
  - `RouteManagerPage.jsx`: Interactive route directory, stop sequencer, distance/duration editor (`/api/v1/routes`).
  - `ServiceSchedulerPage.jsx`: Departure/arrival timetable editor, bus/driver schedule linking (`/api/v1/services`).
  - `TouristCorridorsPage.jsx`: Scenic corridor showcase and promotional route tagging (`/api/v1/routes/tourist-corridors`).
- **Mobile**:
  - `JourneySearchScreen.dart`: Multi-criteria search (origin, destination, date, AC, Wi-Fi) (`/api/v1/journeys/search`).
  - `JourneyComparisonScreen.dart`: Direct vs. multi-hop candidate cards with fares, transfer times, and seat counters.

### Component 2: Fleet, Seat & Resource Feasibility (Student 2 - Nuhadh)
- **Web**:
  - `FleetMatrixBuilderPage.jsx`: Bus inventory, registration, capacity, luxury/standard class, maintenance toggle (`/api/v1/buses`).
  - `SeatLayoutDesignerPage.jsx`: Interactive 2D drag/grid seat map designer with row/column coordinates (`/api/v1/seats/layouts`).
  - `DriverRosteringPage.jsx`: Driver directory, license validation, departure assignment with rest-hour compliance (`/api/v1/drivers`).
  - `FleetReviewsDashboardPage.jsx`: Customer sentiment, fleet ratings, and issue flags (`/api/v1/reviews`).
- **Mobile**:
  - `SeatPickerScreen.dart`: Real-time 2D bus seat picker rendering available, held, and booked seats with multi-seat selection (`/api/v1/services/{id}/seats`).
  - `ReviewSubmissionScreen.dart`: Post-journey 5-star rating, cleanliness/punctuality tags, and feedback form (`/api/v1/reviews`).

### Component 3: Booking, Ticketing & Passenger Manifest (Student 3 - Mithila)
- **Web**:
  - `OperatorDashboardPage.jsx`: Departure boards, live occupancy KPIs, and revenue analytics.
  - `BookingManifestMonitorPage.jsx`: Real-time passenger manifest, check-in status, search/filter, and instant CSV/PDF export (`/api/v1/bookings/manifest/{serviceId}`).
- **Mobile**:
  - `HoldCountdownBar.dart`: Persistent 10-minute temporary seat reservation countdown ticker (`/api/v1/bookings/hold`).
  - `PaymentCheckoutScreen.dart`: Simulated card payment sandbox with LKR billing (`/api/v1/payments/confirm`).
  - `TicketWalletScreen.dart`: Active, completed, and cancelled tickets with offline local storage.
  - `QrTicketPassCard.dart`: HMAC-signed cryptographically verifiable QR ticket (`/api/v1/tickets/{id}`).
  - `TieredRefundModal.dart`: Live cancellation preview with tiered refund breakdown (`/api/v1/bookings/cancel`).

### Component 4: Disruption, Rebooking & AI Ops (Student 4 - Dineth)
- **Web**:
  - `DisruptionIntakePage.jsx`: Incident logging (breakdown, weather, roadblocks) with affected service impact (`/api/v1/disruptions`).
  - `ManagerApprovalWorkbenchPage.jsx`: Human-in-the-loop triage; inspect AI proposals, metric diffs, enter rationale comments, execute `Approve`, `Reject`, or `Revise` (`/api/v1/approvals`).
  - `AiObservabilityPage.jsx`: Step-by-step visual trace of multi-agent LangGraph workflow execution (`/api/v1/ai/workflows`).
  - `ServiceAlertBroadcastPage.jsx`: Network-wide emergency broadcast banner composer (`/api/v1/service-alerts`).
- **Mobile**:
  - `DisruptionAlertScreen.dart`: In-app push notification banner for affected passengers.
  - `AiRebookingSheet.dart`: One-tap comparison showing original vs. replacement bus/seat with fare waiver acceptance (`/api/v1/rebooking/accept`).
  - `ConductorScannerScreen.dart`: Hardware camera QR scanner with audio/haptic feedback and offline cryptographic verification (`/api/v1/tickets/verify`).
  - `ConductorManifestScreen.dart`: Service passenger checklist with manual boarding toggle.

### Cross-Cutting & Governance (Shared)
- **Web**:
  - `LoginPage.jsx` & `RegisterPage.jsx`: JWT authentication, token refresh, and role redirection (`/api/v1/auth`).
  - `AdminUsersPage.jsx`: User provisioning, role governance (`Admin`, `TransportManager`, `TransportOperator`) (`/api/v1/admin/users`).
  - `CommandPalette.jsx`: Universal `Ctrl+K` search navigation.
  - `ThemeStore.js`: Dark/Light theme toggle with zero-flicker persistence.
  - `OfflineBanner.jsx`: Network connectivity loss detector.
- **Mobile**:
  - `PassengerAuthScreen.dart`: Sign in / sign up with token caching.
  - `PassengerNavigationShell.dart` & `ConductorNavigationShell.dart`: Role-tailored navigation shells.
  - `PassengerSettingsScreen.dart`: Theme and notification preferences.
