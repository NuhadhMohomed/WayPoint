# Mobile App Identity, Onboarding & First-Run Experience Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish WayPoint mobile's unified brand identity, branded cold-start splash, 3-slide passenger onboarding carousel, guest route discovery with first-run guidance, and permanent onboarding skip for returning users.

**Architecture:** Update `WayPointLogo` as the universal visual token; extend `SecureStorageService` with persistent onboarding completion tracking; rewire `AuthGate` to orchestrate cold-start states (`BrandedSplashScreen` → `OnboardingScreen` → `PassengerNavigationShell` in guest or authenticated mode); add `FirstRunWelcomeCard` to the Explore screen and `AuthPromptModal` for guest checkout gating.

**Tech Stack:** Flutter 3, Dart 3, flutter_bloc, flutter_secure_storage, flutter_test.

**Spec:** [docs/superpowers/specs/2026-10-06-mobile-app-identity-onboarding-design.md](file:///c:/Users/Nuhad/Documents/GitHub/WayPoint/docs/superpowers/specs/2026-10-06-mobile-app-identity-onboarding-design.md)

## Global Constraints

- Flutter clients must communicate exclusively through ASP.NET Core (`http://localhost:5010/api/v1`); direct database or external connections are prohibited (`AGENTS.md`).
- Brand color palette is anchored on Spring Green (`#32DE84`) and Forest Navy (`#042611`) with WCAG AAA contrast ratio ($\ge 7:1$).
- Onboarding state must persist across cold restarts via `SecureStorageService`; returning users must never see onboarding again.
- Guest users must be able to explore routes and view seat layouts without an upfront login wall, requiring authentication only when reserving a seat.
- Every task must follow strict TDD: failing test first, verification, minimal code, passing test, commit.

## Review Focus

1. First launch with no stored preferences: Expect `BrandedSplashScreen` briefly, then `OnboardingScreen` without rendering unstyled default containers.
2. User taps "Skip" on slide 1: Expect immediate storage write of `has_completed_onboarding: true` and transition directly to `PassengerNavigationShell`.
3. Cold app restart for a returning user: Expect immediate bypass of `OnboardingScreen` directly into the Explore shell in <300ms.
4. Guest passenger taps "Reserve Seat" on seat matrix: Expect `AuthPromptModal` bottom sheet without throwing a null user exception or crash.
5. First-run welcome card corridor tap: Expect search origin and destination to immediately populate with selected expressway stops and card to dismiss upon search execution.

---

### Task 1: Core Brand Identity & Logo System Refinement

**Files:**
- Modify: `mobile/lib/core/widgets/waypoint_logo.dart`
- Modify: `mobile/lib/features/auth/screens/passenger_auth_screen.dart:86-137`
- Test: `mobile/test/core/widgets/waypoint_logo_test.dart`

**Interfaces:**
- Produces: `enum WayPointLogoVariant { iconOnly, horizontal, stacked }`
- Produces: `WayPointLogo({Key? key, double size, WayPointLogoVariant variant, bool isDark})`

- [ ] **Step 1: Write the failing widget test for WayPointLogo variants**

```dart
// mobile/test/core/widgets/waypoint_logo_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';
import 'package:waypoint_mobile/core/theme/app_theme.dart';

void main() {
  testWidgets('renders iconOnly variant without title or subtitle', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: WayPointLogo(size: 32, variant: WayPointLogoVariant.iconOnly),
        ),
      ),
    );

    expect(find.byIcon(Icons.alt_route_rounded), findsOneWidget);
    expect(find.text('WayPoint'), findsNothing);
    expect(find.text('Sri Lanka Transit'), findsNothing);
  });

  testWidgets('renders stacked variant with centered title and subtitle', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: WayPointLogo(size: 64, variant: WayPointLogoVariant.stacked),
        ),
      ),
    );

    expect(find.byIcon(Icons.alt_route_rounded), findsOneWidget);
    expect(find.text('WayPoint'), findsOneWidget);
    expect(find.text('Sri Lanka Transit'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/core/widgets/waypoint_logo_test.dart`
Expected: FAIL with compilation error on `WayPointLogoVariant`.

- [ ] **Step 3: Implement `WayPointLogoVariant` and clean up `PassengerAuthScreen`**

In `mobile/lib/core/widgets/waypoint_logo.dart`:
- Add `enum WayPointLogoVariant { iconOnly, horizontal, stacked }`.
- Update `WayPointLogo` constructor and `build` method to layout `iconOnly`, `horizontal` (Row), or `stacked` (Column).
- In `mobile/lib/features/auth/screens/passenger_auth_screen.dart`, replace the legacy `// Velora Logo Header` container with `const WayPointLogo(size: 64, variant: WayPointLogoVariant.stacked)`.

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/core/widgets/waypoint_logo_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/core/widgets/waypoint_logo.dart mobile/lib/features/auth/screens/passenger_auth_screen.dart mobile/test/core/widgets/waypoint_logo_test.dart
git commit -m "feat(mobile): refine WayPointLogo variants and unify brand header in auth"
```

---

### Task 2: Storage Persistence for Onboarding Flag

**Files:**
- Modify: `mobile/lib/core/storage/secure_storage_service.dart:10-50`
- Test: `mobile/test/core/storage/secure_storage_service_test.dart`

**Interfaces:**
- Produces: `Future<bool> isOnboardingCompleted()`
- Produces: `Future<void> setOnboardingCompleted({bool completed = true})`

- [ ] **Step 1: Write the failing unit test for onboarding flag persistence**

```dart
// mobile/test/core/storage/secure_storage_service_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}

void main() {
  late MockFlutterSecureStorage mockStorage;
  late SecureStorageService service;

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    service = SecureStorageService(storage: mockStorage);
  });

  test('isOnboardingCompleted returns false when key is absent', () async {
    when(() => mockStorage.read(key: 'waypoint_has_completed_onboarding'))
        .thenAnswer((_) async => null);

    final completed = await service.isOnboardingCompleted();
    expect(completed, isFalse);
  });

  test('setOnboardingCompleted writes true string to storage', () async {
    when(() => mockStorage.write(
          key: 'waypoint_has_completed_onboarding',
          value: 'true',
        )).thenAnswer((_) async => {});

    await service.setOnboardingCompleted();
    verify(() => mockStorage.write(
          key: 'waypoint_has_completed_onboarding',
          value: 'true',
        )).called(1);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/core/storage/secure_storage_service_test.dart`
Expected: FAIL with "The method 'isOnboardingCompleted' isn't defined".

- [ ] **Step 3: Implement `isOnboardingCompleted` and `setOnboardingCompleted`**

In `mobile/lib/core/storage/secure_storage_service.dart`:
```dart
static const String _onboardingKey = 'waypoint_has_completed_onboarding';

Future<bool> isOnboardingCompleted() async {
  final val = await _storage.read(key: _onboardingKey);
  return val == 'true';
}

Future<void> setOnboardingCompleted({bool completed = true}) async {
  await _storage.write(key: _onboardingKey, value: completed ? 'true' : 'false');
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/core/storage/secure_storage_service_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/core/storage/secure_storage_service.dart mobile/test/core/storage/secure_storage_service_test.dart
git commit -m "feat(mobile): add onboarding completion persistence in SecureStorageService"
```

---

### Task 3: Branded Splash Screen & Cold-Start Gate State Machine

**Files:**
- Create: `mobile/lib/features/navigation/screens/branded_splash_screen.dart`
- Modify: `mobile/lib/features/navigation/screens/auth_gate.dart:10-48`
- Test: `mobile/test/features/navigation/screens/branded_splash_screen_test.dart`
- Test: `mobile/test/features/navigation/screens/auth_gate_test.dart`

**Interfaces:**
- Consumes: `SecureStorageService.isOnboardingCompleted()`
- Produces: `BrandedSplashScreen`
- Modifies: `AuthGate` to branch into `OnboardingScreen` if `!isOnboardingCompleted`

- [ ] **Step 1: Write the failing widget test for BrandedSplashScreen**

```dart
// mobile/test/features/navigation/screens/branded_splash_screen_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/navigation/screens/branded_splash_screen.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

void main() {
  testWidgets('renders WayPointLogo, tagline and version info', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: BrandedSplashScreen(),
      ),
    );

    expect(find.byType(WayPointLogo), findsOneWidget);
    expect(find.text('Sri Lanka Intercity Express Network'), findsOneWidget);
    expect(find.text('v0.1.0 • WayPoint'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/navigation/screens/branded_splash_screen_test.dart`
Expected: FAIL with "Target of URI doesn't exist: branded_splash_screen.dart".

- [ ] **Step 3: Implement `BrandedSplashScreen` and integrate into `AuthGate`**

In `mobile/lib/features/navigation/screens/branded_splash_screen.dart`:
- Center `WayPointLogo(size: 72, variant: WayPointLogoVariant.stacked)`.
- Tagline: *"Sri Lanka Intercity Express Network"*.
- Thin 2px `LinearProgressIndicator` in Spring Green (`#32DE84`).
- Version text: *"v0.1.0 • WayPoint"*.

In `mobile/lib/features/navigation/screens/auth_gate.dart`:
- Replace raw `CircularProgressIndicator` with `const BrandedSplashScreen()`.
- Check `storageService.isOnboardingCompleted()`. If false, navigate/render `OnboardingScreen`.

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/navigation/screens/branded_splash_screen_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/navigation/screens/branded_splash_screen.dart mobile/lib/features/navigation/screens/auth_gate.dart mobile/test/features/navigation/screens/branded_splash_screen_test.dart
git commit -m "feat(mobile): add BrandedSplashScreen and integrate into AuthGate cold start"
```

---

### Task 4: 3-Slide Passenger Onboarding Carousel

**Files:**
- Create: `mobile/lib/features/onboarding/screens/onboarding_screen.dart`
- Test: `mobile/test/features/onboarding/screens/onboarding_screen_test.dart`

**Interfaces:**
- Consumes: `SecureStorageService.setOnboardingCompleted()`
- Produces: `OnboardingScreen({Key? key, required VoidCallback onFinish})`

- [ ] **Step 1: Write the failing widget test for OnboardingScreen**

```dart
// mobile/test/features/onboarding/screens/onboarding_screen_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/onboarding/screens/onboarding_screen.dart';

void main() {
  testWidgets('displays slide 1 content and Skip button', (tester) async {
    bool finished = false;
    await tester.pumpWidget(
      MaterialApp(
        home: OnboardingScreen(onFinish: () => finished = true),
      ),
    );

    expect(find.text('Explore Intercity Transit'), findsOneWidget);
    expect(find.text('Skip'), findsOneWidget);
    expect(find.text('Next'), findsOneWidget);

    await tester.tap(find.text('Skip'));
    await tester.pump();
    expect(finished, isTrue);
  });

  testWidgets('advances slides and shows Get Started on last slide', (tester) async {
    bool finished = false;
    await tester.pumpWidget(
      MaterialApp(
        home: OnboardingScreen(onFinish: () => finished = true),
      ),
    );

    // Slide 1 -> Slide 2
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(find.text('Pick Your Exact Seat'), findsOneWidget);

    // Slide 2 -> Slide 3
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    expect(find.text('Board with Offline QR Tickets'), findsOneWidget);
    expect(find.text('Get Started'), findsOneWidget);

    await tester.tap(find.text('Get Started'));
    await tester.pump();
    expect(finished, isTrue);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/onboarding/screens/onboarding_screen_test.dart`
Expected: FAIL with "Target of URI doesn't exist: onboarding_screen.dart".

- [ ] **Step 3: Implement `OnboardingScreen`**

In `mobile/lib/features/onboarding/screens/onboarding_screen.dart`:
- Implement a `PageView` with 3 slides:
  1. *Explore Intercity Transit* with `Icons.alt_route_rounded` in Spring Green.
  2. *Pick Your Exact Seat* with `Icons.event_seat_rounded` in Amber.
  3. *Board with Offline QR Tickets* with `Icons.qr_code_2_rounded` in Sky Blue.
- Top bar with "Skip" action.
- Bottom navigation with animated dots and "Next" / "Get Started" button.
- On "Skip" or "Get Started", invoke `onFinish()`.

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/onboarding/screens/onboarding_screen_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/onboarding/screens/onboarding_screen.dart mobile/test/features/onboarding/screens/onboarding_screen_test.dart
git commit -m "feat(mobile): create 3-slide passenger OnboardingScreen with progression"
```

---

### Task 5: Explore Screen First-Run Guidance

**Files:**
- Create: `mobile/lib/features/journey/widgets/first_run_welcome_card.dart`
- Modify: `mobile/lib/features/journey/screens/journey_search_screen.dart`
- Test: `mobile/test/features/journey/widgets/first_run_welcome_card_test.dart`

**Interfaces:**
- Produces: `FirstRunWelcomeCard({required Function(String origin, String dest) onSelectCorridor, required VoidCallback onDismiss})`

- [ ] **Step 1: Write the failing widget test for FirstRunWelcomeCard**

```dart
// mobile/test/features/journey/widgets/first_run_welcome_card_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/widgets/first_run_welcome_card.dart';

void main() {
  testWidgets('renders corridor chips and invokes callback on tap', (tester) async {
    String selectedOrigin = '';
    String selectedDest = '';
    bool dismissed = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: FirstRunWelcomeCard(
            onSelectCorridor: (orig, dest) {
              selectedOrigin = orig;
              selectedDest = dest;
            },
            onDismiss: () => dismissed = true,
          ),
        ),
      ),
    );

    expect(find.text('Welcome to WayPoint! 🚌'), findsOneWidget);
    expect(find.text('Colombo ⇄ Galle'), findsOneWidget);

    await tester.tap(find.text('Colombo ⇄ Galle'));
    expect(selectedOrigin, 'Colombo');
    expect(selectedDest, 'Galle');

    await tester.tap(find.byIcon(Icons.close_rounded));
    expect(dismissed, isTrue);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/journey/widgets/first_run_welcome_card_test.dart`
Expected: FAIL with "Target of URI doesn't exist: first_run_welcome_card.dart".

- [ ] **Step 3: Implement `FirstRunWelcomeCard` and mount in `JourneySearchScreen`**

In `mobile/lib/features/journey/widgets/first_run_welcome_card.dart`:
- Card layout with Spring Green surface accent.
- Title: *"Welcome to WayPoint! 🚌"*.
- Subtitle: *"Search intercity express buses or tap a popular corridor below to get started."*
- Action chips:
  - `Colombo ⇄ Galle` (origin: Colombo, dest: Galle)
  - `Colombo ⇄ Kandy` (origin: Colombo, dest: Kandy)
  - `Colombo ⇄ Jaffna` (origin: Colombo, dest: Jaffna)
- Dismiss close button.

In `mobile/lib/features/journey/screens/journey_search_screen.dart`:
- Maintain state `bool _showFirstRunCard = true`.
- Mount `FirstRunWelcomeCard` above the search card.
- Tapping a corridor chip sets `_originController.text` and `_destinationController.text`.
- Executing a search dismisses the card (`_showFirstRunCard = false`).

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/journey/widgets/first_run_welcome_card_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/journey/widgets/first_run_welcome_card.dart mobile/lib/features/journey/screens/journey_search_screen.dart mobile/test/features/journey/widgets/first_run_welcome_card_test.dart
git commit -m "feat(mobile): add FirstRunWelcomeCard with expressway corridor discovery"
```

---

### Task 6: Guest Browsing & Reservation Auth Gate Modal

**Files:**
- Create: `mobile/lib/features/booking/widgets/auth_prompt_modal.dart`
- Modify: `mobile/lib/features/navigation/screens/passenger_navigation_shell.dart:14-72`
- Modify: `mobile/lib/features/booking/screens/seat_picker_screen.dart`
- Test: `mobile/test/features/booking/widgets/auth_prompt_modal_test.dart`

**Interfaces:**
- Produces: `AuthPromptModal({required VoidCallback onSignIn, required VoidCallback onCancel})`
- Modifies: `PassengerNavigationShell` to accept nullable `UserModel? user`

- [ ] **Step 1: Write the failing widget test for AuthPromptModal**

```dart
// mobile/test/features/booking/widgets/auth_prompt_modal_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/booking/widgets/auth_prompt_modal.dart';
import 'package:waypoint_mobile/core/widgets/waypoint_logo.dart';

void main() {
  testWidgets('renders WayPointLogo, auth message, and action buttons', (tester) async {
    bool signInTapped = false;
    bool cancelTapped = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: AuthPromptModal(
            onSignIn: () => signInTapped = true,
            onCancel: () => cancelTapped = true,
          ),
        ),
      ),
    );

    expect(find.byType(WayPointLogo), findsOneWidget);
    expect(find.text('Sign In to Reserve'), findsOneWidget);
    expect(find.text('Sign In / Register'), findsOneWidget);

    await tester.tap(find.text('Sign In / Register'));
    expect(signInTapped, isTrue);

    await tester.tap(find.text('Continue Browsing'));
    expect(cancelTapped, isTrue);
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/booking/widgets/auth_prompt_modal_test.dart`
Expected: FAIL with "Target of URI doesn't exist: auth_prompt_modal.dart".

- [ ] **Step 3: Implement `AuthPromptModal` and update `PassengerNavigationShell`**

In `mobile/lib/features/booking/widgets/auth_prompt_modal.dart`:
- Bottom sheet content featuring `WayPointLogo(size: 40, variant: WayPointLogoVariant.iconOnly)`.
- Title: *"Sign In to Reserve"*.
- Subtitle: *"Please create an account or sign in to hold your seat and receive your digital ticket QR pass."*
- Primary button: "Sign In / Register".
- Secondary button: "Continue Browsing".

In `mobile/lib/features/navigation/screens/passenger_navigation_shell.dart`:
- Change `final UserModel? user;` to nullable.
- In `_buildProfileTab`, if `user == null`, display a polite guest banner with a "Sign In" button.

In `mobile/lib/features/booking/screens/seat_picker_screen.dart`:
- When user taps "Hold Seats" or "Book": if current user is unauthenticated, show modal bottom sheet with `AuthPromptModal`.

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/booking/widgets/auth_prompt_modal_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/booking/widgets/auth_prompt_modal.dart mobile/lib/features/navigation/screens/passenger_navigation_shell.dart mobile/lib/features/booking/screens/seat_picker_screen.dart mobile/test/features/booking/widgets/auth_prompt_modal_test.dart
git commit -m "feat(mobile): add AuthPromptModal and enable guest browsing in passenger shell"
```

---

### Task 7: End-to-End Onboarding Quality Check Verification

**Files:**
- Create: `mobile/test/features/onboarding/onboarding_quality_journey_test.dart`

**Interfaces:**
- Consumes: All components from Tasks 1–6 to verify the complete new-user-to-returning-user journey.

- [ ] **Step 1: Write the end-to-end user journey test**

```dart
// mobile/test/features/onboarding/onboarding_quality_journey_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';
import 'package:waypoint_mobile/core/network/api_client.dart';
import 'package:waypoint_mobile/main.dart';
import 'package:waypoint_mobile/features/onboarding/screens/onboarding_screen.dart';
import 'package:waypoint_mobile/features/journey/widgets/first_run_welcome_card.dart';

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}

void main() {
  late MockFlutterSecureStorage mockStorage;
  late SecureStorageService storageService;
  late ApiClient apiClient;

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    storageService = SecureStorageService(storage: mockStorage);
    apiClient = ApiClient(storage: storageService);
  });

  testWidgets('full onboarding journey: new user sees onboarding, skips, lands on explore with first-run card', (tester) async {
    // 1. Initial state: onboarding not completed
    when(() => mockStorage.read(key: 'waypoint_has_completed_onboarding'))
        .thenAnswer((_) async => null);
    when(() => mockStorage.read(key: 'waypoint_auth_token'))
        .thenAnswer((_) async => null);
    when(() => mockStorage.write(key: 'waypoint_has_completed_onboarding', value: 'true'))
        .thenAnswer((_) async => {});

    await tester.pumpWidget(
      WayPointApp(
        storageService: storageService,
        apiClient: apiClient,
      ),
    );
    await tester.pumpAndSettle();

    // Verify Onboarding is visible
    expect(find.byType(OnboardingScreen), findsOneWidget);

    // 2. User taps Skip
    await tester.tap(find.text('Skip'));
    await tester.pumpAndSettle();

    // Verify storage write was invoked
    verify(() => mockStorage.write(key: 'waypoint_has_completed_onboarding', value: 'true')).called(1);

    // Verify user reaches Explore screen with FirstRunWelcomeCard
    expect(find.byType(FirstRunWelcomeCard), findsOneWidget);

    // 3. Simulate returning user on cold start
    when(() => mockStorage.read(key: 'waypoint_has_completed_onboarding'))
        .thenAnswer((_) async => 'true');

    await tester.pumpWidget(
      WayPointApp(
        storageService: storageService,
        apiClient: apiClient,
      ),
    );
    await tester.pumpAndSettle();

    // Onboarding must NOT be shown to returning users
    expect(find.byType(OnboardingScreen), findsNothing);
  });
}
```

- [ ] **Step 2: Run test to verify it passes against all integrated components**

Run: `flutter test test/features/onboarding/onboarding_quality_journey_test.dart`
Expected: PASS

- [ ] **Step 3: Run entire mobile test suite to confirm zero regressions**

Run: `flutter test`
Expected: All tests PASS.

- [ ] **Step 4: Commit**

```bash
git add mobile/test/features/onboarding/onboarding_quality_journey_test.dart
git commit -m "test(mobile): add full onboarding and returning user quality journey integration test"
```
