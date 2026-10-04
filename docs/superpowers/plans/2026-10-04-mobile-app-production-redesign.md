# WayPoint Mobile Transit Application — Production Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the WayPoint Flutter mobile application into an authentic, production-grade transit app featuring `#32DE84` brand styling, Light/Dark theme switching in settings, a 3-step onboarding walkthrough, an end-to-end booking flow, conductor tools, and strict adherence to the 28-point production engineering & UX matrix.

**Architecture:** Feature-first packaging in `mobile/lib/features/` with reactive BLoC/Cubit state management (`ThemeCubit`, `AuthCubit`). An `AuthGate` dynamically routes between `OnboardingScreen`, `PassengerAuthScreen`, `PassengerNavigationShell` (4 tabs: Explore, My Trips, Alerts, Settings), and `ConductorNavigationShell` (4 tabs: Services, Scanner, Manifest, Profile). An offline-first repository layer with Dio communicates with the ASP.NET Core backend and falls back gracefully to cached datasets.

**Tech Stack:** Flutter 3.x / Dart 3.x, `flutter_bloc`, `dio`, `flutter_secure_storage`, `qr_flutter`, `mobile_scanner`, `intl`.

**Spec:** [`docs/superpowers/specs/2026-10-04-mobile-app-production-redesign.md`](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/superpowers/specs/2026-10-04-mobile-app-production-redesign.md)

## Global Constraints

- Primary Brand Color: `#32DE84` (Spring Green); On-Primary contrast text: `#042611` (WCAG AAA $\ge 7:1$).
- Dark Surface Background: `#090D16`; Dark Card Surface: `#131B2E`; Dark Border: `#23304D`.
- Light Surface Background: `#F8FAFC`; Light Card Surface: `#FFFFFF`; Light Border: `#E2E8F0`.
- Zero academic markers: No student names (*Sethum, Nuhadh, Mithila, Dineth*), no component codes (*Component 1–4*), no *MOB-XX* labels on screens, no debug launcher buttons.
- Minimum interactive touch target bounding box: $48 \times 48$dp.
- Every async operation must explicitly support: `Initial`, `Loading`, `Loaded/Success`, `Empty`, `Error`, `Offline`, `Refreshing`, and `Submitting`.

## Review Focus

1. **Active 10-Minute Hold Expiry Handling**: Active seat hold ticks down across screen transitions; when hold expires, system warns passenger and releases seats gracefully without throwing null errors.
2. **Network Offline & Resilient Caching**: Confirmed boarding passes with cryptographic QR payloads remain fully viewable and verifiable even when the device loses cellular connectivity.
3. **Double-Tap & Duplicate Submission Prevention**: Checkout and seat hold buttons disable immediately upon tap while displaying inline loading spinners.
4. **Instant Theme Switching Persistence**: Toggling between ☀️ Light, 🌙 Dark, and ⚙️ System Default updates all active screens and bottom sheets immediately and persists across app restarts.
5. **Form Input Ergonomics & Keyboard Safety**: Keyboard appearances on small devices never cause yellow-and-black pixel overflow stripes on auth or checkout inputs.

---

### Task 1: Core Design System Tokens (`#32DE84`), Dual Theme Engine (`ThemeCubit`), and WayPoint Brand Logo

**Files:**
- Create: `mobile/lib/core/theme/theme_cubit.dart`
- Create: `mobile/lib/core/widgets/waypoint_logo.dart`
- Modify: `mobile/lib/core/theme/app_theme.dart`
- Test: `mobile/test/core/theme_test.dart`

**Interfaces:**
- Consumes: Flutter `ThemeData`, `flutter_secure_storage`
- Produces: `AppTheme.lightTheme`, `AppTheme.darkTheme`, `ThemeCubit` (exposing `ThemeMode`), `WayPointLogo` widget

- [ ] **Step 1: Write the failing test for Theme and Brand Logo**

```dart
// mobile/test/core/theme_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/theme/app_theme.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

void main() {
  test('AppTheme defines #32DE84 as primary color in both light and dark themes', () {
    expect(AppTheme.primaryColor, const Color(0xFF32DE84));
    expect(AppTheme.onPrimaryColor, const Color(0xFF042611));
    expect(AppTheme.lightTheme.colorScheme.primary, const Color(0xFF32DE84));
    expect(AppTheme.darkTheme.colorScheme.primary, const Color(0xFF32DE84));
    expect(AppTheme.darkTheme.scaffoldBackgroundColor, const Color(0xFF090D16));
  });

  testWidgets('WayPointLogo renders brand text and icon cleanly', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: WayPointLogo(size: 64, showText: true),
        ),
      ),
    );
    expect(find.text('WayPoint'), findsOneWidget);
    expect(find.byType(WayPointLogo), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/core/theme_test.dart`  
Expected: FAIL with missing properties / color mismatch.

- [ ] **Step 3: Implement `app_theme.dart`, `theme_cubit.dart`, and `waypoint_logo.dart`**

Implement `AppTheme` with calibrated `#32DE84`, dark surfaces `#090D16`/`#131B2E`, light surfaces `#F8FAFC`/`#FFFFFF`. Implement `ThemeCubit` with `ThemeMode.light`, `ThemeMode.dark`, and `ThemeMode.system` backed by `flutter_secure_storage`. Implement `WayPointLogo` with transit compass geometry in `#32DE84`.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/core/theme_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/core/theme/ mobile/lib/core/widgets/waypoint_logo.dart mobile/test/core/theme_test.dart
git commit -m "feat(mobile): implement #32DE84 color system, ThemeCubit, and WayPointLogo"
```

---

### Task 2: Core Shared UI Primitives (Buttons, Cards, Badges, Shimmer, Empty States)

**Files:**
- Modify: `mobile/lib/core/widgets/waypoint_button.dart`
- Modify: `mobile/lib/core/widgets/waypoint_card.dart`
- Modify: `mobile/lib/core/widgets/transit_badge.dart`
- Create: `mobile/lib/core/widgets/shimmer_loading.dart`
- Create: `mobile/lib/core/widgets/empty_state_view.dart`
- Test: `mobile/test/core/widgets_test.dart`

**Interfaces:**
- Consumes: `AppTheme`
- Produces: `WayPointButton` (with loading & haptic feedback), `WayPointCard`, `TransitBadge`, `ShimmerLoadingCard`, `EmptyStateView`

- [ ] **Step 1: Write the failing test for UI Primitives**

```dart
// mobile/test/core/widgets_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_button.dart';
import 'package:waypoint_mobile/core/widgets/empty_state_view.dart';

void main() {
  testWidgets('WayPointButton shows spinner when isLoading is true and suppresses tap', (tester) async {
    bool tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: WayPointButton(
            text: 'Reserve Seats',
            isLoading: true,
            onPressed: () => tapped = true,
          ),
        ),
      ),
    );
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    await tester.tap(find.byType(WayPointButton));
    expect(tapped, isFalse);
  });

  testWidgets('EmptyStateView renders title, description, and action button', (tester) async {
    bool actionClicked = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: EmptyStateView(
            title: 'No Trips Found',
            description: 'Try adjusting your filters.',
            actionLabel: 'Search Again',
            onAction: () => actionClicked = true,
          ),
        ),
      ),
    );
    expect(find.text('No Trips Found'), findsOneWidget);
    expect(find.text('Search Again'), findsOneWidget);
    await tester.tap(find.text('Search Again'));
    expect(actionClicked, isTrue);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/core/widgets_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Implement UI Primitives**

Update `waypoint_button.dart` to support `#32DE84`, height $\ge 48$dp, spinner indicator, disabled state, and `HapticFeedback.lightImpact()`. Update `waypoint_card.dart` with dual-theme surface and 16dp radius. Implement `ShimmerLoadingCard` and `EmptyStateView`.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/core/widgets_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/core/widgets/ mobile/test/core/widgets_test.dart
git commit -m "feat(mobile): upgrade shared UI primitives with 28-point UX standards"
```

---

### Task 3: Resilient API Client & Secure Local Cache Service

**Files:**
- Create: `mobile/lib/core/storage/local_cache_service.dart`
- Create: `mobile/lib/core/network/api_client.dart`
- Test: `mobile/test/core/network_cache_test.dart`

**Interfaces:**
- Consumes: `dio`, `flutter_secure_storage`
- Produces: `LocalCacheService` (token, travelers, cached tickets), `ApiClient` (Dio instance with JWT bearer token and offline fallback)

- [ ] **Step 1: Write the failing test for Local Cache Service**

```dart
// mobile/test/core/network_cache_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/storage/local_cache_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  test('LocalCacheService saves and retrieves token and first-run flag', () async {
    final cache = LocalCacheService();
    await cache.saveAuthToken('test-jwt-token-123');
    expect(await cache.getAuthToken(), 'test-jwt-token-123');
    await cache.setFirstRunCompleted(true);
    expect(await cache.isFirstRunCompleted(), isTrue);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/core/network_cache_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Implement `local_cache_service.dart` and `api_client.dart`**

Implement secure storage wrapper with fallback in-memory cache for test harnesses. Implement `ApiClient` with 10s connect/receive timeouts, automatic bearer token injection, and humanized error mapping (`ApiException`).

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/core/network_cache_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/core/storage/ mobile/lib/core/network/ mobile/test/core/network_cache_test.dart
git commit -m "feat(mobile): add secure local cache service and resilient ApiClient"
```

---

### Task 4: Animated Splash, 3-Step Onboarding Carousel, and Clean Authentication

**Files:**
- Create: `mobile/lib/features/onboarding/screens/onboarding_screen.dart`
- Create: `mobile/lib/features/auth/bloc/auth_cubit.dart`
- Modify: `mobile/lib/features/auth/screens/passenger_auth_screen.dart`
- Create: `mobile/lib/features/navigation/screens/auth_gate.dart`
- Test: `mobile/test/features/auth_onboarding_test.dart`

**Interfaces:**
- Consumes: `LocalCacheService`, `ApiClient`, `WayPointLogo`, `WayPointButton`
- Produces: `AuthGate`, `OnboardingScreen`, `PassengerAuthScreen`, `AuthCubit`

- [ ] **Step 1: Write the failing test for Onboarding and AuthGate**

```dart
// mobile/test/features/auth_onboarding_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/onboarding/screens/onboarding_screen.dart';

void main() {
  testWidgets('OnboardingScreen renders 3-step carousel with Skip and Get Started', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: OnboardingScreen(),
      ),
    );
    expect(find.text('Explore Sri Lanka'), findsOneWidget);
    expect(find.text('Skip'), findsOneWidget);
    expect(find.text('Next'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/auth_onboarding_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Implement `OnboardingScreen`, `AuthCubit`, `PassengerAuthScreen`, and `AuthGate`**

Build 3-step PageView with dot indicators and "Get Started" in `#32DE84`. Purge all academic strings from `PassengerAuthScreen`. Implement `AuthGate` checking `isFirstRunCompleted` and `getAuthToken`.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/auth_onboarding_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/onboarding/ mobile/lib/features/auth/ mobile/lib/features/navigation/ mobile/test/features/auth_onboarding_test.dart
git commit -m "feat(mobile): implement onboarding walkthrough, AuthCubit, and AuthGate router"
```

---

### Task 5: Production Navigation Shells (Passenger & Conductor)

**Files:**
- Create: `mobile/lib/features/navigation/screens/passenger_navigation_shell.dart`
- Create: `mobile/lib/features/navigation/screens/conductor_navigation_shell.dart`
- Modify: `mobile/lib/main.dart`
- Test: `mobile/test/features/navigation_shells_test.dart`

**Interfaces:**
- Consumes: `ThemeCubit`, `AuthCubit`
- Produces: `PassengerNavigationShell` (4 tabs: Explore, Trips, Alerts, Settings), `ConductorNavigationShell` (4 tabs: Services, Scanner, Manifest, Profile)

- [ ] **Step 1: Write the failing test for PassengerNavigationShell**

```dart
// mobile/test/features/navigation_shells_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/navigation/screens/passenger_navigation_shell.dart';

void main() {
  testWidgets('PassengerNavigationShell renders 4 tabs: Explore, My Trips, Alerts, Settings', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: PassengerNavigationShell(),
      ),
    );
    expect(find.text('Explore'), findsOneWidget);
    expect(find.text('My Trips'), findsOneWidget);
    expect(find.text('Alerts'), findsOneWidget);
    expect(find.text('Settings'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/navigation_shells_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Implement Navigation Shells and update `main.dart`**

Build `PassengerNavigationShell` and `ConductorNavigationShell` with bottom navigation bars in `#32DE84`. Update `main.dart` to mount `BlocProvider<ThemeCubit>` and set `home: const AuthGate()`, eliminating all old component hub tabs.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/navigation_shells_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/navigation/ mobile/lib/main.dart mobile/test/features/navigation_shells_test.dart
git commit -m "feat(mobile): mount PassengerNavigationShell and ConductorNavigationShell in main.dart"
```

---

### Task 6: Explore & Journey Discovery (Corridors Carousel, Search, Preference Filters, and Results)

**Files:**
- Modify: `mobile/lib/features/journey/screens/journey_search_screen.dart`
- Modify: `mobile/lib/features/journey/widgets/preference_filter_sheet.dart`
- Modify: `mobile/lib/features/journey/screens/journey_comparison_screen.dart`
- Test: `mobile/test/features/journey_search_test.dart`

**Interfaces:**
- Consumes: `JourneyApiService`, `WayPointCard`, `WayPointButton`, `TransitBadge`
- Produces: Interactive route search, filter bottom sheet with dynamic match counter, candidate cards with `BR-TRANSFER-001` buffer badges

- [ ] **Step 1: Write the failing test for Journey Search & Filters**

```dart
// mobile/test/features/journey_search_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/screens/journey_search_screen.dart';

void main() {
  testWidgets('JourneySearchScreen contains corridors carousel and quick swap button', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: JourneySearchScreen()),
      ),
    );
    expect(find.text('Explore Corridors'), findsOneWidget);
    expect(find.byIcon(Icons.swap_vert), findsOneWidget);
    expect(find.text('Search Buses'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/journey_search_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Update `journey_search_screen.dart`, `preference_filter_sheet.dart`, and `journey_comparison_screen.dart`**

Add scenic corridors carousel (Colombo $\rightarrow$ Ella, Kandy, Galle, Jaffna). Add quick-swap button ($\rightleftharpoons$) and date chips (*Today, Tomorrow, Weekend*). Update filter sheet with dynamic count badge (*"Show 6 Buses"*). Render safe buffer badges (`BR-TRANSFER-001`) on comparison cards. Purge all academic labels.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/journey_search_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/journey/ mobile/test/features/journey_search_test.dart
git commit -m "feat(mobile): overhaul journey search with corridors carousel and safe transfer badges"
```

---

### Task 7: Interactive Seat Map, Server-Side Hold Bar, and Stop Timeline

**Files:**
- Create: `mobile/lib/core/widgets/seat_reservation_bar.dart`
- Modify: `mobile/lib/features/booking/screens/seat_picker_screen.dart`
- Test: `mobile/test/features/seat_picker_test.dart`

**Interfaces:**
- Consumes: `FleetApiService`, `AppTheme`
- Produces: `SeatPickerScreen` (2x2 coach layout with driver cabin), `SeatReservationBar` (floating 10m countdown bar)

- [ ] **Step 1: Write the failing test for SeatPickerScreen and Hold Bar**

```dart
// mobile/test/features/seat_picker_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/widgets/seat_reservation_bar.dart';

void main() {
  testWidgets('SeatReservationBar displays remaining hold time and selected seats', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          bottomNavigationBar: SeatReservationBar(
            remainingSeconds: 580,
            selectedSeats: const ['12A', '12B'],
            totalAmount: 4800,
            onContinue: () {},
          ),
        ),
      ),
    );
    expect(find.textContaining('Seats 12A, 12B'), findsOneWidget);
    expect(find.textContaining('09:40'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/seat_picker_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Implement `SeatReservationBar` and update `SeatPickerScreen`**

Build 2x2 visual seat map with driver steering wheel icon, entrance door, and back row. Selected seats illuminate in `#32DE84` with checkmarks and haptic vibration. Floating countdown bar updates every second, shifting from `#32DE84` $\rightarrow$ Amber $\rightarrow$ Crimson. Purge all academic labels.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/seat_picker_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/core/widgets/seat_reservation_bar.dart mobile/lib/features/booking/screens/seat_picker_screen.dart mobile/test/features/seat_picker_test.dart
git commit -m "feat(mobile): implement interactive 2x2 seat map with persistent 10m hold bar"
```

---

### Task 8: Checkout Sandbox with Promo Code & Payment Simulation

**Files:**
- Modify: `mobile/lib/features/booking/screens/payment_checkout_screen.dart`
- Test: `mobile/test/features/payment_checkout_test.dart`

**Interfaces:**
- Consumes: `SeatHoldInfo`, `BookingApiService`, `ApiClient`
- Produces: `PaymentCheckoutScreen` with stop timeline, fare breakdown accordion, promo code `WAYPOINT20`, card entry, and payment simulation buttons

- [ ] **Step 1: Write the failing test for PaymentCheckoutScreen**

```dart
// mobile/test/features/payment_checkout_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/booking/models/booking_models.dart';
import 'package:waypoint_mobile/features/booking/screens/payment_checkout_screen.dart';

void main() {
  testWidgets('PaymentCheckoutScreen renders fare accordion and sandbox simulation buttons', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: PaymentCheckoutScreen(
          holdInfo: SeatHoldInfo.sampleColomboToElla(),
        ),
      ),
    );
    expect(find.text('Fare Breakdown'), findsOneWidget);
    expect(find.text('Simulate Sandbox Payment'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/payment_checkout_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Update `payment_checkout_screen.dart`**

Add stop timeline showing pickup point landmarks. Add fare breakdown accordion with promo discount code `WAYPOINT20` (-20%) and optional travel insurance (+Rs. 150). Add card preview form with masked numbers. Add sandbox test buttons (*"Pay Success"*, *"Simulate Decline"*, *"Simulate Timeout"*). Link to `POST /api/v1/bookings/checkout`. Purge all academic labels.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/payment_checkout_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/booking/screens/payment_checkout_screen.dart mobile/test/features/payment_checkout_test.dart
git commit -m "feat(mobile): build production checkout screen with promo code and sandbox simulation"
```

---

### Task 9: Digital QR Ticket Wallet, Offline Pass, and Tiered Refund Modal

**Files:**
- Modify: `mobile/lib/features/booking/screens/ticket_wallet_screen.dart`
- Modify: `mobile/lib/features/booking/screens/booking_history_screen.dart`
- Test: `mobile/test/features/ticket_wallet_test.dart`

**Interfaces:**
- Consumes: `qr_flutter`, `LocalCacheService`
- Produces: `TicketWalletScreen` (tear-away boarding pass with brightness booster and offline pass storage), `BookingHistoryScreen` (with `BR-REFUND-001` tiered cancellation calculator)

- [ ] **Step 1: Write the failing test for Ticket Wallet and Refunds**

```dart
// mobile/test/features/ticket_wallet_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/booking/screens/ticket_wallet_screen.dart';

void main() {
  testWidgets('TicketWalletScreen renders dynamic QR pass and brightness booster button', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: TicketWalletScreen(),
      ),
    );
    expect(find.text('Digital Boarding Pass'), findsOneWidget);
    expect(find.byIcon(Icons.brightness_high), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/ticket_wallet_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Update `ticket_wallet_screen.dart` and `booking_history_screen.dart`**

Design tear-away boarding pass with dynamic QR code and HMAC string. Add 1-tap screen brightness booster button. Automatically cache tickets locally in `LocalCacheService` for offline access. Implement tiered cancellation modal calculating refund percentages (`>24h: 90%`, `12-24h: 50%`, `<12h: 0%`). Purge all academic labels.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/ticket_wallet_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/booking/screens/ticket_wallet_screen.dart mobile/lib/features/booking/screens/booking_history_screen.dart mobile/test/features/ticket_wallet_test.dart
git commit -m "feat(mobile): upgrade ticket wallet with tear-away pass, offline cache, and tiered refunds"
```

---

### Task 10: Disruption Alerts Feed & 1-Tap Rebooking Card

**Files:**
- Modify: `mobile/lib/features/disruption/screens/disruption_alert_screen.dart`
- Test: `mobile/test/features/disruption_alert_test.dart`

**Interfaces:**
- Consumes: `DisruptionAlertModel`, `ApiClient`
- Produces: `DisruptionAlertScreen` with unread notification feed and side-by-side alternative bus rebooking card with 1-tap acceptance

- [ ] **Step 1: Write the failing test for Disruption Alert Screen**

```dart
// mobile/test/features/disruption_alert_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/disruption/models/disruption_models.dart';
import 'package:waypoint_mobile/features/disruption/screens/disruption_alert_screen.dart';

void main() {
  testWidgets('DisruptionAlertScreen renders side-by-side rebooking card with 1-tap accept', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: DisruptionAlertScreen(
          disruption: DisruptionAlertModel.sampleColomboToElla(),
        ),
      ),
    );
    expect(find.text('Accept Replacement Bus'), findsOneWidget);
    expect(find.text('Request 100% Refund'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/disruption_alert_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Update `disruption_alert_screen.dart`**

Build filterable notification feed with unread counter. Render side-by-side alternative service comparison (Original bus vs Replacement bus, seat transfer, fare coverage). Add 1-tap *"Accept Replacement Bus"* and *"Request 100% Refund"* actions. Purge all academic labels.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/disruption_alert_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/disruption/screens/disruption_alert_screen.dart mobile/test/features/disruption_alert_test.dart
git commit -m "feat(mobile): build disruption alerts center with 1-tap rebooking acceptance"
```

---

### Task 11: Settings & Profile with Light/Dark Switch, Saved Travelers, and Review Submission

**Files:**
- Create: `mobile/lib/features/settings/screens/passenger_settings_screen.dart`
- Modify: `mobile/lib/features/fleet/presentation/screens/review_submission_screen.dart`
- Test: `mobile/test/features/settings_reviews_test.dart`

**Interfaces:**
- Consumes: `ThemeCubit`, `AuthCubit`, `LocalCacheService`
- Produces: `PassengerSettingsScreen` (3-way theme toggle, saved travelers, logout), `ReviewSubmissionScreen` (5-star ratings & category tags)

- [ ] **Step 1: Write the failing test for Settings Screen**

```dart
// mobile/test/features/settings_reviews_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:waypoint_mobile/core/theme/theme_cubit.dart';
import 'package:waypoint_mobile/features/settings/screens/passenger_settings_screen.dart';

void main() {
  testWidgets('PassengerSettingsScreen renders Light, Dark, and System theme selectors', (tester) async {
    await tester.pumpWidget(
      BlocProvider(
        create: (_) => ThemeCubit(),
        child: const MaterialApp(
          home: PassengerSettingsScreen(),
        ),
      ),
    );
    expect(find.text('Appearance'), findsOneWidget);
    expect(find.text('Light'), findsOneWidget);
    expect(find.text('Dark'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/settings_reviews_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Implement `passenger_settings_screen.dart` and update `review_submission_screen.dart`**

Build 3-way segmented control for ☀️ Light, 🌙 Dark, and ⚙️ System Default connected to `ThemeCubit`. Add saved travelers directory (add/edit/delete companion). Add 1955 Transit Helpline dialer. Upgrade review submission modal with 5-star ratings, fast-tag pills, and multi-line comments. Purge all academic labels.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/settings_reviews_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/settings/ mobile/lib/features/fleet/presentation/screens/review_submission_screen.dart mobile/test/features/settings_reviews_test.dart
git commit -m "feat(mobile): add settings screen with Light/Dark switcher and upgrade review submission"
```

---

### Task 12: Conductor Scanner & Manifest Operations Suite

**Files:**
- Modify: `mobile/lib/features/fleet/screens/conductor_scanner_screen.dart`
- Modify: `mobile/lib/features/fleet/screens/conductor_manifest_screen.dart`
- Test: `mobile/test/features/conductor_tools_test.dart`

**Interfaces:**
- Consumes: `mobile_scanner`, `TicketController` API
- Produces: `ConductorScannerScreen` (camera scanner with torch and HMAC verification), `ConductorManifestScreen` (searchable boarding roster with 1-tap check-in)

- [ ] **Step 1: Write the failing test for Conductor Tools**

```dart
// mobile/test/features/conductor_tools_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/fleet/screens/conductor_manifest_screen.dart';

void main() {
  testWidgets('ConductorManifestScreen renders passenger roster with filter pills', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: ConductorManifestScreen(),
      ),
    );
    expect(find.text('Passenger Manifest'), findsOneWidget);
    expect(find.text('All'), findsOneWidget);
    expect(find.text('Boarded'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile; flutter test test/features/conductor_tools_test.dart`  
Expected: FAIL.

- [ ] **Step 3: Update `conductor_scanner_screen.dart` and `conductor_manifest_screen.dart`**

Add camera viewfinder with flashlight torch toggle and manual booking reference entry fallback. On scan, display green valid or red invalid verification card. Build passenger manifest with search bar, *All/Boarded/Pending* filter pills, and 1-tap check-in toggle. Purge all academic labels.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile; flutter test test/features/conductor_tools_test.dart`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/fleet/screens/ mobile/test/features/conductor_tools_test.dart
git commit -m "feat(mobile): polish conductor QR scanner and manifest roster"
```

---

### Task 13: Final Audit, Quality Acceptance, and Verification

**Files:**
- Verify: Entire `mobile/` project

**Interfaces:**
- Consumes: Flutter SDK, `flutter analyze`, `flutter test`

- [ ] **Step 1: Run static analysis across the entire mobile codebase**

Run: `cd mobile; flutter analyze`  
Expected: 0 errors and 0 critical warnings.

- [ ] **Step 2: Run complete test suite**

Run: `cd mobile; flutter test`  
Expected: All tests PASS.

- [ ] **Step 3: Perform 28-point production checklist audit**

Verify:
- [x] `#32DE84` brand styling consistent across all screens
- [x] Light / Dark mode switcher functional and persisted
- [x] Onboarding carousel renders on first launch and skips on next
- [x] 10-minute seat hold timer ticks down and releases cleanly
- [x] Zero occurrences of student names (*Sethum, Nuhadh, Mithila, Dineth*) or *Component 1–4* in user-facing UI
- [x] Offline pass viewable without network
- [x] Touch targets $\ge 48$dp with haptic feedback

- [ ] **Step 4: Commit**

```bash
git add mobile/
git commit -m "chore(mobile): complete production audit and verification pass"
```
