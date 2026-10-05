# WayPoint Full-Stack Frontend System Design Specification
**Web (React + Vite) & Mobile (Flutter) Applications**

- **Date**: 2026-10-05
- **Author**: Antigravity & Nuhadh (Student 2 - Fleet & Resource Lead)
- **Status**: Validated Design Draft (Pending Final User Approval)
- **Target Modules**: `web/` (React 18 + Vite) & `mobile/` (Flutter 3.x)
- **Authoritative Backend**: ASP.NET Core Web API (`http://localhost:5010/api/v1`) & PostgreSQL 18
- **AI Subsystem**: Multi-Agent LangGraph (4 Distinct Specialized Domain Agents)
- **Design System Authority**: Google Stitch Design System (`assets/0a9e5af03d7d4795a3ce2e1cd7f5d6f9`, `projects/15831387990617273223`)

---

## 1. Executive Summary & Architectural Mandate

This specification governs the ground-up development of the two client presentation applications for the **WayPoint** AI-Powered Intercity Transit Platform:
1. **React Web Application (`web/`)**: High-density Operations Hub and Control Room for Transport Operators, Dispatchers, Transport Managers, and System Administrators.
2. **Flutter Mobile Application (`mobile/`)**: High-performance, tactile cross-platform client for Sri Lankan transit Passengers and on-board Bus Conductors.

### 1.1 Strict Compliance Invariants
- **SE3090 Integrated Architecture Rule (`REQ-ASSIGN-04`)**: Disconnected prototypes are forbidden. Both Web and Mobile consume the authoritative ASP.NET Core REST API (`http://localhost:5010/api/v1`), sharing authentication JWTs, database records, and server-side validation invariants.
- **Equal 4-Student Ownership (`REQ-INDIV-01`, `REQ-INDIV-03`)**: Every student owns their slice across backend, database, React web, Flutter mobile, AI agents, and automated tests (10 marks React + 10 marks Flutter per student).
- **Vibe-Code Production-Readiness Gate (20 Rules)**: Zero "happy-path only" shortcuts. Every screen, query, and form must implement complete finite state machines (Loading, Skeleton, Empty, Error with Retry, Offline, Unauthorized, Session Expired, Double-Submit Lock, Unsaved Changes Guard).

---

## 2. Technology Stack & Framework Architecture

### 2.1 React Web Application (`web/`)
- **Runtime & Bundler**: React 18 with Vite (Fast HMR, modern ES modules).
- **Styling & Tokens**: Tailwind CSS v3 with custom design system extensions synchronized with Google Stitch.
- **Server State Management**: **TanStack Query (React Query) v5** with query-key factories, automated cache invalidation on mutations (`staleTime: 5000` for real-time endpoints), and background refetching.
- **Client UI State Management**: **Zustand** for lightweight UI state (Command Palette modal state, drawer toggles, theme switcher, active canvas tool).
- **HTTP Client**: Centralized Axios client (`web/src/api/client.js`) with request interceptors (Bearer token injection) and response interceptors (401 auto-logout, 403 forbidden toast, standardized error object extraction).
- **Icons**: Lucide React.
- **Testing**: Vitest + React Testing Library + MSW (Mock Service Worker) for component and role tests.

### 2.2 Flutter Mobile Application (`mobile/`)
- **SDK & Language**: Flutter 3.x with Dart 3 (null safety, pattern matching, records).
- **Architecture**: Feature-First Clean Architecture (`core/`, `features/{feature_name}/{data, domain, presentation}`).
- **State Management**: **Flutter BLoC & Cubit** (`flutter_bloc`) enforcing immutable state classes (`Initial`, `Loading`, `Success`, `Error`, `Empty`).
- **HTTP Client**: **Dio** with interceptors, connection timeouts (10s connect, 15s receive), and automatic header management.
- **Secure Persistence**: `flutter_secure_storage` for JWT tokens and refresh credentials.
- **Hardware Integrations**:
  - `mobile_scanner`: High-speed camera barcode/QR scanner for Bus Conductor verification.
  - `vibration` / `services.HapticFeedback`: Tactile haptic feedback on seat selection, hold timeout alerts, and scan confirmation.
  - `screen_brightness`: Auto-brightness boost to 100% on ticket QR display.
- **Testing**: Flutter Test + `bloc_test` + Mockito for unit and widget test suites.

---

## 3. Design System & Visual Language (Google Stitch)

Both applications adhere strictly to the **WayPoint Stitch Design System**:
- **Brand Tokens**:
  - `Lanka Blue` (`#0056D2` / `waypoint-blue`): Authority, maritime legacy, primary actions, active navigation.
  - `Sunset Amber` (`#FEB300` / `waypoint-amber`): Kinetic urgency, pending approval badges, temporary seat hold timers.
  - `Jungle Green` (`#005312` / `waypoint-green`): Eco-transit, verified bookings, available seats, conductor validation success.
  - `Coral Red` (`#BA1A1A` / `waypoint-red`): Critical disruptions, cancellations, seat locks, error states.
  - `Surface Canvas`: Web `#F8FAFC` (Slate Canvas) / `#FFFFFF` (Surface); Mobile `#FBF9F4` (Warm Alabaster) / `#1C2526` (Deep Charcoal typography).
- **Typography**:
  - Headings: **Plus Jakarta Sans** (SemiBold/Bold, tight letter spacing `-0.015em`).
  - Body & Tables: **Inter** (Regular/Medium, optimized for high data density).
  - Transit & Technical Codes: **Space Grotesk** / **JetBrains Mono** (Seat numbers, timestamps, route codes, UUIDs).

---

## 4. Exhaustive Backend API ➔ Frontend Screen Coverage Matrix

Every single one of the 22 ASP.NET Core controllers and Python AI capabilities is fully mapped to user-facing screens and workflows:

| Controller | HTTP Methods & Routes | Frontend Platform | Screen / Feature Component | Functional Purpose |
|---|---|---|---|---|
| **`AuthController`** | `POST /register`<br>`POST /login`<br>`GET /me`<br>`POST /change-password`<br>`POST /refresh-token` | Web & Mobile | `LoginPage`<br>`RegisterPage`<br>`ProfileModal` / `ProfileScreen` | User authentication, token issuance, secure storage, auto-refresh on 401, profile display. |
| **`UserController`** | `GET /users`<br>`POST /users/provision`<br>`PUT /users/{id}/role`<br>`PUT /users/{id}/status`<br>`DELETE /users/{id}` | Web | `AdminUsersPage`<br>`ProvisionUserModal`<br>`ChangeRoleModal` | Administrator governance: RBAC role provisioning (`Admin`, `TransportManager`, `Operator`, `Passenger`), account lock/unlock, user deletion. |
| **`RouteController`** | `GET /routes`<br>`POST /routes`<br>`GET /routes/{id}`<br>`PUT /routes/{id}`<br>`DELETE /routes/{id}`<br>`GET /routes/corridors` | Web | `RouteManagerPage`<br>`RouteEditorModal`<br>`TouristCorridorsPage` | Student 1 (Sethum): Route catalogue, ordered intermediate stops with offset minutes, GPS boarding points, tourist corridor tags. |
| **`ServiceController`** | `GET /services`<br>`POST /services`<br>`GET /services/{id}`<br>`PUT /services/{id}`<br>`DELETE /services/{id}` | Web | `ServiceSchedulerPage`<br>`ScheduleServiceModal` | Student 1 (Sethum): Scheduled service departures, timetable frequency, linking route, bus, driver, and seasonal fare rules. |
| **`JourneySearchController`** | `POST /journeys/search`<br>`POST /journeys/rank` | Web & Mobile | **Web**: `JourneySearchDebugger`<br>**Mobile**: `JourneySearchScreen`<br>`JourneyResultsScreen` | Student 1 (Sethum): Multimodal intercity search, transfer feasibility calculation (minimum 20-min buffer), preference-aware scoring (cheapest, fastest, AC, direct). |
| **`BusController`** | `GET /buses`<br>`POST /buses`<br>`GET /buses/{id}`<br>`PUT /buses/{id}/maintenance` | Web | `FleetMatrixPage`<br>`RegisterBusModal`<br>`MaintenanceHistoryModal` | Student 2 (Nuhadh): Bus inventory directory, registration, vehicle class (Luxury, Standard), maintenance status toggle with cost logging. |
| **`SeatLayoutController`** | `GET /seats/layouts`<br>`POST /seats/layouts`<br>`GET /seats/layouts/{id}` | Web | `SeatLayoutDesignerPage` | Student 2 (Nuhadh): 2D Visual Seat Layout Canvas builder (2×2, 2×1, VIP Express), row/column grid editor, accessibility tags. |
| **`SeatAvailabilityController`** | `GET /services/{id}/seats` | Web & Mobile | **Web**: `SeatOccupancyViewer`<br>**Mobile**: `SeatPickerScreen` | Student 2 (Nuhadh): Real-time seat inventory calculation displaying live states: `Available`, `Held`, `Booked`, `Blocked`. |
| **`SeatHoldController`** | `POST /bookings/hold`<br>`POST /bookings/hold/release` | Mobile | `SeatHoldBar`<br>`CheckoutHoldScreen` | Student 3 (Mithila): Atomic 10-minute temporary seat hold reservation with live countdown bar, conflict detection (409), and release. |
| **`PaymentController`** | `POST /payments/confirm-sandbox-charge` | Mobile | `PaymentCheckoutModal`<br>`PaymentSandboxScreen` | Student 3 (Mithila): Transactional payment sandbox authorization, simulated card/wallet charge, failure toggle, booking confirmation. |
| **`BookingController`** | `GET /bookings`<br>`GET /bookings/{id}`<br>`GET /bookings/manifest`<br>`POST /bookings/cancel` | Web & Mobile | **Web**: `BookingManifestPage`<br>**Mobile**: `MyBookingsScreen`<br>`BookingDetailScreen`<br>`CancelBookingModal` | Student 3 (Mithila):<br>- Web: Real-time passenger departure manifest, boarding status tracker, CSV export.<br>- Mobile: Booking history, cancellation with refund preview. |
| **`TicketController`** | `GET /tickets/{id}`<br>`POST /tickets/verify-qr` | Mobile | **Passenger**: `TicketWalletScreen`<br>**Conductor**: `ConductorScannerScreen` | Student 3 (Mithila):<br>- Passenger: Digital QR e-ticket with HMAC-SHA256 payload and offline display.<br>- Conductor: Camera QR boarding verification scanner. |
| **`DriverController`** | `GET /drivers`<br>`POST /drivers`<br>`POST /drivers/assign` | Web | `DriverRosteringPage`<br>`AddDriverModal`<br>`AssignShiftModal` | Student 2 (Nuhadh): Driver directory, license verification, shift assignments with mandatory 8-hour rest period validation (`BR-RESOURCE-002`). |
| **`ResourceFeasibilityController`** | `POST /feasibility/evaluate-replacement` | Web | `ResourceFeasibilityDrawer`<br>`DisruptionIntakePage` | Student 2 (Nuhadh): Disruption replacement solver checking unassigned fleet buses, seat capacity matching, and driver rest compliance. |
| **`ReviewController`** | `POST /reviews`<br>`GET /reviews/service/{id}`<br>`GET /reviews/driver/{id}`<br>`GET /reviews/bus/{id}`<br>`DELETE /reviews/{id}` | Web & Mobile | **Mobile**: `ReviewSubmissionModal`<br>**Web**: `FleetReviewsPage` | Student 2 (Nuhadh):<br>- Mobile: Post-trip driver and bus review (1–5 stars, anonymous toggle, 7-day rule, profanity sanitization).<br>- Web: Fleet review analytics. |
| **`DisruptionController`** | `POST /disruptions`<br>`GET /disruptions`<br>`GET /disruptions/{id}`<br>`GET /disruptions/{id}/impact` | Web | `DisruptionIntakePage`<br>`DisruptionBlastRadiusModal` | Student 4 (Dineth): Incident logging (bus breakdown, weather, roadblocks), automated affected passenger blast-radius calculation. |
| **`RebookingController`** | `POST /rebooking/generate-proposal`<br>`POST /rebooking/accept`<br>`POST /rebooking/refund` | Web & Mobile | **Web**: `RebookingReviewDrawer`<br>**Mobile**: `DisruptionAlertBanner`<br>`RebookingProposalDialog` | Student 4 (Dineth): Multi-agent AI rebooking proposal generation; Passenger 1-tap alternative journey acceptance or 100% refund. |
| **`ApprovalController`** | `GET /approvals/pending`<br>`POST /approvals/{id}/decision` | Web | `ManagerApprovalPage`<br>`ApprovalDecisionModal` | Student 4 (Dineth): Transport Manager Approval Workbench. Inspects before/after passenger impact and executes cryptographic `Approve`/`Reject`/`Revise`. |
| **`AiWorkflowController`** | `GET /ai/workflows`<br>`GET /ai/workflows/{id}`<br>`POST /ai/workflows/trigger` | Web | `AiObservabilityPage`<br>`WorkflowExecutionTimeline` | Student 4 (Dineth): AI Observability Dashboard displaying 4-agent LangGraph execution states, tool invocation logs, timings, and safe-failure recovery. |
| **`NotificationController`** | `GET /notifications`<br>`PUT /notifications/{id}/read` | Web & Mobile | `NotificationBellPopover`<br>`NotificationsScreen` | Real-time in-app notification center for disruption alerts, booking confirmations, and timetable modifications. |
| **`ServiceAlertController`** | `GET /service-alerts`<br>`POST /service-alerts` | Web & Mobile | `ServiceAlertBroadcastBanner`<br>`ServiceAlertManagerModal` | Network-wide public service alert broadcast across website top-bar and mobile home feed. |
| **`DevController`** | `POST /dev/seed`<br>`GET /dev/health` | Web | `DevToolboxDrawer` | Developer debugging drawer for quick database reseeding, system health telemetry, and test role switching. |

---

## 5. Detailed Screen & Component Specifications

### 5.1 Web Application Architecture (`web/`)

#### Layout Shell: `DashboardLayout.jsx`
- **Sidebar**: Collapsible navigation rail (expanded: 240px, collapsed: 64px) with role-filtered links:
  - *All Operators*: Overview, Routes, Services, Fleet, Drivers, Manifests, Disruptions.
  - *Transport Managers*: Manager Approval Workbench, AI Observability Hub.
  - *Administrators*: User Governance, Audit Logs, Dev Sandbox.
- **Top Bar**:
  - Global Search / Command Palette shortcut (`Ctrl+K` / `Cmd+K`).
  - Active Disruption Indicator pill (flashes amber/red when pending approvals or critical disruptions exist).
  - Notification Bell with unread badge count.
  - Dark / Light / High-Contrast Theme Switcher.
  - User Avatar with role pill (`TransportManager`, `Operator`, `Admin`) and Logout action.
- **Breadcrumbs**: Dynamic hierarchical path navigation (`Fleet > Seat Layout Designer`).
- **Offline Banner**: Persistent amber alert bar appearing instantly when browser loses connection.

#### Screen Details
1. **`OverviewPage.jsx`**:
   - High-level KPI widgets: Active Fleet (e.g., 28/32 buses), Network Occupancy (84.2%), Pending Approvals (3 requiring manager action), Today's Scheduled Departures (42).
   - Recent Disruption Feed with direct link to Disruption Intake.
   - Departure Timeline: Real-time ticker of buses departing within the next 2 hours.
2. **`RouteManagerPage.jsx` & `TouristCorridorsPage.jsx` (Sethum)**:
   - Data table of routes with origin, destination, distance (km), estimated duration, and tourist corridor flag.
   - Interactive Stop Sequencer: Re-order intermediate stops, configure arrival/departure offset minutes, and input GPS coordinates.
   - Tourist Highlights Drawer: Colombo–Ella, Colombo–Kandy, Colombo–Galle, Colombo–Nuwara Eliya corridor filters.
3. **`ServiceSchedulerPage.jsx` (Sethum)**:
   - Master timetable view grouped by route.
   - New Departure Scheduler: Selects route, assigns bus from available fleet, assigns rested driver, sets departure time, and configures base fare.
4. **`FleetMatrixPage.jsx` (Nuhadh)**:
   - Bus inventory grid: Registration number, bus class (Luxury/Standard), seat capacity, assigned layout, and maintenance badge.
   - Maintenance Drawer: Toggle bus maintenance status with mandatory reason and cost logging; blocks assignment to active services.
5. **`SeatLayoutDesignerPage.jsx` (Nuhadh - Selected Frontend-Only Feature W1)**:
   - Interactive 2D Visual Seat Layout Canvas: Click or drag to define rows (1–15) and columns (1–4).
   - Toggle Aisle positions, Driver Cabin indicator, Emergency Exits, and Accessible/Wheelchair seats.
   - Real-time JSON schema preview and validation.
6. **`DriverRosteringPage.jsx` (Nuhadh)**:
   - Driver roster table with license number, status (Active, Off-Duty, On-Trip), and recent trip history.
   - Shift Assignment Modal with automated 8-Hour Rest Period validation (`BR-RESOURCE-002`); displays error alert if rest window is violated.
7. **`FleetReviewsPage.jsx` (Nuhadh)**:
   - Aggregated ratings distribution (1–5 stars) for buses and drivers.
   - Filterable review comments with profanity-masking indicators and anonymous badge display.
8. **`BookingManifestPage.jsx` (Mithila - Selected Frontend-Only Feature W4)**:
   - Live departure passenger manifest filtered by service.
   - Real-time boarding status toggles (`Booked`, `Boarded`, `No-Show`).
   - One-click CSV Export and Print-Friendly PDF Manifest with bus company header.
9. **`DisruptionIntakePage.jsx` (Dineth - Selected Frontend-Only Feature W6)**:
   - Disruption event intake form: select service, disruption type (Mechanical, Weather, Roadblock, Driver Illness), severity level.
   - Interactive Blast-Radius Simulator Slider: Drag delay minutes to preview affected passengers and connecting transfers before logging.
   - One-click trigger for Multi-Agent AI Rebooking Proposal.
10. **`ManagerApprovalPage.jsx` (Dineth - Selected Frontend-Only Feature W5)**:
    - Queue of pending high-impact operational changes (`PendingManagerApproval`).
    - Visual Before/After JSON Diff Inspector: Color-coded comparison showing original service/bus/driver vs proposed remedy.
    - Affected passenger summary card (count, compensation liability, travel time delta).
    - Cryptographic approval action: `Approve` (executes DB transaction), `Reject` (cancels proposal), `Request Revision` (with mandatory notes).
11. **`AiObservabilityPage.jsx` (Dineth)**:
    - Execution timeline of multi-agent LangGraph workflow.
    - Trace breakdown of all 4 agents: Journey Analysis, Resource Feasibility, Booking Options, and Validation & Safety.
    - Tool invocation inspector: Displays input arguments and returned JSON for all 10 allow-listed tools.
    - Execution metrics: per-step latency (ms), token usage, deterministic validation rule passes, safe-failure recovery logs.
12. **`AdminUsersPage.jsx`**:
    - Complete user directory with search, role filters, and pagination.
    - User provisioning modal with password strength meter and role selector (`Admin`, `TransportManager`, `Operator`, `Passenger`).
    - Status toggle (Active / Suspended) with confirmation modal.

---

### 5.2 Mobile Application Architecture (`mobile/`)

#### Navigation Architecture: `WayPointNavigationShell.dart`
- **Bottom Navigation Bar** with 4 core tabs:
  - **Tab 1: Search & Explore** (`JourneySearchScreen.dart`)
  - **Tab 2: My Bookings & Tickets** (`TicketWalletScreen.dart` & `BookingHistoryScreen.dart`)
  - **Tab 3: Service Alerts & Disruptions** (`ServiceAlertsScreen.dart`)
  - **Tab 4: Profile & Settings** (`ProfileScreen.dart`)
- **Conductor Scanner Mode Action Button**: Prominent top-right app bar button (visible when logged in as Conductor or Operator) that immediately opens `ConductorScannerScreen.dart`.

#### Screen Details
1. **`JourneySearchScreen.dart` & `JourneyResultsScreen.dart` (Sethum)**:
   - Origin and Destination city selector with Sri Lankan transit hubs (Colombo Fort, Kandy Goodshed, Ella, Galle, Nuwara Eliya).
   - Date picker, time-of-day filter, and passenger count.
   - Candidate Journey Cards: Departure/Arrival times, duration, direct vs connecting badge, and transfer window indicator (enforcing 20-min minimum buffer).
   - Filter chips: Lowest Fare, Fastest Route, AC, Free Wi-Fi, Reclining Seats.
2. **`SeatPickerScreen.dart` (Nuhadh - Selected Frontend-Only Feature M4 & M2)**:
   - Custom-painted 2D Bus Seat Layout with smooth gesture zoom and pan.
   - Visual orientation: Front windshield, driver steering wheel, entry door.
   - Dynamic seat states: `Available` (green outline), `Selected` (solid green), `Held` (amber with clock), `Booked` (subtle gray with lock).
   - Tactile Haptic Feedback (`M2`): Light tap on selection, double vibration on tapped occupied seat.
   - Sticky bottom bar showing selected seats, total LKR fare, and "Hold Seats (10 Mins)" CTA button.
3. **`CheckoutHoldScreen.dart` & `PaymentSandboxScreen.dart` (Mithila)**:
   - 10-Minute Seat Hold Countdown Widget (`M3`): Animated countdown bar that updates every second; buzzes phone when 2 minutes remain.
   - Summary card: Route, departure, seat numbers, fare breakdown.
   - Payment Sandbox Drawer: Toggle mock payment methods (Credit/Debit Card, Genie, FriMi, eZ Cash), optional simulated failure toggle to verify rollback.
   - Successful payment triggers single-transaction confirmation and redirects to Ticket Wallet.
4. **`TicketWalletScreen.dart` (Mithila - Selected Frontend-Only Feature M1 & M3)**:
   - Dynamic Hero Card: Departure Countdown ("Departs in 2h 45m from Colombo Fort Bay 4").
   - Digital E-Ticket Stub: Scalloped perforation border, route, passenger name, seat numbers, fare.
   - Cryptographically Signed HMAC-SHA256 QR Code: Offline cached ticket vault; opening the ticket automatically boosts screen brightness to 100% (`M1`) for instant scanning.
5. **`BookingHistoryScreen.dart` & `ReviewSubmissionModal.dart` (Nuhadh & Mithila)**:
   - List of completed, active, and cancelled bookings.
   - Cancellation & Refund Policy Estimator: Shows eligible refund percentage (>24h = 90%, 12–24h = 50%, <12h = 0%) with immediate seat release.
   - Post-Trip Review Modal: 1–5 star rating for bus cleanliness and driver behavior, optional comment with profanity filter check, anonymous review toggle, strictly enforced 7-day post-trip window.
6. **`DisruptionAlertBanner.dart` & `RebookingProposalDialog.dart` (Dineth)**:
   - High-urgency push/in-app alert banner when a booked service is disrupted.
   - Rebooking Dialog: Shows the AI-recommended alternative service, departure adjustment, and transfer notes.
   - Passenger actions: "Accept Alternative Journey" (updates booking in 1 tap) or "Request Instant 100% Refund".
7. **`ConductorScannerScreen.dart` (Mithila - Selected Frontend-Only Feature M7)**:
   - Fullscreen camera barcode scanner using `mobile_scanner`.
   - Fast-scan Flashlight / Torch toggle button (`M7`).
   - Real-time QR verification against `/api/v1/tickets/verify-qr`:
     - *Valid Ticket*: Instant green overlay, success chime, phone vibration, marks passenger as `Boarded`.
     - *Invalid / Duplicate Scan*: Red warning overlay, alert buzz, displays exact error ("Ticket already boarded at 07:14 AM" or "Wrong Service").

---

## 6. Implementation of the 20 Production-Readiness Gates

| # | Gate Criterion | Exact Implementation in Web & Mobile |
|---|---|---|
| **1** | **Never Happy-Path Only** | Every screen wraps content in a finite state machine: `SkeletonLoading` ➔ `EmptyState` ➔ `ErrorState` (with retry button) ➔ `ActiveView`. |
| **2** | **Handle Failure as Carefully as Success** | Centralized Axios/Dio error parsing converts HTTP 400, 401, 403, 404, 409, 500, and network timeouts into friendly user messages with actionable next steps. Raw stack traces are forbidden. |
| **3** | **Build Real Forms** | All forms enforce schema validation (Zod on Web, FormValidators on Flutter), disabled submit buttons with spinner during submission, double-click protection, and preservation of input values on server rejection. |
| **4** | **Security is Not Optional** | Passwords masked with visibility toggles; JWT tokens stored in HTTP-only/secure storage; role claims checked on client navigation and re-enforced by server; zero hardcoded API keys. |
| **5** | **Truly Responsive Web** | Tested across 320px (mobile browser), 768px (tablet), 1024px (laptop), and 1440px+ (control-room display). Tables collapse into responsive cards on mobile. |
| **6** | **Truly Mobile UI** | Flutter utilizes `SafeArea`, Android back button handling, `SingleChildScrollView` with keyboard-avoiding physics, and 48px minimum touch targets. |
| **7** | **Accessibility Considered** | High-contrast WCAG 2.1 AA compliant color pairings; screen-reader accessible labels (`aria-label` / `Semantics`); no reliance on color alone (icons accompany all status badges). |
| **8** | **Data Correctness & Persistence** | 100% of data is fetched from and saved to the ASP.NET Core API and PostgreSQL. Zero mocked frontend storage masquerading as real functionality. |
| **9** | **Search, Lists & Tables Production-Ready** | Debounced search inputs (300ms); pagination controls; multi-column sorting; empty search state ("No routes found matching 'Ella'"). |
| **10** | **Performance Matters** | TanStack Query caches API responses; lazy loading for routes; optimized SVG icons; no duplicate network requests. |
| **11** | **Navigation Survives Real Users** | Direct URLs work; page refresh retains auth state; 404 Not Found screen with "Return to Home" button; unsaved changes guard on form exit. |
| **12** | **UX Communicates** | Toasts for background events (disruptions, bookings); inline badges for statuses; confirmation dialogs for destructive actions (cancel booking, lock user). |
| **13** | **No Placeholder Quality UX** | Zero dead buttons, zero `#` links, zero placeholder text. Every interactive element has an active handler or route. |
| **14** | **Hostile User Testing** | Validated against rapid double-clicks, empty submissions, special characters (`<script>`, emojis), and network disconnections during checkout. |
| **15** | **Verify Backend + Frontend Together** | Full flow verified: Flutter Search ➔ ASP.NET Core API ➔ PostgreSQL ➔ Multi-Agent AI ➔ React Approval ➔ DB Transaction ➔ Flutter Ticket. |
| **16** | **Test Build & Deployment** | Web builds cleanly with `npm run build`; mobile compiles with `flutter analyze` and `flutter test`. |
| **17** | **Review Surrounding Codebase** | Re-uses shared domain types, conforms to `.editorconfig`, eliminates redundant utility files. |
| **18** | **Document Important Behavior** | Step-by-step setup instructions, environment variable references (`.env.example`), and API contract documentation. |
| **19** | **20-Question Completion Check** | All 20 readiness questions must pass before declaring any screen complete. |
| **20** | **The Most Important Rule** | Optimized for real humans: reliable, predictable, safe, and pleasant for operators and passengers alike. |

---

## 7. Stitch MCP Screen Generation Strategy

To ensure visual excellence and adherence to Stitch design standards:
1. **Design System Baseline**: Synchronize with existing project `projects/15831387990617273223` (Bus Booking System) and export tokens into `docs/design/DESIGN.md`.
2. **Screen Generation via StitchMCP**:
   - Utilize `StitchMCP:generate_screen_from_text` for specialized high-fidelity screens:
     - `Operator Overview & Disruption Hub` (Desktop)
     - `Seat Layout Designer Canvas` (Desktop)
     - `Manager Approval & AI Observability Workbench` (Desktop)
     - `Interactive Bus Seat Picker with Gestures` (Mobile)
     - `Digital QR Ticket Wallet & Departure Card` (Mobile)
     - `Conductor Camera QR Scanner Overlay` (Mobile)
3. **Component Synthesis**:
   - Extract generated HTML/CSS from Stitch screens using `stitch::react-components` for React and translate into idiomatic Flutter widgets with `flutter-expert`.
   - Refactor into modular primitives in `web/src/components/ui/` and `mobile/lib/core/widgets/`.

---

## 8. Verification & Test Plan

1. **Automated Unit & Component Tests**:
   - Web: Vitest tests for every page covering loading, error, empty, and populated states (`npm run test`).
   - Mobile: Flutter tests for BLoC state transitions and widget rendering (`flutter test`).
2. **End-to-End Workflow Verification**:
   - Passenger registers on Flutter ➔ Searches Colombo to Ella ➔ Selects seats on 2D map ➔ Reserves 10-minute hold ➔ Completes sandbox payment ➔ Receives signed QR ticket.
   - Operator logs disruption on React ➔ Multi-agent AI generates rebooking plan ➔ Transport Manager inspects diff and approves ➔ Passenger receives notification on Flutter and verifies updated ticket.
   - Conductor scans QR ticket on mobile camera ➔ Validates HMAC signature ➔ Passenger marked `Boarded` on React manifest.
