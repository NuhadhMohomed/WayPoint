# WayPoint Mobile Transit Application — Production Redesign Specification

**Document Identifier:** `SPEC-MOB-2026-10-04-02`  
**Status:** Approved by User  
**Target:** `mobile/` (Flutter 3.x / Dart 3.x / Android & iOS)  
**Primary Brand Color:** `#32DE84` (Spring Green / Vibrant Mint) with High-Contrast Deep Slate Surfaces  
**Architectural Pattern:** Feature-First BLoC / Cubit Architecture with Resilient Offline-First Repository Layer  
**Preceding Architecture:** Academic Monorepo with Component 1–4 Test Buttons and Student Attribution Labels  

---

## 1. Executive Summary & Design Mandate

The initial WayPoint mobile harness was implemented as an academic prototype with four tabs partitioned by student names ("Sethum", "Nuhadh", "Mithila", "Dineth"), assignment codes ("SE3090 Assignment 1"), and debug launcher buttons ("Open MOB-05", "Open MOB-10").

This specification defines the complete transformation of the mobile codebase into an **authentic, commercial-grade consumer transit application** for passengers and bus conductors across Sri Lanka's intercity network.

### Core Deliverables:
1. **Total Academic Purge**: Elimination of all student names, assignment tags, and developer launcher buttons.
2. **Authoritative Palette Overhaul (`#32DE84`)**: Full calibration around Spring Green (`#32DE84`), deep navy slates (`#090D16` / `#131B2E`), and WCAG AAA compliant text tokens (`#042611` on primary).
3. **Dual Theme Engine with Settings Toggle**: Real-time switching between ☀️ Light Mode, 🌙 Dark Mode, and ⚙️ System Default, persisted locally in secure storage.
4. **App Launcher Icon & Startup Splash**: Integrated WayPoint transit brand identity on the Android APK and animated startup splash screen.
5. **Strict Role-Based Navigation**:
   - **Passenger Role**: 4-tab bottom navigation (**Explore**, **My Trips / Wallet**, **Alerts**, **Settings**).
   - **Conductor Role**: Dedicated staff navigation (**Assigned Services**, **Live QR Scanner**, **Passenger Manifest & Check-in**, **Conductor Profile**).
6. **End-to-End Real Booking Journey**: Route discovery, preference filtering, candidate comparison, 2x2/2x1 interactive seat matrix with server-side 10-minute hold bar, stop timeline, sandbox payment checkout, digital QR boarding pass wallet, tiered refund cancellation, and post-trip 5-star reviews.
7. **28-Point Production Engineering & UX Standard**: Comprehensive handling of loading states (shimmers, spinners, pull-to-refresh), meaningful empty states, friendly human-readable errors with retry buttons, offline caching, input focus management, tap targets $\ge 48$dp, haptic feedback, and responsive layout across all device sizes.

---

## 2. Visual Design System & Color Palette (`#32DE84`)

### 2.1 Authoritative Palette Tokens

```
                      ┌──────────────────────────────┐
                      │ Primary Spring Green #32DE84 │ (Brand Hero & Active Elements)
                      └──────────────┬───────────────┘
                                     │
           ┌─────────────────────────┴─────────────────────────┐
           ▼                                                   ▼
┌─────────────────────────────┐                     ┌─────────────────────────────┐
│    LIGHT THEME (Fresh)      │                     │    DARK THEME (Midnight)    │
│  Page Bg: #F8FAFC (Slate 50)│                     │  Page Bg: #090D16           │
│  Card Bg: #FFFFFF           │                     │  Card Bg: #131B2E (Navy)    │
│  Surface2:#F1F5F9 (Slate100)│                     │  Surface2:#1A243B           │
│  Border:  #E2E8F0 (Slate200)│                     │  Border:  #23304D           │
│  Text:    #0F172A / #64748B │                     │  Text:    #F8FAFC / #94A3B8 │
│  Container:#D9FBE8 (Mint)   │                     │  Container:#0E3820 (Forest) │
└─────────────────────────────┘                     └─────────────────────────────┘
```

| Token Name | Light Theme Hex | Dark Theme Hex | Semantic Usage |
| :--- | :--- | :--- | :--- |
| `primary` | `#32DE84` | `#32DE84` | Primary CTA buttons, active tab indicators, selected seats, verified checkmarks |
| `onPrimary` | `#042611` | `#042611` | Deep pine text placed on `#32DE84` buttons (contrast ratio $\ge 7:1$, WCAG AAA) |
| `primaryContainer` | `#D9FBE8` | `#0E3820` | Subtle badge backgrounds, active card highlight glows, progress containers |
| `background` | `#F8FAFC` | `#090D16` | Scaffold background |
| `surface` | `#FFFFFF` | `#131B2E` | Card backgrounds, dialogs, bottom sheets, navigation bar |
| `surfaceSubdued` | `#F1F5F9` | `#1A243B` | Inactive seat cells, text fields, chips, divider bars |
| `border` | `#E2E8F0` | `#23304D` | Card borders, table dividers, seat boundaries |
| `textPrimary` | `#0F172A` | `#F8FAFC` | Screen titles, station names, seat numbers, fare amounts |
| `textMuted` | `#64748B` | `#94A3B8` | Timestamps, bus plate numbers, distance badges, secondary descriptions |
| `accentAmber` | `#F59E0B` | `#FBBF24` | 10-minute hold countdown bar, transfer buffer warnings ($\ge 20$ min rule) |
| `accentRose` | `#EF4444` | `#F87171` | Disrupted service flags, cancelled bookings, payment declined states |
| `accentSky` | `#0284C7` | `#38BDF8` | Highway expressway badges, luxury AC tier tags, timetable metadata |

### 2.2 Typography Pairing
- **Headings & Badges**: *Plus Jakarta Sans* (`FontWeight.w700` and `FontWeight.w600`, tracking `-0.02em`).
- **Body & Numerical Timetables**: *Inter* (`FontWeight.w400` and `FontWeight.w500`, high legibility for 24h clock, LKR currency, and seat codes).

---

## 3. High-Level System Architecture & Navigation Routing

```
┌────────────────────────────────────────────────────────────────────────┐
│                                AuthGate                                │
│   (Checks First-Launch Flag, Stored JWT Token, and User Role Claim)    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
       ┌────────────────────────────┼────────────────────────────┐
       ▼                            ▼                            ▼
┌───────────────┐           ┌───────────────┐           ┌────────────────┐
│  Onboarding   │           │ PassengerAuth │           │  Role Router   │
│  (3-step deck)│           │ (Login/Reg)   │           │                │
└──────┬────────┘           └───────┬───────┘           └───────┬────────┘
       │                            │                           │
       └────────────────────────────┴───────────────────────────┤
                                                                ▼
                     ┌──────────────────────────────────────────┴──────────────────────────────────────────┐
                     ▼                                                                                     ▼
  ┌─────────────────────────────────────┐                                               ┌─────────────────────────────────────┐
  │        PassengerShell (4 Tabs)      │                                               │         ConductorShell (4 Tabs)     │
  ├─────────────────────────────────────┤                                               ├─────────────────────────────────────┤
  │ Tab 1: Explore & Search             │                                               │ Tab 1: Active Services              │
  │ • Sri Lanka Corridors Carousel      │                                               │ • Assigned bus route & departures   │
  │ • Quick Swap Origin ⇄ Destination   │                                               │ • Service status & passenger totals │
  │ • Date Chips (Today/Tomorrow)       │                                               │                                     │
  │ • Amenity Filters & Search Trigger  │                                               │ Tab 2: Live QR Scanner              │
  │ • Active Upcoming Trip Hero Banner  │                                               │ • Camera viewfinder + flashlight    │
  │                                     │                                               │ • HMAC verification & boarding logs │
  │ Tab 2: My Trips & Ticket Wallet     │                                               │                                     │
  │ • Active Boarding Pass with QR      │                                               │ Tab 3: Boarding Manifest            │
  │ • Departure Countdown Timer         │                                               │ • Passenger list by seat number     │
  │ • Offline Pass Caching              │                                               │ • 1-tap manual check-in toggle      │
  │ • Tiered Refund Cancellation Modal  │                                               │                                     │
  │ • Past Trips & Review Submissions   │                                               │ Tab 4: Conductor Profile            │
  │                                     │                                               │ • Staff credentials, shift details  │
  │ Tab 3: Alerts & Service Advisory    │                                               │ • Session logout                    │
  │ • Live corridor disruption notices  │                                               └─────────────────────────────────────┘
  │ • 1-Tap alternative bus rebooking   │
  │                                     │
  │ Tab 4: Account & Settings           │
  │ • Passenger profile details         │
  │ • Saved travel companions directory │
  │ • Interactive Light/Dark switcher   │
  │ • Help hotline & session logout     │
  └─────────────────────────────────────┘
```

---

## 4. Screen-by-Screen Specifications & State Machines

### 4.1 Startup Splash & Onboarding (`OnboardingScreen.dart`)
- **App Launcher Icon**: Clean vector transit pin embracing a stylized bus path in glowing `#32DE84` on `#090D16` slate.
- **Animated Splash Screen**: App launches with centered brand mark, glowing ring pulse, and app title: *"WayPoint • Sri Lanka Intercity Transit"*. Reads stored token in background (250ms).
- **Onboarding Carousel**: 3 illustrated cards for first-time users:
  1. *Explore Sri Lanka*: "Discover express routes from Colombo to Ella, Kandy, Galle, and Jaffna."
  2. *Live 10-Minute Seat Holds*: "Lock your favorite window or aisle seat in real time with guaranteed holds."
  3. *Digital Boarding Passes*: "Scan and ride with cryptographic QR tickets—even without internet connectivity."
- **Actions**: *"Skip"* (top right) and dynamic *"Next"* / *"Get Started"* button in `#32DE84`. Sets `onboarding_completed: true` in `flutter_secure_storage`.

### 4.2 Clean Authentication & Profile Initialization (`PassengerAuthScreen.dart`)
- **Tabs**: Clean segmented toggle between **Sign In** and **Create Account**.
- **Form Fields**:
  - Email (with email keyboard type and inline regex validation).
  - Password (with eye toggle for show/hide, minimum 8 characters).
  - Full Name & Phone Number (`+94` format) on Registration.
  - Role dropdown default to `Passenger` (with optional `Conductor` for staff sign-in).
- **UX Protections**: Disabled submit button during async request, spinner indicator, duplicate tap suppression, humanized error snackbar on bad credentials.
- **API Connection**: `POST /api/v1/auth/login` and `POST /api/v1/auth/register`. On success, writes JWT token and user profile to secure storage and mounts `PassengerShell` or `ConductorShell`.

### 4.3 Explore & Journey Search (`JourneySearchScreen.dart`)
- **Hero Corridor Carousel**: Horizontal cards showing Sri Lanka's prime routes:
  - *Colombo $\rightarrow$ Ella* (Hill Country Scenic A5, from Rs. 2,400)
  - *Colombo $\rightarrow$ Kandy* (Central A1 Express, from Rs. 950)
  - *Colombo $\rightarrow$ Galle* (Southern E01 Expressway, from Rs. 1,200)
  - *Colombo $\rightarrow$ Jaffna* (Northern Link A9, from Rs. 3,100)
  - Tapping a corridor auto-fills origin, destination, and triggers search.
- **Search Card**:
  - Origin & Destination with interactive swap button ($\rightleftharpoons$).
  - Quick Date Selector with chips: *"Today"*, *"Tomorrow"*, *"This Weekend"*, and calendar icon for custom date.
  - Passenger Stepper: $1$ to $10$ passengers.
  - Filter button with active filter counter badge.
- **Active Trip Banner**: If the user has a confirmed upcoming trip departing in $<24$ hours, an interactive card floats above the search bar: *"Next Trip: Colombo $\rightarrow$ Kandy at 14:45 • Seat 14B [View QR Pass]"*.
- **States Handled**: Initial (Corridors loaded), Searching (Shimmer cards), Error (Retry banner), Network Offline (Fallback to cached routes).

### 4.4 Preference Filter Sheet (`PreferenceFilterSheet.dart`)
- **Modal Sheet**: Draggable bottom sheet with handle.
- **Controls**:
  - Departure Time Windows: Morning ($06:00 - 12:00$), Afternoon ($12:00 - 18:00$), Night ($18:00 - 06:00$).
  - Bus Class Toggles: *Luxury AC*, *Semi-Luxury*, *Standard*.
  - Amenities: *Wi-Fi*, *USB Power*, *Reclining Seats*, *Luggage Space*.
  - Direct Only switch.
- **Dynamic Action Button**: Bottom CTA updates in real time: *"Show 6 Buses"* with `#32DE84` background.

### 4.5 Journey Comparison & Selection (`JourneyComparisonScreen.dart`)
- **Candidate Cards**:
  - Departure & Arrival time, duration, and operator logo/tier pill.
  - Bus model: *Yutong Luxury AC*, *King Long Express*, *Ashok Leyland Cruiser*.
  - Direct vs Connecting indicator.
  - **Transfer Buffer Badge (`BR-TRANSFER-001`)**: Connecting routes show a safe green shield if transfer window $\ge 20$ mins, or amber warning if tight.
  - Real LKR fare (e.g., `Rs. 2,400.00`).
  - Available seats count (`8 seats left`).
- **Empty State**: Friendly card: *"No buses match your filters. Try clearing amenity filters or picking another date [Reset Filters]"*.

### 4.6 Interactive Seat Map & 10-Minute Hold Bar (`SeatPickerScreen.dart`)
- **Visual Bus Layout**:
  - Driver cabin header with steering wheel icon, entrance door, and driver seat.
  - 2x2 layout (columns A, B, aisle, C, D) with 10 rows + 5-seat back row.
  - Seat Statuses:
    - *Available*: Light surface with slate outline (`#E2E8F0` / `#23304D`).
    - *Selected*: Vibrant Spring Green (`#32DE84`) with checkmark and haptic feedback.
    - *Held*: Amber outline with pulsing clock icon (`Reserved by another`).
    - *Booked*: Dimmed slate background (`Unavailable`).
    - *Ladies / Priority*: Subtle icon indicator.
- **Floating 10-Minute Hold Bar (`SeatReservationBar.dart`)**:
  - Upon selecting seats and pressing *"Reserve Seats"*, backend `POST /api/v1/bookings/hold` creates an authoritative lock.
  - Persistent floating bar animates at bottom: *"Seats 12A, 12B Held • 09:48 remaining [Continue to Checkout]"*.
  - Progress bar shifts from `#32DE84` $\rightarrow$ Amber ($\le 5$ min) $\rightarrow$ Crimson ($\le 2$ min).
  - Ticking timer triggers automatic release dialog on expiration with option to re-select.

### 4.7 Intermediate Stop Selector & Boarding Timeline
- Vertical step progress indicator embedded in checkout:
  - Origin terminal (e.g. *Bastian Mawatha Pettah - 08:30 AM*).
  - Intermediate boarding stops (e.g. *Kadawatha Highway Interchange - 09:10 AM*).
  - Destination terminal (e.g. *Ella Town Center - 14:15 PM*).
  - Radio selector allowing passenger to designate exact pickup point.

### 4.8 Payment Sandbox Checkout (`PaymentCheckoutScreen.dart`)
- **Hold Status Reminder**: Sticky top countdown banner matching the active hold ID.
- **Fare Breakdown Accordion**:
  - Base Bus Fare: `Rs. 2,400.00 × 2 = Rs. 4,800.00`
  - Transit Levy: `Rs. 120.00`
  - Promo Code Input: Input field + *"Apply"*. Code `WAYPOINT20` discounts $20\%$ with instant animated green badge.
  - Optional Travel Insurance: Checkbox (`+ Rs. 150.00 per passenger`).
  - Total Payable in bold `Rs. 3,990.00`.
- **Primary Passenger & Saved Travelers**:
  - Autofill from logged-in user profile with quick-chip to select saved companion travelers.
  - NIC / Passport number and Emergency Phone Number validation.
- **Payment Method Selector**:
  - *Credit / Debit Card* (Visa, Mastercard with live formatted card preview).
  - *LankaPay / FriMi* (Direct interbank mobile checkout).
  - *Cash on Boarding* (Pay conductor at bus gate).
- **Sandbox Simulation Controls**:
  - Test buttons for evaluation: *"Pay Rs. 3,990 (Success)"*, *"Simulate Card Decline"*, *"Simulate Timeout"*.
- **Confirmation Flow**: Calls `POST /api/v1/bookings/checkout`. On success, releases hold, issues booking reference, triggers confetti animation, and navigates directly to `TicketWalletScreen`.

### 4.9 Digital QR Ticket Wallet (`TicketWalletScreen.dart`)
- **Tear-Away Boarding Pass Design**:
  - Perforated border edge with notch cutouts.
  - High-resolution dynamic QR code generated using `qr_flutter` containing HMAC payload.
  - Bus route code (`CMB-ELL-001`), Departure time, Boarding bay (`Bay 04`), Bus registration plate (`WP-NB-4491`).
  - Seat badges in `#32DE84` (`Seat 12A, 12B`).
  - Live departure countdown: *"Departs in 3 hours 20 minutes"*.
- **Quick Utility Toolbar**:
  - 🔆 **Screen Brightness Booster**: 1-tap toggle to boost display to $100\%$ brightness for fast scanner reading at night.
  - 💾 **Offline Pass**: Automatically cached in `flutter_secure_storage` so the pass works with zero cellular network.
  - 📅 **Add to Calendar**: Exports trip schedule to system calendar.
  - 📤 **Share Itinerary**: Native share sheet to WhatsApp / SMS.

### 4.10 Booking History & Tiered Refunds (`BookingHistoryScreen.dart`)
- **Tabbed Filter**: *Upcoming Trips* | *Past & Completed Trips* | *Cancelled*.
- **Tiered Cancellation & Refund Modal (`BR-REFUND-001`)**:
  - Tapping *"Cancel Ticket"* on an upcoming trip opens the authoritative refund breakdown:
    - Departure offset calculation (e.g. *"Departure is in 36 hours"*).
    - Policy Tier: **Tier 1 (>24h): 90% Refund Eligible**.
    - Original Fare: `Rs. 2,400.00`
    - Policy Deduction ($10\%$ fee): `- Rs. 240.00`
    - Net Refund to Original Card: `Rs. 2,160.00`
  - Explicit confirmation button: *"Confirm Cancellation & Refund Rs. 2,160.00"*. Calls `POST /api/v1/bookings/cancel`. Immediately returns seat to available pool in database.

### 4.11 Disruption Alerts & 1-Tap Rebooking (`DisruptionAlertScreen.dart`)
- **Notification Inbox**: Filterable feed with unread badge counter.
- **High-Impact Disruption Card**:
  - Crimson banner: *"URGENT: Bus Breakdown on Route CMB-KAN-001"*.
  - Reason: Mechanical issue on original coach `WP-NA-9021`.
  - **Side-by-Side Rebooking Proposal**:
    - Original Bus: Departure $14:00$ $\rightarrow$ Replacement Bus: Departure $14:30$ (`WP-NB-4491`).
    - Allocated Replacement Seats: Automatic transfer to identical row `12A, 12B`.
    - Fare Difference: `Rs. 0.00 (Fully Covered)`.
  - **1-Tap Action Buttons**:
    - *"Accept Replacement Bus"* (calls backend rebooking acceptance).
    - *"Request 100% Instant Refund"* (releases booking with zero penalty).

### 4.12 Post-Trip Bus & Driver Reviews (`ReviewSubmissionScreen.dart`)
- **Trigger**: App prompts passenger upon trip completion in *Past Trips*.
- **Interactive 5-Star Rating**:
  - Bus Coach Rating (Cleanliness, AC, Seat Comfort).
  - Driver Rating (Punctuality, Safe Driving).
- **Fast-Tag Pills**: *"Punctual"*, *"Clean AC"*, *"Polite Conductor"*, *"Smooth Highway Driving"*, *"Loud Music"*, *"Slight Delay"*.
- **Comments Box**: Multi-line passenger feedback.
- **Backend API**: `POST /api/v1/reviews/buses` and `POST /api/v1/reviews/drivers`.

### 4.13 Settings & User Control (`PassengerSettingsScreen.dart`)
- **Theme Switcher**: Segmented control with 3 active states: ☀️ **Light**, 🌙 **Dark**, ⚙️ **System Default**. Instantly updates `ThemeMode` in `ThemeCubit` and persists in storage.
- **Saved Travelers Directory**: List of companion passenger profiles (Name, NIC/Passport) with add/edit/delete modal for rapid checkout auto-fill.
- **Saved Payment Cards**: Masked cards (`•••• 4491`) with remove option.
- **Language Switcher**: English, සිංහල (Sinhala), தமிழ் (Tamil).
- **Transit Support & Helpline**: Direct dialer to Sri Lanka transit hotline (`1955`).
- **Session Logout**: Securely clears JWT token and returns to `PassengerAuthScreen`.

### 4.14 Conductor Operations Suite (`ConductorScannerScreen.dart` & `ConductorManifestScreen.dart`)
- **Live QR Scanner (`ConductorScannerScreen.dart`)**:
  - Camera viewfinder with target reticle using `mobile_scanner`.
  - Flashlight torch toggle button for dark bus loading bays.
  - Manual booking reference input modal as backup.
  - **Validation Card**:
    - Green checkmark for Valid Ticket: displays passenger name, seat numbers, route, and boarding bay. Calls `POST /api/v1/tickets/verify`.
    - Red cross for Invalid Ticket: displays reason (Expired, Duplicate Scan, Wrong Service).
- **Passenger Manifest Roster (`ConductorManifestScreen.dart`)**:
  - Roster of all passengers booked on today's departure.
  - Filter pills: *All (40)* | *Boarded (28)* | *Pending (12)*.
  - Search bar to locate passenger by name or seat number.
  - 1-tap manual check-in toggle button per passenger.

---

## 5. The 28-Point Production Engineering & UX Standard Matrix

Every screen in the mobile app strictly conforms to the following operational matrix:

| # | Standard Category | Production Rule Enforced in WayPoint Mobile |
| :--- | :--- | :--- |
| **1** | **Global States** | Every screen explicitly models: `Initial`, `Loading`, `Loaded/Success`, `Empty`, `Error`, `Offline`, `Refreshing`, `Submitting`, `Disabled`. No screen ever renders a blank canvas. |
| **2** | **Loading UX** | Shimmer skeleton cards on journey searches, inline button spinners on form submissions, full pull-to-refresh on lists. Submission buttons disable immediately to prevent duplicate requests. |
| **3** | **Empty States** | Every list distinguishes between: Never booked, No search results, Filters narrowed to zero, and Network failed to load. Clear call-to-action buttons guide the user (*"Search Routes"*, *"Reset Filters"*). |
| **4** | **Humanized Errors** | Zero raw technical strings (no `HTTP 500`, no `NullReferenceException`). Errors explain what happened and provide a *"Try Again"* recovery action. |
| **5** | **Offline Resilience** | App monitors network connectivity. Active tickets and cached routes remain viewable in offline mode with a subtle amber banner: *"Viewing cached offline pass"*. |
| **6** | **Navigation & History** | Android hardware back button and iOS swipe-to-pop preserve screen stack. Popping from checkout returns to seat map without resetting chosen seats. |
| **7** | **Form Ergonomics** | Email, phone (`+94`), and numerical keyboards bound to relevant inputs. Passwords toggle visibility. Labels mark required fields clearly. Focus moves smoothly to next input on *"Next"* action. |
| **8** | **Touch Targets** | All interactive buttons and seat cells have minimum bounding boxes of $48\times 48$dp with ripple/pressed feedback. |
| **9** | **Purposeful Motion** | Fluid page transitions, modal slides, and subtle scale feedback on seat selection. Zero excessive or decorative animations. Respects reduced-motion device settings. |
| **10** | **Accessibility** | All icons have `semanticLabel`s. Text elements scale properly with system font settings without clipping. Color is never the sole indicator of status (icons accompany colors). |
| **11** | **Responsive Layout** | Layouts dynamically scale using `LayoutBuilder` and `MediaQuery`. Safe areas protect content against device notches, dynamic islands, and home indicator bars. |
| **12** | **Real-World Edge Cases** | Text truncates with clean ellipsis (`TextOverflow.ellipsis`). Handles long passenger names (*"Kariyapperuma Athukoralalage..."*), extreme rupee totals, and special characters gracefully. |
| **13** | **Lists & Search** | City search debounces keystrokes ($300$ms). Includes a 1-tap clear button ($\times$). Pull-to-refresh resets pagination cleanly. |
| **14** | **Image Fallbacks** | Network images use `cached_network_image` with local asset fallbacks and shimmer placeholders during initial load. |
| **15** | **User Feedback** | Every user action confirms state: *"Seats reserved for 10m"*, *"Booking confirmed"*, *"Review submitted"*, accompanied by light haptic feedback (`HapticFeedback.lightImpact()`). |
| **16** | **Destructive Actions** | Booking cancellation and session logout require an explicit dialog with humanized explanation of the consequences before execution. |
| **17** | **Data Persistence** | App remembers user session, selected theme mode, saved travel companions, and last-searched routes in secure storage. |
| **18** | **App Lifecycle** | Transitioning app from background to foreground automatically checks active seat hold timer and re-syncs state without dumping user out of checkout. |
| **19** | **Permissions** | Camera permission for Conductor QR Scanner requests access with a friendly explanation modal first. Gracefully handles denial with a manual ticket code entry fallback. |
| **20** | **Client Security** | Zero secrets in client code. Bearer JWT tokens stored exclusively in `flutter_secure_storage`. No sensitive passenger data printed to console logs. |
| **21** | **Performance** | Const widget constructors used everywhere possible. Minimal rebuild scopes via targeted `BlocBuilder` selectors. |
| **22** | **Microcopy** | Action-oriented button copy (*"Pay Rs. 3,990"*, *"Choose Seats"*, *"Confirm Boarding"*). Clear, friendly, professional tone throughout. |
| **23** | **Visual Uniformity** | Authoritative `$32DE84` tokens, $16$dp card corner radiuses, consistent $1$dp slate borders, and cohesive Plus Jakarta Sans typography across every screen. |
| **24** | **First-Run Experience** | Concise 3-step onboarding walkthrough that can be skipped in 1 tap, setting expectations without creating friction. |
| **25** | **Functional Settings** | Every toggle in the settings screen (Theme, Travelers, Language) is fully functional and persists across app restarts. |
| **26** | **State Machines** | Strict Cubit state classes (`JourneySearchState`, `SeatHoldState`, `BookingState`, `ThemeState`) driving UI deterministically. |
| **27** | **Quality Bar** | Zero mock placeholder buttons. Real end-to-end flows connecting search $\rightarrow$ seat selection $\rightarrow$ hold timer $\rightarrow$ checkout $\rightarrow$ wallet $\rightarrow$ check-in. |
| **28** | **Final Audit** | Comprehensive screen-by-screen checklist verified prior to merging. |

---

## 6. Implementation & Package Organization Plan

The complete implementation will update or introduce the following files in `mobile/`:

```
mobile/
├── assets/
│   └── icons/
│       └── waypoint_logo.png           # Official WayPoint vector transit mark
├── lib/
│   ├── main.dart                       # Entry point mounting WayPointApp with ThemeCubit
│   ├── core/
│   │   ├── theme/
│   │   │   ├── app_theme.dart          # #32DE84 palette, lightTheme & darkTheme
│   │   │   └── theme_cubit.dart        # ThemeMode state manager & secure storage persistence
│   │   ├── network/
│   │   │   └── api_client.dart         # Dio HTTP client with JWT interceptor & offline fallback
│   │   ├── storage/
│   │   │   └── local_cache_service.dart# Secure storage for tokens, travelers, & offline tickets
│   │   └── widgets/
│   │       ├── waypoint_logo.dart      # Scalable brand logo with transit compass & bus path
│   │       ├── waypoint_button.dart    # #32DE84 primary, secondary, outline buttons with loading state
│   │       ├── waypoint_card.dart      # Dual-theme card with 1px slate border & rounded corners
│   │       ├── transit_badge.dart      # Semantic status badges (Active, Held, Express, Luxury)
│   │       ├── seat_reservation_bar.dart# Floating 10m countdown bar with progress indicator
│   │       ├── shimmer_loading.dart    # Reusable shimmer skeleton cards
│   │       └── empty_state_view.dart   # Universal empty state with icon, message, & action
│   └── features/
│       ├── onboarding/
│       │   └── screens/
│       │       └── onboarding_screen.dart # 3-step illustrated walkthrough carousel
│       ├── auth/
│       │   ├── bloc/auth_cubit.dart    # Authentication & role routing state
│       │   └── screens/
│       │       └── passenger_auth_screen.dart # Clean login & register forms
│       ├── navigation/
│       │   └── screens/
│       │       ├── auth_gate.dart      # Root router checking first-launch & credentials
│       │       ├── passenger_navigation_shell.dart # 4-tab bottom navigation for passengers
│       │       └── conductor_navigation_shell.dart # 4-tab bottom navigation for staff
│       ├── journey/
│       │   ├── models/journey_models.dart
│       │   ├── services/journey_api_service.dart
│       │   └── screens/
│       │       ├── journey_search_screen.dart     # Corridors carousel & route search
│       │       ├── preference_filter_sheet.dart   # Bottom sheet with live counter
│       │       └── journey_comparison_screen.dart # Results with transfer buffer badges
│       ├── booking/
│       │   ├── models/booking_models.dart
│       │   ├── services/booking_api_service.dart
│       │   └── screens/
│       │       ├── seat_picker_screen.dart        # 2x2 interactive seat map with hold lock
│       │       ├── payment_checkout_screen.dart   # Fare accordion & sandbox payment
│       │       ├── ticket_wallet_screen.dart      # Tear-away boarding pass with QR
│       │       └── booking_history_screen.dart    # Trip history & tiered refund modal
│       ├── disruption/
│       │   ├── models/disruption_models.dart
│       │   └── screens/
│       │       └── disruption_alert_screen.dart   # Alerts feed & 1-tap rebooking card
│       ├── conductor/
│       │   └── screens/
│       │       ├── conductor_scanner_screen.dart  # Camera QR scanner & HMAC validation
│       │       └── conductor_manifest_screen.dart # Live passenger boarding roster
│       ├── review/
│       │   └── screens/
│       │       └── review_submission_screen.dart  # 5-star ratings & category tags
│       └── settings/
│           └── screens/
│               └── passenger_settings_screen.dart # Light/Dark switch & saved travelers
```

---

## 7. Verification & Quality Acceptance Criteria

1. **Compilation & Analysis**:
   - `flutter analyze` runs with 0 errors and 0 critical warnings.
   - All unit/widget tests in `mobile/test/` pass cleanly.
2. **Visual & Theme Verification**:
   - Primary color throughout the app is verified as Spring Green `#32DE84`.
   - Switching between Light Mode and Dark Mode in Settings instantly transforms all screens, cards, modals, and sheets.
   - Onboarding carousel is displayed on first launch and skipped on subsequent launches.
3. **End-to-End Booking Flow**:
   - Passenger can search from Colombo to Ella/Kandy $\rightarrow$ view comparison cards $\rightarrow$ select seats in the visual 2x2 map $\rightarrow$ see the 10-minute hold bar tick down $\rightarrow$ simulate sandbox payment $\rightarrow$ receive active digital QR boarding pass in Ticket Wallet.
4. **Offline Resilience**:
   - Confirmed tickets remain readable in Ticket Wallet when device network is toggled off.
5. **Clean Codebase**:
   - No occurrences of student names or academic component tags remain in any user-facing mobile screen.
