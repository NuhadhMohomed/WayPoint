# Post-Onboarding Sign In / Register Prompt Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure every user who finishes or skips the onboarding carousel is immediately prompted to sign in or register, while providing an optional "Continue as Guest" action to preserve non-blocking timetable discovery.

**Architecture:** Update `PassengerAuthScreen` to accept an optional `onContinueAsGuest` callback and auto-dismiss upon successful authentication; update `AuthGate` to present `PassengerAuthScreen` as the immediate destination after onboarding whenever the user is unauthenticated, transitioning to `PassengerNavigationShell(user: null)` only when the user explicitly chooses guest exploration or `PassengerNavigationShell(user: state.user)` once authenticated.

**Tech Stack:** Flutter 3.x, Dart 3, `flutter_bloc` v8.1, `flutter_secure_storage`, `flutter_test`, `mocktail`.

**Spec:** Requirement: "make sure user is asked to sign in or register right after onboarding", aligned with [ADR-002](../../adr/ADR-002-flutter-state-management.md) and [System Architecture](../../architecture/system-architecture.md).

## Global Constraints

- Flutter client must communicate exclusively through the authoritative ASP.NET Core Web API; direct database connections are prohibited (`AGENTS.md`).
- Onboarding completion must remain persisted via `SecureStorageService` (`has_completed_onboarding: true`).
- Visual styles must strictly adhere to `AppTheme` design tokens (Spring Green `#32DE84`, Dark Surface `#0A1B17`, Light `#F4F7F5`).
- The transition must preserve guest exploration when desired (via explicit "Continue as Guest" action), while ensuring unauthenticated users are never dropped silently into the app without being asked to authenticate first.
- Every task must follow strict TDD: failing test first, verification, implementation, passing test, commit.

## Review Focus

1. User completes or skips onboarding: User is immediately shown the `PassengerAuthScreen` (Sign In / Register tabs) rather than silently landing on the Explore shell.
2. User enters valid credentials and signs in from `PassengerAuthScreen`: Emits `Authenticated` and displays `PassengerNavigationShell(user: user)` with full access.
3. User chooses "Continue as Guest" from `PassengerAuthScreen`: Transitions to `PassengerNavigationShell(user: null)` with guest status and first-run welcome card.
4. Returning authenticated user cold starts the app: Bypasses both onboarding and auth screen, immediately rendering `PassengerNavigationShell` (or `ConductorNavigationShell`).
5. Authenticated passenger logs out from settings: `AuthCubit` emits `Unauthenticated`, returning user to `PassengerAuthScreen` asking to sign in or register again.

---

### Task 1: Update `PassengerAuthScreen` to Support Guest Bypass and Auto-Pop on Authentication

**Files:**
- Modify: `mobile/lib/features/auth/screens/passenger_auth_screen.dart`
- Test: `mobile/test/features/auth/passenger_auth_test.dart`

**Interfaces:**
- Produces: `PassengerAuthScreen({Key? key, VoidCallback? onContinueAsGuest})`
- Consumes: `AuthCubit`, `AuthState`

- [ ] **Step 1: Write failing widget tests for `PassengerAuthScreen` with `onContinueAsGuest`**

Add tests in `mobile/test/features/auth/passenger_auth_test.dart` verifying:
1. Renders "Continue as Guest" button when `onContinueAsGuest` callback is provided.
2. Does NOT render "Continue as Guest" button when `onContinueAsGuest` is null.
3. Tapping "Continue as Guest" invokes the provided callback.
4. Auto-pops the route when `Navigator.canPop()` and `state is Authenticated`.

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/auth/passenger_auth_test.dart`
Expected: FAIL with compilation error or missing widget for `onContinueAsGuest`.

- [ ] **Step 3: Implement `onContinueAsGuest` and auto-pop in `PassengerAuthScreen`**

In `mobile/lib/features/auth/screens/passenger_auth_screen.dart`:
- Add `final VoidCallback? onContinueAsGuest;` parameter to `PassengerAuthScreen`.
- In `BlocConsumer<AuthCubit, AuthState>` listener:
  ```dart
  if (state is Authenticated && Navigator.of(context).canPop()) {
    Navigator.of(context).pop();
  }
  ```
- In the `build` method below the `TabBarView` (or bottom action bar), if `widget.onContinueAsGuest != null`, render a styled text button:
  ```dart
  Center(
    child: TextButton.icon(
      key: const Key('continue_as_guest_button'),
      onPressed: widget.onContinueAsGuest,
      icon: const Icon(Icons.arrow_forward_rounded, size: 18),
      label: const Text(
        'Continue as Guest',
        style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
      ),
    ),
  ),
  ```

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/auth/passenger_auth_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/auth/screens/passenger_auth_screen.dart mobile/test/features/auth/passenger_auth_test.dart
git commit -m "feat(mobile): add guest continue action and auto-pop to PassengerAuthScreen"
```

---

### Task 2: Rewire `AuthGate` to Present `PassengerAuthScreen` Directly After Onboarding

**Files:**
- Modify: `mobile/lib/features/navigation/screens/auth_gate.dart`
- Test: `mobile/test/features/navigation/screens/auth_gate_test.dart`

**Interfaces:**
- Consumes: `SecureStorageService`, `AuthCubit`, `PassengerAuthScreen`, `PassengerNavigationShell`, `ConductorNavigationShell`

- [ ] **Step 1: Write failing widget tests in `auth_gate_test.dart`**

Update `mobile/test/features/navigation/screens/auth_gate_test.dart`:
1. `shows PassengerAuthScreen when onboarding is completed and user is unauthenticated`:
   Verify `find.byType(PassengerAuthScreen)` is found (and `find.text('Sign In')` / `find.text('Register')` are present).
2. `transitions to PassengerNavigationShell when user taps Continue as Guest`:
   Tap `find.byKey(const Key('continue_as_guest_button'))`, settle, verify `find.byType(PassengerNavigationShell)` is found.
3. `shows PassengerNavigationShell directly when authenticated`:
   Verify `find.byType(PassengerNavigationShell)` is found for authenticated user without showing auth screen.

- [ ] **Step 2: Run test to verify it fails**

Run: `flutter test test/features/navigation/screens/auth_gate_test.dart`
Expected: FAIL because `AuthGate` currently renders `PassengerNavigationShell` directly when unauthenticated.

- [ ] **Step 3: Implement post-onboarding auth prompt logic in `AuthGate`**

In `mobile/lib/features/navigation/screens/auth_gate.dart`:
- Add `bool _isGuestMode = false;` to `_AuthGateState`.
- When `_onFinishOnboarding()` is called:
  ```dart
  Future<void> _onFinishOnboarding() async {
    await _storage.setOnboardingCompleted(completed: true);
    if (mounted) {
      setState(() {
        _hasCompletedOnboarding = true;
        _isGuestMode = false; // ensure unauthenticated user is prompted
      });
    }
  }
  ```
- In the `BlocBuilder<AuthCubit, AuthState>` builder:
  ```dart
  if (state is Authenticated) {
    _isGuestMode = false;
    final role = state.user.role.toLowerCase();
    if (role == 'conductor' || role == 'transitmanager' || role == 'driver') {
      return ConductorNavigationShell(user: state.user);
    }
    return PassengerNavigationShell(user: state.user);
  }

  // If user explicitly chose to continue as guest during this session
  if (_isGuestMode) {
    return const PassengerNavigationShell(user: null);
  }

  // Unauthenticated user prompted to Sign In or Register
  return PassengerAuthScreen(
    onContinueAsGuest: () {
      setState(() {
        _isGuestMode = true;
      });
    },
  );
  ```

- [ ] **Step 4: Run test to verify it passes**

Run: `flutter test test/features/navigation/screens/auth_gate_test.dart`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add mobile/lib/features/navigation/screens/auth_gate.dart mobile/test/features/navigation/screens/auth_gate_test.dart
git commit -m "feat(mobile): display PassengerAuthScreen immediately after onboarding in AuthGate"
```

---

### Task 3: Update and Verify End-to-End Onboarding Journey Tests

**Files:**
- Modify: `mobile/test/features/onboarding/onboarding_quality_journey_test.dart`

**Interfaces:**
- Consumes: `WayPointApp`, `OnboardingScreen`, `PassengerAuthScreen`, `PassengerNavigationShell`

- [ ] **Step 1: Update the E2E onboarding quality journey test**

In `mobile/test/features/onboarding/onboarding_quality_journey_test.dart`:
- When user taps 'Skip' on `OnboardingScreen`:
  1. Expect `find.byType(PassengerAuthScreen)` to be displayed immediately (confirming user is asked to sign in or register).
  2. Expect both 'Sign In' and 'Register' tabs to be visible.
  3. User taps 'Continue as Guest' (`find.byKey(const Key('continue_as_guest_button'))`).
  4. User then lands on the Explore screen with `find.byType(FirstRunWelcomeCard)`.

- [ ] **Step 2: Run test to verify it passes**

Run: `flutter test test/features/onboarding/onboarding_quality_journey_test.dart`
Expected: PASS

- [ ] **Step 3: Run the entire Flutter test suite to verify zero regressions**

Run: `flutter test`
Expected: All 75+ tests PASS with zero errors.

- [ ] **Step 4: Commit**

```bash
git add mobile/test/features/onboarding/onboarding_quality_journey_test.dart
git commit -m "test(mobile): verify end-to-end user is prompted for auth right after onboarding"
```
