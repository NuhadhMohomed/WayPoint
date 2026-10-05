# WayPoint Web App UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED PROCESS SUB-SKILL: Use `superpowers:executing-plans` (or `superpowers:subagent-driven-development`) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Completely redesign the WayPoint React Web App UI to synchronize with the mobile frontend's Spring Green (`#32DE84`) + Midnight Navy (`#090D16`) color palette, incorporate enterprise-grade frontend-only transit NOC features (Command Palette `Ctrl+K`, A4 printable manifests, interactive fare & buffer simulator, visual seat filters, driver HOS rest gauge, AI trace waterfall, JSON diff inspector, tiered refund simulator, offline banner), and achieve 100% compliance with the 20-point Non-Negotiable Product Quality Gate.

**Architecture:** A multi-tier transit operations web workspace built on React 18, Vite, Tailwind CSS, TanStack Query, and Zustand. The design system bridges Stitch `DESIGN.md` tokens and Flutter `AppTheme` into CSS variables (`:root` / `.dark`), managed via a persistent `themeStore`. State is partitioned cleanly between client-side auth/theme/density stores and server-side TanStack queries with zero waterfalls and optimistic UI.

**Tech Stack:** React 18, Vite 5, Tailwind CSS 3.4, TanStack Query v5, Zustand 4, Lucide React (standardized stroke), Vitest + React Testing Library, Axios.

**Spec Reference:**
- `docs/design/DESIGN.md` (Design tokens & component rules)
- `docs/design/stitch-screens-index.md` (Screens `WEB-01` to `WEB-12`)
- `docs/requirements/requirements.md` (SRS: `FR-AUTH` to `FR-REVIEW`)
- `docs/requirements/business-rules.md` (Business Rules: `BR-TRANSFER-001`, `BR-HOLD-001`, `BR-APPROVAL-001`, `BR-REFUND-001`)
- `mobile/lib/core/theme/app_theme.dart` (Authoritative Spring Green mobile color tokens)

---

## Skill Governance Matrix

The following specialized skills govern each task and phase of this plan:

| Task / Phase | Governing Skills | Specific Rules & Directives Enforced |
| :--- | :--- | :--- |
| **All Tasks** | `using-superpowers`, `full-output-enforcement` | Announce skill usage before actions; ban all truncation (`// ...`, `// TODO`, `/* rest */`); full production code only. |
| **Task 1: Tokens, Theme & Density** | `design-taste-frontend`, `ui-ux-pro-max`, `react-vite-dashboard` | Enforce 3 Dials (`Variance: 6`, `Motion: 4`, `Density: 8`); map mobile tokens (`#32DE84`, `#042611`, `#090D16`, `#131B2E`) to `:root` & `.dark`; add compact/spacious density. |
| **Task 2: UI Primitives & Productivity** | `high-end-visual-design`, `ui-ux-pro-max`, `web-design-guidelines`, `react-ui-patterns` | Double-bezel architecture; Command Palette (`Ctrl+K`); Keyboard Shortcuts modal (`?`); JSON Diff viewer; Offline banner; CSV export utility; min 44px touch targets. |
| **Task 3: Shell & Navigation** | `high-end-visual-design`, `design-taste-frontend`, `web-design-guidelines` | Mobile drawer navigation (<768px); SLST live clock & departure countdowns; Spring Green active markers; global hotkeys mounting; 404 page. |
| **Task 4: Auth & Overview Cockpit** | `react-ui-patterns`, `react-best-practices`, `high-end-visual-design` | Inline form validation; password toggle; live NOC telematics KPI cards; departure countdown board; eliminate query waterfalls. |
| **Task 5: Journey Planning Hub** | `react-vite-dashboard`, `react-ui-patterns`, `ui-ux-pro-max` | Interactive Distance, Travel Time & Fare Calculator; 20m transfer buffer validator (`BR-TRANSFER-001`); saved filter presets in `localStorage`. |
| **Task 6: Fleet & Seat Studio** | `react-vite-dashboard`, `ui-ux-pro-max`, `high-end-visual-design` | Visual WYSIWYG 2x2/2x1 seat map with window/aisle highlight filters; Driver 8h rest compliance gauge (`BR-RESOURCE-002`); review analytics charts. |
| **Task 7: Bookings & Manifest** | `react-vite-dashboard`, `react-ui-patterns`, `ui-ux-pro-max` | Print-ready A4 gate manifest (`@media print`); CSV manifest export; 10m hold countdown bar; payment sandbox; interactive tiered refund simulator (`BR-REFUND-001`). |
| **Task 8: Disruption & AI Observability** | `react-vite-dashboard`, `react-ui-patterns`, `design-taste-frontend` | Incident intake; before/after diff for manager approval; broadcast center with push preview; AI multi-agent DAG with interactive timing waterfall. |
| **Task 9: Admin Console & Audit** | `react-vite-dashboard`, `web-design-guidelines` | RBAC user management; filterable SHA-256 immutable audit explorer with syntax-highlighted JSON Diff viewer. |
| **Task 10: Quality Gate Verification** | `verification-before-completion`, `test-driven-development` | Full Vitest test run; production bundle build; mobile viewport verification; hotkey and print style audit; evidence before completion. |

---

## Global Constraints

- **Color Lock**: Primary accent must strictly be Spring Green (`#32DE84`), on-primary text `#042611`, hover `#1EAE60`. Secondary: Sunset Amber (`#F59E0B`). Tertiary: Sky Blue (`#0284C7`). Error: Crimson (`#EF4444`). Dark Canvas: `#090D16`, Dark Surface: `#131B2E`. Light Canvas: `#F8FAFC`, Light Surface: `#FFFFFF`. No random purple or blue gradients.
- **Typography Lock**: Plus Jakarta Sans for headings & display. Inter for body, forms, and data tables. Monospace for cryptographic hashes, timestamps, and ticket numbers.
- **Accessibility Floor**: WCAG AA minimum across all text/backgrounds; WCAG AAA ($\ge 7:1$) on primary interactive buttons (`#042611` on `#32DE84`).
- **Touch Target Floor**: Minimum $48 \times 48\text{px}$ touch target on all clickable mobile elements (`ui-ux-pro-max`).
- **No Truncation / No Placeholders**: Every file modification must output the complete file. Banned patterns: `// ...`, `// TODO`, `/* rest of code */`.

---

## Review Focus (5 Critical Quality Gate Failure Modes)

1. **Flashing Loading Skeletons on Refetch (`react-ui-patterns`)**: Loading skeleton must render ONLY when initial data is null/undefined. Refetches or background syncs must preserve visible data.
2. **Double Form Submissions (`react-ui-patterns`)**: Buttons must be disabled with loading spinners during asynchronous operations.
3. **Contrast Failure on Interactive Buttons (`design-taste-frontend` & `ui-ux-pro-max`)**: Primary buttons using `#32DE84` must NEVER use white text. Must strictly use `#042611` (Forest Black).
4. **Mobile Navigation Breakage (<768px) (`web-design-guidelines`)**: Fixed 256px sidebar must collapse into a responsive hamburger slide-out drawer on tablet and mobile viewports.
5. **Raw API Error Exposure (`react-ui-patterns`)**: Technical errors, 401s, 409s, and `[object Object]` must never be presented raw. Must be mapped to actionable human-readable messages with retry controls.

---

## Tasks

### Task 1: Design Tokens, Tailwind Configuration, Theme Engine & Workspace Density

**Governing Skills:** `design-taste-frontend`, `ui-ux-pro-max`, `react-vite-dashboard`

**Files:**
- Create: `web/src/store/themeStore.js`
- Modify: `web/tailwind.config.js`
- Modify: `web/src/index.css`
- Create: `web/src/store/__tests__/themeStore.test.js`

**Interfaces:**
- Consumes: Mobile theme tokens from `mobile/lib/core/theme/app_theme.dart`
- Produces: `useThemeStore` hook (`theme`, `density`, `toggleTheme`, `toggleDensity`, `setTheme`), Tailwind utility classes (`bg-waypoint-primary`, `text-waypoint-onPrimary`, `bg-waypoint-darkSurface`, etc.), and CSS custom properties (`--background`, `--foreground`, `--primary`, `--card`, `--table-row-h`).

- [ ] **Step 1: Write unit tests for ThemeStore**
  Create `web/src/store/__tests__/themeStore.test.js`:
  ```javascript
  import { describe, it, expect, beforeEach } from 'vitest'
  import { useThemeStore } from '../themeStore'

  describe('ThemeStore', () => {
    beforeEach(() => {
      localStorage.clear()
      document.documentElement.className = ''
      useThemeStore.setState({ theme: 'dark', density: 'spacious' })
    })

    it('initializes with dark theme and spacious density by default', () => {
      expect(useThemeStore.getState().theme).toBe('dark')
      expect(useThemeStore.getState().density).toBe('spacious')
    })

    it('toggles between dark and light theme', () => {
      useThemeStore.getState().toggleTheme()
      expect(useThemeStore.getState().theme).toBe('light')
      expect(document.documentElement.classList.contains('dark')).toBe(false)

      useThemeStore.getState().toggleTheme()
      expect(useThemeStore.getState().theme).toBe('dark')
      expect(document.documentElement.classList.contains('dark')).toBe(true)
    })

    it('toggles density between spacious and compact', () => {
      useThemeStore.getState().toggleDensity()
      expect(useThemeStore.getState().density).toBe('compact')
      expect(document.documentElement.classList.contains('density-compact')).toBe(true)

      useThemeStore.getState().toggleDensity()
      expect(useThemeStore.getState().density).toBe('spacious')
      expect(document.documentElement.classList.contains('density-compact')).toBe(false)
    })
  })
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `npm test web/src/store/__tests__/themeStore.test.js`
  Expected: FAIL with "Cannot find module '../themeStore'"

- [ ] **Step 3: Implement `themeStore.js`**
  Implement Zustand store with `localStorage` persistence, initial OS preference detection, `.dark` class management, and `density-compact` class management.

- [ ] **Step 4: Update `web/tailwind.config.js` with Mobile Brand Tokens**
  Replace obsolete colors with authoritative tokens:
  - `waypoint.primary`: `#32DE84` (Spring Green)
  - `waypoint.onPrimary`: `#042611` (Forest Black)
  - `waypoint.primaryDark`: `#1EAE60` (Hover Emerald)
  - `waypoint.amber`: `#F59E0B` (Sunset Amber)
  - `waypoint.sky`: `#0284C7` (Corridor Sky Blue)
  - `waypoint.error`: `#EF4444` (Crimson Alert)
  - `waypoint.darkBg`: `#090D16` (Midnight Navy)
  - `waypoint.darkSurface`: `#131B2E` (Deep Navy Slate)
  - `waypoint.darkSubdued`: `#1A243B` (Elevated Navy Slate)
  - `waypoint.darkBorder`: `#23304D` (Navy Border)
  - `waypoint.lightBg`: `#F8FAFC`
  - `waypoint.lightSurface`: `#FFFFFF`
  - `waypoint.lightBorder`: `#E2E8F0`
  - Font families: `font-display: ['Plus Jakarta Sans', ...]`, `font-sans: ['Inter', ...]`, `font-mono: ['JetBrains Mono', ...]`.

- [ ] **Step 5: Update `web/src/index.css` with Google Fonts, CSS Variables & Print Styles**
  - Import Google Fonts for `Plus Jakarta Sans` (600, 700, 800) and `Inter` (400, 500, 600).
  - Map `:root` (light) and `.dark` variables to exact HSL equivalents of mobile tokens.
  - Add `.density-compact` rules (reduced row height and padding for cockpit mode).
  - Define custom scrollbars and `@media print` rules.

- [ ] **Step 6: Run tests to verify they pass**
  Run: `npm test web/src/store/__tests__/themeStore.test.js`
  Expected: PASS

---

### Task 2: Core Production UI Component Primitives & Productivity Suite

**Governing Skills:** `high-end-visual-design`, `ui-ux-pro-max`, `web-design-guidelines`, `react-ui-patterns`

**Files:**
- Modify: `web/src/components/ui/Button.jsx`
- Modify: `web/src/components/ui/Card.jsx`
- Modify: `web/src/components/ui/Input.jsx`
- Modify: `web/src/components/ui/TransitBadge.jsx`
- Modify: `web/src/components/ui/SeatReservationBar.jsx`
- Create: `web/src/components/ui/Modal.jsx`
- Create: `web/src/components/ui/DataTable.jsx`
- Create: `web/src/components/ui/EmptyState.jsx`
- Create: `web/src/components/ui/ErrorState.jsx`
- Create: `web/src/components/ui/Skeleton.jsx`
- Create: `web/src/components/ui/Breadcrumbs.jsx`
- Create: `web/src/components/ui/CommandPalette.jsx`
- Create: `web/src/components/ui/KeyboardShortcutsModal.jsx`
- Create: `web/src/components/ui/JsonDiffViewer.jsx`
- Create: `web/src/components/ui/OfflineBanner.jsx`
- Create: `web/src/lib/csvExport.js`
- Modify: `web/src/components/ui/index.js`
- Create: `web/src/components/ui/__tests__/Primitives.test.jsx`

**Interfaces:**
- Consumes: Tailwind classes and CSS variables from Task 1.
- Produces: Exported UI primitives and productivity tools in `web/src/components/ui/index.js` and `web/src/lib/csvExport.js`.

- [ ] **Step 1: Write unit tests for UI Primitives & Utilities**
  Create `web/src/components/ui/__tests__/Primitives.test.jsx` testing Button (loading state, disabled, variants), TransitBadge (semantic roles), Modal (Escape key, portal), EmptyState, ErrorState, and CSV Export utility.

- [ ] **Step 2: Run test to verify it fails**
  Run: `npm test web/src/components/ui/__tests__/Primitives.test.jsx`
  Expected: FAIL with missing components or props.

- [ ] **Step 3: Implement & Upgrade UI Components**
  - **`Button.jsx`**: Spring Green primary with `#042611` text, hover `#1EAE60`, loading state with inline spinner, active scale feedback (`active:scale-[0.98]`), min 44px height.
  - **`Card.jsx`**: Double-bezel architecture support (`dark:bg-[#131B2E] dark:border-[#23304D] bg-white border-slate-200`).
  - **`Input.jsx`**: Explicit `label htmlFor`, focus ring `focus:ring-[#32DE84]/40`, clear search button, error text mapping.
  - **`TransitBadge.jsx`**: Authoritative badges (`Available` Spring Green, `Held` Sunset Amber, `Booked` Slate, `Disrupted` Crimson, `Express` Sky Blue).
  - **`Modal.jsx`**: Accessible dialog with focus trap, `Escape` key dismissal, backdrop click protection, and smooth spring reveal.
  - **`DataTable.jsx`**: Semantic `<table>` with sortable column headers, loading skeleton rows, pagination bar, density support, and built-in "Export to CSV" button.
  - **`EmptyState.jsx`**: Contextual transit icon, title, description, and action CTA button.
  - **`ErrorState.jsx`**: Alert card with human-readable error and "Retry" callback button.
  - **`Skeleton.jsx`**: Pulsing placeholder matching final layout shape (`text`, `card`, `table-row`).
  - **`Breadcrumbs.jsx`**: Hierarchical path navigator for nested tab routes.
  - **`CommandPalette.jsx`**: Global `Ctrl+K` search modal with fuzzy query matching across routes, buses, and quick actions.
  - **`KeyboardShortcutsModal.jsx`**: Accessible `?` key help dialog showing all system hotkeys.
  - **`JsonDiffViewer.jsx`**: Side-by-side syntax-highlighted before/after audit viewer with one-click copy hash button.
  - **`OfflineBanner.jsx`**: Real-time `navigator.onLine` detector with fixed top warning banner.
  - **`csvExport.js`**: Client-side CSV generator supporting custom columns and auto-download.

- [ ] **Step 4: Run tests to verify they pass**
  Run: `npm test web/src/components/ui/__tests__/Primitives.test.jsx`
  Expected: PASS

---

### Task 3: Shell Layout, Responsive Navigation Drawer, SLST Clock & Global Hotkeys

**Governing Skills:** `high-end-visual-design`, `design-taste-frontend`, `web-design-guidelines`

**Files:**
- Modify: `web/src/layouts/DashboardLayout.jsx`
- Create: `web/src/pages/NotFoundPage.jsx`
- Modify: `web/src/App.jsx`
- Create: `web/src/layouts/__tests__/DashboardLayout.test.jsx`

**Interfaces:**
- Consumes: `useAuthStore`, `useThemeStore`, and UI primitives from Task 2.
- Produces: Responsive application frame with dark/light mode toggle, density switcher, mobile navigation drawer, live SLST clock, global command palette, and 404 page.

- [ ] **Step 1: Write test for DashboardLayout**
  Test responsive navigation toggling, theme switcher interaction, SLST clock display, and hotkey listeners (`Ctrl+K`, `?`).

- [ ] **Step 2: Run test to verify it fails**
  Run: `npm test web/src/layouts/__tests__/DashboardLayout.test.jsx`

- [ ] **Step 3: Overhaul `DashboardLayout.jsx`**
  - Dynamic theme styling (`dark:bg-[#090D16] bg-slate-50`).
  - Sidebar styled in Deep Navy Slate (`dark:bg-[#131B2E] bg-white dark:border-[#23304D] border-slate-200`).
  - Active navigation highlighted in Spring Green (`dark:bg-[#32DE84]/15 bg-[#32DE84]/20 text-[#32DE84] border-[#32DE84]/30`).
  - Mobile responsive drawer: Hamburger toggle visible on `< 1024px`, slide-out backdrop drawer with 48px touch targets.
  - Top Bar Telematics:
    - Live **SLST Clock** (UTC+5:30) with pulsing green indicator.
    - Quick Command Palette trigger button (`Ctrl+K`).
    - Keyboard shortcut help button (`?`).
    - Theme Switcher toggle (Sun/Moon icon).
    - Density Switcher toggle (Cockpit Compact vs Spacious).
    - Database status pill + seed railway DB button.
    - User avatar with role chip.
  - Mount global `OfflineBanner`, `CommandPalette`, and `KeyboardShortcutsModal`.

- [ ] **Step 4: Create `NotFoundPage.jsx`**
  Transit-themed 404 screen with clear explanation and "Back to Operations Dashboard" primary button.

- [ ] **Step 5: Run tests to verify they pass**
  Run: `npm test web/src/layouts/__tests__/DashboardLayout.test.jsx`
  Expected: PASS

---

### Task 4: Authentication & Overview Telematics Cockpit

**Governing Skills:** `react-ui-patterns`, `react-best-practices`, `high-end-visual-design`

**Files:**
- Modify: `web/src/pages/LoginPage.jsx`
- Modify: `web/src/pages/RegisterPage.jsx`
- Modify: `web/src/pages/OverviewPage.jsx`

**Interfaces:**
- Consumes: `authApi`, `useAuthStore`, `devApi`.
- Produces: Fully styled auth screens with role presets and real-time NOC telematics overview dashboard with live departure countdown board.

- [ ] **Step 1: Overhaul `LoginPage.jsx` and `RegisterPage.jsx`**
  - Spring Green primary CTA button with `#042611` high-contrast text.
  - Standardized `Input` components with labels and validation error display.
  - Interactive demo role credential chips (`Admin`, `TransportManager`, `Operator`, `Passenger`).
  - Password visibility toggle.

- [ ] **Step 2: Overhaul `OverviewPage.jsx`**
  - **Live NOC Metrics Strip**: 4 bento KPI cards:
    1. Active Fleet in Transit (pulsing Spring Green status).
    2. Overall On-Time Performance (OTP %).
    3. Today's Booked Passengers.
    4. Gross Revenue in LKR (`Rs.`).
  - **Live Departure Ticker Board**: Real-time departure list with live countdown badges ("Departing in 12m", "Boarding Now", "On Schedule").
  - **Component Workspaces Bento**:
    - Component 1: Journey Planning & Route Catalogue (Sethum)
    - Component 2: Fleet, Seat & Resource Feasibility (Nuhadh)
    - Component 3: Booking, Ticketing & Passenger Options (Mithila)
    - Component 4: Disruption, Rebooking & Approval (Dineth)
    - Review & Ratings Dashboard (`SCR-FLEET-101`)
  - Active Railway PostgreSQL connectivity pill and seed trigger.

- [ ] **Step 3: Run existing test suite to ensure zero regressions**
  Run: `npm test`
  Expected: All 15 existing tests pass.

---

### Task 5: Journey Planning, Distance/Fare Simulator & Corridor Hub

**Governing Skills:** `react-vite-dashboard`, `react-ui-patterns`, `ui-ux-pro-max`

**Files:**
- Modify: `web/src/features/journey/JourneyHubLayout.jsx`
- Modify: `web/src/features/journey/RouteManagerPage.jsx` (`WEB-02`)
- Modify: `web/src/features/journey/ServiceSchedulerPage.jsx` (`WEB-04`)
- Modify: `web/src/features/journey/TouristCorridorsPage.jsx` (`WEB-03`)

**Interfaces:**
- Consumes: `journeyApi.getRoutes`, `createRoute`, `getRouteById`.
- Produces: Corridor route manager with stop sequence visualizer, interactive distance/fare simulator, saved filter presets, and timetable scheduler.

- [ ] **Step 1: Update `JourneyHubLayout.jsx`**
  Tab bar with Spring Green active pills, counters, and breadcrumb navigation.

- [ ] **Step 2: Overhaul `RouteManagerPage.jsx` (`WEB-02`)**
  - Interactive stop sequence visualizer with arrival/departure offset minutes and km distance from origin.
  - **Interactive Distance & Segment Fare Simulator**: Select any origin and destination stop along a corridor to calculate mileage, duration, LKR segment fare, and 20-minute transfer buffer safety check (`BR-TRANSFER-001`).
  - **Saved Filter Presets**: Quick filter chips ("Expressways Only", "Hill Country Routes", "Active") saved in `localStorage`.
  - Reusable `DataTable` with sorting, search filtering, CSV export, and skeleton loading state.
  - "Add Route" modal with client-side validation and stop builder.

- [ ] **Step 3: Overhaul `ServiceSchedulerPage.jsx` (`WEB-04`)**
  - Scheduled departures table with bus/driver assignment dropdowns, departure time filters (Morning, Afternoon, Night Mail), and schedule overlap warning alerts (`BR-TIME-001`).

- [ ] **Step 4: Overhaul `TouristCorridorsPage.jsx` (`WEB-03`)**
  - Destination showcase cards: Southern Expressway E01, Central Expressway E04, Hill Country Ella/Kandy, and Coastal links.

- [ ] **Step 5: Verify existing RouteManager tests pass**
  Run: `npm test web/src/features/journey/__tests__/RouteManager.test.jsx`
  Expected: PASS

---

### Task 6: Fleet Management, Visual Seat Studio & Driver HOS Rest Gauge

**Governing Skills:** `react-vite-dashboard`, `ui-ux-pro-max`, `high-end-visual-design`

**Files:**
- Modify: `web/src/features/fleet/FleetHubLayout.jsx`
- Modify: `web/src/features/fleet/FleetMatrixBuilderPage.jsx` (`WEB-05`)
- Modify: `web/src/features/fleet/SeatLayoutDesignerPage.jsx`
- Modify: `web/src/features/fleet/DriverRosteringPage.jsx` (`WEB-06`)
- Modify: `web/src/pages/fleet/FleetReviewsDashboardPage.jsx` (`SCR-FLEET-101`)

**Interfaces:**
- Consumes: `fleetApi.getBuses`, `getSeatLayouts`, `getDrivers`, `reviewApi`.
- Produces: Fleet inventory matrix, interactive WYSIWYG seat layout designer with window/aisle filters, driver 8h HOS rest compliance gauge, and reviews dashboard.

- [ ] **Step 1: Update `FleetHubLayout.jsx`**
  Tabbed navigation for Buses, Seat Layouts, Drivers, and Reviews.

- [ ] **Step 2: Overhaul `FleetMatrixBuilderPage.jsx` (`WEB-05`)**
  Fleet table with registration plate, class (Luxury/Standard), maintenance toggle switch, CSV export, and "Register Bus" modal.

- [ ] **Step 3: Overhaul `SeatLayoutDesignerPage.jsx`**
  - Visual 2x2 and 2x1 interactive seat map grid with driver cabin, passenger entrance, and emergency exit.
  - **Dynamic Seat Legend & Filters**: Interactive filter chips to pulse/highlight "Available Window Seats", "Available Aisle Seats", and "Active 10m Holds".
  - Real-time seat status toggling: Available (Spring Green), Held (Sunset Amber with 10m timer), Booked (Muted Slate), Blocked (Dark Slate).
  - Selected seat inspection sidebar.

- [ ] **Step 4: Overhaul `DriverRosteringPage.jsx` (`WEB-06`)**
  - Visual 24-hour HOS timeline.
  - **Driver Rest Hours Compliance Gauge**: Displays off-duty hours since last trip; highlights in yellow/red if rest is below the mandatory 8-hour threshold (`BR-RESOURCE-002`).
  - Schedule clash warning detector for concurrent shift overlaps.

- [ ] **Step 5: Overhaul `FleetReviewsDashboardPage.jsx` (`SCR-FLEET-101`)**
  - Rating distribution histograms (1 to 5 stars).
  - Interactive star filter (click 5-star bar to filter to only 5-star reviews).
  - Passenger review cards with sentiment tagging and automated profanity moderation badges.

- [ ] **Step 6: Verify existing Fleet tests pass**
  Run: `npm test web/src/features/fleet/__tests__/SeatLayoutDesigner.test.jsx`
  Expected: PASS

---

### Task 7: Bookings, Live Manifest, Printable Sheet & Payment Sandbox

**Governing Skills:** `react-vite-dashboard`, `react-ui-patterns`, `ui-ux-pro-max`

**Files:**
- Modify: `web/src/features/bookings/BookingManifestMonitorPage.jsx` (`WEB-11`)
- Modify: `web/src/features/bookings/OperatorDashboardPage.jsx` (`WEB-01`)

**Interfaces:**
- Consumes: `bookingApi.getBookings`, `getManifest`, `processPayment`.
- Produces: Live departure manifest with boarding verification, A4 printable gate sheet (`@media print`), CSV manifest export, 10m hold countdown monitor, payment sandbox emulator, and tiered refund simulator.

- [ ] **Step 1: Overhaul `BookingManifestMonitorPage.jsx` (`WEB-11`)**
  - Live Departure Manifest table with passenger names, NIC, seat numbers, and boarding action (`Boarded` / `Absent`).
  - **Print-Ready A4 Departure Gate Manifest**: Triggers clean browser print sheet with official WayPoint header, conductor checklist, and passenger roster.
  - **Export Manifest to CSV**: Instant client-side download of passenger roster for terminal staff.
  - **10-Minute Seat Reservation Bar (`SeatReservationBar.jsx`)**: Live countdown in Sunset Amber with ticking seconds.
  - **Dual-Strategy Payment Sandbox**: Preset test cards (Instant Approval, Insufficient Funds, Gateway Timeout) with charge breakdown in LKR.
  - **Interactive Tiered Refund Simulator (`BR-REFUND-001`)**: Departure offset slider (0–72h) showing exact rupee refund breakdown (90% for $>24$h, 50% for 12–24h, 0% for $<12$h, 100% for disruption).

- [ ] **Step 2: Overhaul `OperatorDashboardPage.jsx` (`WEB-01`)**
  - Operator NOC overview: live departure stream, capacity utilization gauges, and quick incident dispatch button.

- [ ] **Step 3: Verify existing Booking Manifest tests pass**
  Run: `npm test web/src/features/bookings/__tests__/BookingManifest.test.jsx`
  Expected: PASS

---

### Task 8: Disruption Hub, Manager Approval & AI Observability Waterfall

**Governing Skills:** `react-vite-dashboard`, `react-ui-patterns`, `design-taste-frontend`

**Files:**
- Modify: `web/src/features/disruptions/DisruptionHubLayout.jsx`
- Modify: `web/src/features/disruptions/DisruptionIntakePage.jsx` (`WEB-07`)
- Modify: `web/src/features/disruptions/ManagerApprovalWorkbenchPage.jsx` (`WEB-08`)
- Modify: `web/src/features/disruptions/ServiceAlertBroadcastPage.jsx` (`WEB-09`)
- Modify: `web/src/features/disruptions/AiObservabilityPage.jsx` (`WEB-10`)
- Modify: `web/src/features/disruptions/AdminConsolePage.jsx` (`WEB-12`)

**Interfaces:**
- Consumes: `disruptionApi`, `aiWorkflowApi`, `approvalApi`, `userApi`.
- Produces: Full disruption lifecycle: incident intake, impact solver, human-in-the-loop approval workbench, public alerts, multi-agent AI execution traces with timing waterfall, and audit logs with JSON diff inspector.

- [ ] **Step 1: Update `DisruptionHubLayout.jsx`**
  Tabbed navigation for Incident Intake, Approvals, Alerts, AI Traces, and Admin Audit.

- [ ] **Step 2: Overhaul `DisruptionIntakePage.jsx` (`WEB-07`)**
  Incident intake form (breakdown, closure, weather), severity selector, and automated passenger impact metrics card.

- [ ] **Step 3: Overhaul `ManagerApprovalWorkbenchPage.jsx` (`WEB-08`)**
  - Transport Manager approval queue (`PendingManagerApproval`).
  - Side-by-side Before/After diff: Original Service vs. Proposed Replacement Bus, timetable shift, net fare delta.
  - Decision buttons: `Approve Operational Change`, `Reject`, `Request Revision`.
  - Immutable SHA-256 hash signature indicator.

- [ ] **Step 4: Overhaul `ServiceAlertBroadcastPage.jsx` (`WEB-09`)**
  Public alert composer with target corridor selection and live mobile push notification preview mockup.

- [ ] **Step 5: Overhaul `AiObservabilityPage.jsx` (`WEB-10`)**
  - Visual Multi-Agent Workflow DAG (Planner $\rightarrow$ Journey $\rightarrow$ Resource $\rightarrow$ Safety).
  - **Interactive Tool Execution Waterfall Chart**: Visual timing bar chart showing execution duration (ms) per tool call.
  - Tool trace table with argument DTO inspection and deterministic server-side assertions.

- [ ] **Step 6: Overhaul `AdminConsolePage.jsx` (`WEB-12`)**
  - User role management table with role assignment modal.
  - **Tamper-Evident SHA-256 Audit Log Explorer**: Click any entry to open the `JsonDiffViewer` showing highlighted before/after JSON diffs with copyable SHA-256 hash.

- [ ] **Step 7: Verify existing Disruption tests pass**
  Run: `npm test web/src/features/disruptions/__tests__/DisruptionHub.test.jsx`
  Expected: PASS

---

### Task 9: Comprehensive Quality Gate Audit & Verification

**Governing Skills:** `verification-before-completion`, `full-output-enforcement`

**Files:**
- All web codebase files.

- [ ] **Step 1: Run Full Vitest Test Suite**
  Execute: `npm test` inside `web/`. Verify 100% pass across all test suites.

- [ ] **Step 2: Run Production Build**
  Execute: `npm run build` inside `web/`. Verify zero bundling errors, zero missing imports, and successful production assets output.

- [ ] **Step 3: Audit Against the 20 Failure Modes & Frontend Features**
  - Verify every data query has loading, empty, and error states.
  - Verify all forms have labels, validation, and submit-disabling.
  - Verify dark and light themes toggle seamlessly with persistent state.
  - Verify density toggle (`density-compact` vs `spacious`) operates cleanly.
  - Verify Command Palette (`Ctrl+K`) and Shortcuts Modal (`?`) open and navigate accurately.
  - Verify print-ready manifest stylesheet formats cleanly.
  - Verify CSV export generates valid downloadable CSV files.
  - Verify high contrast ($\ge 7:1$) on primary actions.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-10-05-web-app-production-redesign.md` and artifact storage.

Please review the plan. Which execution approach would you prefer?
- **Native** (Recommended): I implement every task sequentially in this session, running tests and build checks after each task, with full adherence to the skill directives.
- **Subagent-driven**: A fresh subagent implements each task independently and submits it for review.
