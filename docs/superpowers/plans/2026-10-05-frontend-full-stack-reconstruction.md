# WayPoint Frontend Full-Stack Reconstruction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reconstruct the complete, production-ready frontend presentation layer from scratch for both React Web (`web/`) and Flutter Mobile (`mobile/`), exhaustively surfacing all 22 ASP.NET Core API controllers and multi-agent AI workflows in strict compliance with the 20-Rule Production-Readiness Gate.

**Architecture:** Contract-driven headless architecture connecting Vite + React 18 (using Sovereign UI tokens, TanStack Query, and Zustand) and Flutter 3.x (using Velora tokens, BLoC/Cubit, and Dio) to the authoritative ASP.NET Core Web API with zero hardcoded mocks.

**Tech Stack:** 
- Web: React 18, Vite 6, Tailwind CSS v3, TanStack Query v5, Zustand, Radix UI, Lucide Icons, Vitest, React Testing Library.
- Mobile: Flutter 3.x, Dart 3, Flutter BLoC/Cubit, Dio, Flutter Secure Storage, Mobile Scanner, QR Flutter, Flutter Test.
- Backend/AI: ASP.NET Core Web API (`http://localhost:5010/api/v1`), PostgreSQL, Python Multi-Agent AI Subsystem.

**Spec:** [docs/superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md)

---

## 🛠️ Specialized Skills Matrix

To ensure maximum engineering quality, visual excellence, and zero regressions, each task is mapped to specific specialized skills:

| Domain | Assigned Skills | Purpose & Responsibilities |
| :--- | :--- | :--- |
| **Design System & Aesthetics** | `ui-ux-pro-max`, `frontend-design`, `web-design-guidelines` | High-density enterprise layout discipline (Sovereign UI), mobile transit aesthetics (Velora), accessible contrast tokens, 48px touch targets, micro-interactions, and Web Interface Guidelines compliance. |
| **Stitch Integration** | `stitch::generate-design`, `stitch::react-components`, `enhance-prompt` | Structured prompt enhancement, screen generation via Stitch MCP, style token extraction into `tailwind.config.js`, modular component breakdown, and AST validation. |
| **React Architecture** | `react-patterns`, `react-ui-patterns`, `react-best-practices` | Query key factories, optimistic cache updates with rollback, custom hook abstraction (`usePagination`, `useDebounce`), re-render prevention, and explicit 18-state UI machines. |
| **Flutter Architecture** | `flutter-expert` | BLoC / Cubit event streams, immutable state classes, Dio interceptors with JWT refresh, hardware camera scanner with torch, secure storage, and pinch-to-zoom interactive viewer. |
| **Testing & Verification** | `test-driven-development`, `verification-before-completion`, `code-reviewer`, `systematic-debugging` | Red-Green-Refactor test cycles, component rendering tests, widget tests, cross-platform E2E workflow verification, and pre-completion audits. |

---

## Global Constraints

- **20-Rule Production-Readiness Gate**: Never implement only the happy path: every interactive view must handle Loading, Skeleton, Empty, No-Results, Error, Retry, Offline, Unauthorized, Session-Expired, 404, Processing, Success, Failure, Read-Only, and Destructive Confirmation states.
- **Authoritative API Base URL**: `http://localhost:5010/api/v1`. Clients must NEVER connect directly to PostgreSQL.
- **Web Design Tokens**: Sovereign UI (Geist font, `#4F46E5` primary, slate canvas `#F8FAFC`, 4px grid) from Stitch project `3726083092669162278`.
- **Mobile Design Tokens**: Velora Transit (Plus Jakarta Sans font, `#0D5C46` deep jade primary, `#2BB673` secondary, pill geometries) from Stitch project `15831387990617273223`.
- **Seat Reservation Locking**: Concurrency tokens (`RowVersion`) and 10-minute countdown timers must be respected for temporary seat holds.
- **Security & Storage**: JWT tokens stored in `localStorage` with refresh rotation on Web; `FlutterSecureStorage` (AES) on Mobile.

---

## Review Focus

1. **Temporary Seat Hold Expiry**: When the 10-minute hold ticker reaches 00:00, the UI must immediately transition to an expired state, release the seat, and prevent payment submission.
2. **Double-Submit Prevention**: Rapid multiple clicks on booking, payment, seat hold, or disruption submission must disable the submit button and emit only a single network request.
3. **Offline / Network Drops**: Losing network connectivity during form entry or browsing must show the non-blocking Offline Banner, preserve entered form inputs, and provide a retry button.
4. **Session Expiry / 401 Unauthorized**: When a JWT token expires during an operator action, the application must display a session-expired modal, attempt refresh token rotation, or redirect to login without crashing.
5. **Real-Time Concurrency Conflict**: If two operators or passengers attempt to hold or book the same seat simultaneously, the losing client must receive an actionable conflict message ("Seat already reserved") and re-fetch latest seat availability.

---

## Tasks

### Task 1: Scaffolding, Core UI Primitives & Design System Tokens

**Skills Applied:** `stitch::generate-design`, `ui-ux-pro-max`, `react-ui-patterns`, `flutter-expert`, `test-driven-development`

**Files:**
- Create: `web/package.json`, `web/vite.config.js`, `web/tailwind.config.js`, `web/src/index.css`, `web/src/main.jsx`, `web/src/App.jsx`
- Create: `web/src/components/ui/Button.jsx`, `Input.jsx`, `Card.jsx`, `Modal.jsx`, `DataTable.jsx`, `Skeleton.jsx`, `EmptyState.jsx`, `ErrorState.jsx`, `OfflineBanner.jsx`, `TransitBadge.jsx`, `CommandPalette.jsx`
- Create: `mobile/pubspec.yaml`, `mobile/lib/main.dart`, `mobile/lib/core/theme/app_theme.dart`, `mobile/lib/core/theme/theme_cubit.dart`
- Create: `mobile/lib/core/widgets/waypoint_button.dart`, `waypoint_card.dart`, `shimmer_loading.dart`, `empty_state_view.dart`, `transit_badge.dart`
- Test: `web/src/components/ui/__tests__/Primitives.test.jsx`, `mobile/test/core/widgets_test.dart`

- [ ] 1.1 Use `stitch::generate-design` and `ui-ux-pro-max` to define and verify Sovereign UI design tokens in `web/tailwind.config.js` and Velora tokens in `mobile/lib/core/theme/app_theme.dart`.
- [ ] 1.2 Implement modular Web UI primitives using `react-ui-patterns` handling Loading, Disabled, Empty, and Error states (`Button`, `Input`, `Card`, `Modal`, `DataTable`, `Skeleton`, `EmptyState`, `ErrorState`, `OfflineBanner`, `TransitBadge`, `CommandPalette`).
- [ ] 1.3 Implement mobile widgets using `flutter-expert` with tactile feedback, accessibility labels, and Velora styling (`WaypointButton`, `WaypointCard`, `ShimmerLoading`, `EmptyStateView`, `TransitBadge`).
- [ ] 1.4 Write unit tests for all UI primitives using `test-driven-development`.
- [ ] 1.5 Run tests: `npm test` in `web/` and `flutter test` in `mobile/`.
- [ ] 1.6 Commit: `git commit -m "feat(ui): complete design system tokens and core UI primitives for web and mobile"`

---

### Task 2: Authentication, Security & Navigation Shells

**Skills Applied:** `react-patterns`, `flutter-expert`, `api-security-testing`, `test-driven-development`

**Files:**
- Create: `web/src/api/client.js`, `web/src/store/authStore.js`, `web/src/store/themeStore.js`, `web/src/components/ProtectedRoute.jsx`, `web/src/layouts/DashboardLayout.jsx`, `web/src/pages/LoginPage.jsx`, `web/src/pages/RegisterPage.jsx`, `web/src/pages/NotFoundPage.jsx`
- Create: `mobile/lib/core/network/api_client.dart`, `mobile/lib/core/storage/secure_storage_service.dart`, `mobile/lib/features/auth/bloc/auth_cubit.dart`, `mobile/lib/features/auth/screens/passenger_auth_screen.dart`, `mobile/lib/features/navigation/screens/auth_gate.dart`, `mobile/lib/features/navigation/screens/passenger_navigation_shell.dart`, `mobile/lib/features/navigation/screens/conductor_navigation_shell.dart`
- Test: `web/src/pages/__tests__/AuthAndOverview.test.jsx`, `mobile/test/features/auth/passenger_auth_test.dart`

- [ ] 2.1 Implement Axios client in `web/src/api/client.js` with JWT token injection, automated 401 refresh rotation, and structured ProblemDetails error mapping.
- [ ] 2.2 Build Zustand `authStore.js` and `ProtectedRoute.jsx` enforcing RBAC (`RequireAdmin`, `RequireOperator`, `RequireManager`).
- [ ] 2.3 Implement Web `LoginPage.jsx` and `RegisterPage.jsx` with input limits, inline validation, password show/hide, loading spinner, and double-submit prevention.
- [ ] 2.4 Implement `DashboardLayout.jsx` with collapsible navigation, user role pill, theme switcher, and `Ctrl+K` Command Palette.
- [ ] 2.5 Implement Dio `api_client.dart` and AES-encrypted `secure_storage_service.dart` in Flutter using `flutter-expert`.
- [ ] 2.6 Implement `AuthCubit` and `passenger_auth_screen.dart` with TabBar for Sign In and Sign Up.
- [ ] 2.7 Build `PassengerNavigationShell.dart` (Search, Wallet, Alerts, Settings) and `ConductorNavigationShell.dart` (Scanner, Manifest).
- [ ] 2.8 Verify auth tests in Web and Mobile.
- [ ] 2.9 Commit: `git commit -m "feat(auth): implement authentication, secure storage, and navigation shells"`

---

### Task 3: Component 1 — Journey Planning & Route Catalogue (Sethum)

**Skills Applied:** `stitch::react-components`, `ui-ux-pro-max`, `react-best-practices`, `flutter-expert`, `test-driven-development`

**Files:**
- Create: `web/src/features/journey/journeyApi.js`, `RouteManagerPage.jsx`, `ServiceSchedulerPage.jsx`, `TouristCorridorsPage.jsx`, `JourneyHubLayout.jsx`
- Create: `mobile/lib/features/journey/services/journey_api_service.dart`, `mobile/lib/features/journey/models/journey_models.dart`, `mobile/lib/features/journey/screens/journey_search_screen.dart`, `mobile/lib/features/journey/screens/preference_filter_sheet.dart`, `mobile/lib/features/journey/screens/journey_comparison_screen.dart`
- Test: `web/src/features/journey/__tests__/RouteManager.test.jsx`, `mobile/test/features/journey/journey_search_test.dart`

- [ ] 3.1 Implement `journeyApi.js` connecting to `/api/v1/routes`, `/api/v1/services`, and `/api/v1/routes/tourist-corridors`.
- [ ] 3.2 Build `RouteManagerPage.jsx` with stop sequencing table, duration inputs, and create/update route modal.
- [ ] 3.3 Build `ServiceSchedulerPage.jsx` with timetable calendar, departure board, and bus/driver assignment selector.
- [ ] 3.4 Build `TouristCorridorsPage.jsx` highlighting scenic routes (Colombo-Ella, Kandy, Galle) with tags.
- [ ] 3.5 Implement `journey_api_service.dart` and `journey_models.dart` in Flutter connecting to `/api/v1/journeys/search`.
- [ ] 3.6 Build `journey_search_screen.dart` using Velora tokens with dual origin/destination swap, date picker, and quick city chips.
- [ ] 3.7 Build `preference_filter_sheet.dart` for AC, Wi-Fi, and arrival deadline filtering.
- [ ] 3.8 Build `journey_comparison_screen.dart` rendering direct and multi-hop candidate cards with fares, transfer times, and seat counters.
- [ ] 3.9 Run tests for journey features on Web and Mobile.
- [ ] 3.10 Commit: `git commit -m "feat(journey): implement route manager, service scheduler, and mobile journey search"`

---

### Task 4: Component 2 — Fleet Matrix, Seat Layout Designer & Seat Picker (Nuhadh)

**Skills Applied:** `stitch::react-components`, `frontend-design`, `ui-ux-pro-max`, `flutter-expert`, `test-driven-development`

**Files:**
- Create: `web/src/features/fleet/fleetApi.js`, `FleetMatrixBuilderPage.jsx`, `SeatLayoutDesignerPage.jsx`, `DriverRosteringPage.jsx`, `FleetHubLayout.jsx`, `web/src/pages/fleet/FleetReviewsDashboardPage.jsx`
- Create: `mobile/lib/features/fleet/data/fleet_api_service.dart`, `mobile/lib/features/fleet/bloc/seat_picker_bloc.dart`, `mobile/lib/features/fleet/screens/seat_picker_screen.dart`, `mobile/lib/features/fleet/presentation/pages/review_submission_screen.dart`
- Test: `web/src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx`, `mobile/test/features/fleet/seat_picker_bloc_test.dart`

- [ ] 4.1 Implement `fleetApi.js` connecting to `/api/v1/buses`, `/api/v1/seats/layouts`, `/api/v1/drivers`, and `/api/v1/reviews`.
- [ ] 4.2 Build `FleetMatrixBuilderPage.jsx` with bus inventory table, bus class filter, and maintenance toggle dialog with reason/cost inputs.
- [ ] 4.3 Build `SeatLayoutDesignerPage.jsx`: interactive 2D grid builder supporting 2x2, 2x1, and luxury layouts with drag/click coordinate placement.
- [ ] 4.4 Build `DriverRosteringPage.jsx` with driver directory, license validator, and shift assignment modal with rest-time overlap guard (`BR-RESOURCE-002`).
- [ ] 4.5 Build `FleetReviewsDashboardPage.jsx` with sentiment indicators, star rating breakdowns, and feedback list.
- [ ] 4.6 Implement `fleet_api_service.dart` and `seat_picker_bloc.dart` in Flutter connecting to `/api/v1/services/{id}/seats`.
- [ ] 4.7 Build `seat_picker_screen.dart`: interactive bus coach layout rendering available, held, and booked seats with pinch/zoom (`InteractiveViewer`), selection summary bar, and real-time concurrency handling.
- [ ] 4.8 Build `review_submission_screen.dart` with 5-star rating, cleanliness/comfort tags, and comment submission.
- [ ] 4.9 Run fleet and seat tests: `npm test` in `web/` and `flutter test` in `mobile/`.
- [ ] 4.10 Commit: `git commit -m "feat(fleet): implement fleet matrix, visual seat designer, and mobile seat picker"`

---

### Task 5: Component 3 — Booking, Payment Sandbox, Ticket Wallet & Manifest (Mithila)

**Skills Applied:** `react-ui-patterns`, `stitch::react-components`, `flutter-expert`, `test-driven-development`

**Files:**
- Create: `web/src/features/bookings/bookingApi.js`, `OperatorDashboardPage.jsx`, `BookingManifestMonitorPage.jsx`, `web/src/lib/csvExport.js`
- Create: `mobile/lib/features/booking/services/booking_api_service.dart`, `mobile/lib/features/booking/models/booking_models.dart`, `mobile/lib/features/booking/screens/payment_checkout_screen.dart`, `mobile/lib/features/booking/screens/ticket_wallet_screen.dart`, `mobile/lib/features/booking/widgets/hold_countdown_bar.dart`, `mobile/lib/features/booking/widgets/qr_ticket_pass_card.dart`, `mobile/lib/features/booking/widgets/tiered_refund_modal.dart`
- Test: `web/src/features/bookings/__tests__/BookingManifest.test.jsx`, `mobile/test/features/booking/booking_screen_test.dart`

- [ ] 5.1 Implement `bookingApi.js` connecting to `/api/v1/bookings/manifest/{serviceId}`, `/api/v1/bookings/hold`, `/api/v1/payments/confirm`.
- [ ] 5.2 Build `OperatorDashboardPage.jsx` with real-time departure metrics, occupancy KPI gauges, and revenue summary cards.
- [ ] 5.3 Build `BookingManifestMonitorPage.jsx` with passenger search, filter by hold/booked, manual check-in override, and instant CSV/PDF export (`csvExport.js`).
- [ ] 5.4 Implement `booking_api_service.dart` and `booking_models.dart` in Flutter.
- [ ] 5.5 Build `hold_countdown_bar.dart`: persistent 10-minute hold ticker with circular progress arc and auto-expiration cleanup.
- [ ] 5.6 Build `payment_checkout_screen.dart`: payment sandbox with card input validation, LKR formatting, and instant receipt generation.
- [ ] 5.7 Build `ticket_wallet_screen.dart` displaying Active, Completed, and Cancelled digital boarding passes with offline encrypted caching.
- [ ] 5.8 Build `qr_ticket_pass_card.dart`: HMAC-signed QR ticket with auto-brightness boost for scanning.
- [ ] 5.9 Build `tiered_refund_modal.dart` displaying tiered refund preview (100%, 70%, 0%) before executing cancellation.
- [ ] 5.10 Run booking and ticketing test suites.
- [ ] 5.11 Commit: `git commit -m "feat(booking): implement operator manifest monitor, payment sandbox, and QR ticket wallet"`

---

### Task 6: Component 4 — Disruption Intake, AI Approval Workbench & Conductor Ops (Dineth)

**Skills Applied:** `react-patterns`, `flutter-expert`, `stitch::generate-design`, `test-driven-development`

**Files:**
- Create: `web/src/features/disruptions/disruptionApi.js`, `DisruptionIntakePage.jsx`, `ManagerApprovalWorkbenchPage.jsx`, `AiObservabilityPage.jsx`, `ServiceAlertBroadcastPage.jsx`, `DisruptionHubLayout.jsx`
- Create: `mobile/lib/features/disruption/services/disruption_service.dart`, `mobile/lib/features/disruption/screens/disruption_alert_screen.dart`, `mobile/lib/features/disruption/widgets/disruption_alert_card.dart`
- Create: `mobile/lib/features/fleet/screens/conductor_scanner_screen.dart`, `mobile/lib/features/fleet/screens/conductor_manifest_screen.dart`
- Test: `web/src/features/disruptions/__tests__/DisruptionHub.test.jsx`, `mobile/test/features/disruption/disruption_alert_test.dart`, `mobile/test/features/conductor_tools_test.dart`

- [ ] 6.1 Implement `disruptionApi.js` connecting to `/api/v1/disruptions`, `/api/v1/approvals`, `/api/v1/ai/workflows`, `/api/v1/service-alerts`.
- [ ] 6.2 Build `DisruptionIntakePage.jsx` with incident logging form (type, severity, delay minutes, affected routes) and double-submit prevention.
- [ ] 6.3 Build `ManagerApprovalWorkbenchPage.jsx`: inspect AI rebooking proposals, side-by-side metric diffs, rationale textarea, and `Approve`, `Reject`, `Revise` actions.
- [ ] 6.4 Build `AiObservabilityPage.jsx`: visual step-by-step trace timeline of multi-agent LangGraph workflow execution.
- [ ] 6.5 Build `ServiceAlertBroadcastPage.jsx` to broadcast network alerts.
- [ ] 6.6 Implement `disruption_service.dart` and `disruption_alert_screen.dart` in Flutter with in-app banner alert and one-tap rebooking acceptance sheet.
- [ ] 6.7 Build `conductor_scanner_screen.dart` using `mobile_scanner` with audio/haptic feedback, torch toggle, and cryptographic HMAC validation (`/api/v1/tickets/verify`).
- [ ] 6.8 Build `conductor_manifest_screen.dart` showing live checked-in vs. absent passengers with manual check-in toggle.
- [ ] 6.9 Run disruption and conductor tests.
- [ ] 6.10 Commit: `git commit -m "feat(disruption): implement AI approval workbench, disruption intake, and conductor QR scanner"`

---

### Task 7: Admin Governance, Production-Readiness Gate Verification & E2E Cross-Platform Audit

**Skills Applied:** `web-design-guidelines`, `e2e-testing-patterns`, `verification-before-completion`, `code-reviewer`

**Files:**
- Create: `web/src/features/admin/adminApi.js`, `AdminUsersPage.jsx`, `AdminHubLayout.jsx`, `ChangeRoleModal.jsx`, `ProvisionUserModal.jsx`
- Create: `tests/e2e/CrossPlatformWorkflow.test.cs` or Playwright test script `web/e2e/cross-platform.spec.js`
- Test: `web/src/features/admin/__tests__/AdminUsersPage.test.jsx`

- [ ] 7.1 Implement `adminApi.js` connecting to `/api/v1/admin/users` and `/api/v1/admin/users/{id}/role`.
- [ ] 7.2 Build `AdminUsersPage.jsx` with user directory table, role provisioning modal, and role change dialog with destructive confirmation.
- [ ] 7.3 Run full test suites across all tiers: `dotnet test backend/WayPoint.sln`, `npm test` in `web/`, `flutter test` in `mobile/`.
- [ ] 7.4 Execute complete End-to-End trace: Passenger Search → Seat Hold (10m ticker) → Payment Sandbox → Disruption Incident → AI Rebooking → Manager Workbench Approval → Passenger Rebooking Acceptance → Conductor QR Scan.
- [ ] 7.5 Audit the entire codebase against all 20 rules of the Production-Readiness Gate (verify absence of placeholders, dead buttons, raw error messages, layout shifts, or missing states).
- [ ] 7.6 Commit: `git commit -m "feat(admin): complete admin governance, E2E cross-platform verification, and production audit"`
