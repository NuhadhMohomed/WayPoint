# WayPoint Mobile Application (Flutter / Dart 3)

> **Status: Active Development (`v0.1.0+1` / WIP)**  
> The WayPoint cross-platform Flutter client is under active development. Core passenger and conductor screens, BLoC state management, secure token storage, and widget tests are implemented, while live service bindings, role alignment, and offline sync enhancements remain ongoing.

---

## 1. Technology Stack (`pubspec.yaml`)

- **Framework**: Flutter (Dart SDK `>=3.0.0 <4.0.0`)
- **State Management**: `flutter_bloc` (`^8.1.3`) & `equatable` ([ADR-002](../docs/adr/ADR-002-flutter-state-management.md))
- **Networking**: `dio` (`^5.4.0`) + `http` (`^1.6.0`)
- **Storage**: `flutter_secure_storage` (`^9.0.0`) with in-memory fallback in `LocalCacheService`
- **QR & Media**: `qr_flutter` (`^4.1.0`), `mobile_scanner` (`^5.1.1`), `cached_network_image` (`^3.3.0`), `intl` (`^0.19.0`)
- **Testing**: `flutter_test`, `bloc_test`, `mocktail`

---

## 2. Feature Structure (`lib/features/`)

```text
mobile/lib/
├── core/                   # ApiConstants, ApiClient, LocalCacheService, SecureStorageService, AppTheme & shared widgets
└── features/
    ├── auth/               # AuthCubit, AuthState, PassengerAuthScreen
    ├── onboarding/         # OnboardingScreen
    ├── navigation/         # BrandedSplashScreen, AuthGate, PassengerNavigationShell, ConductorNavigationShell
    ├── journey/            # JourneySearchScreen, JourneyComparisonScreen, PreferenceFilterSheet & AI journey widgets
    ├── booking/            # SeatPickerScreen, PaymentCheckoutScreen, TicketWalletScreen, BookingHistoryScreen & widgets
    ├── fleet/              # ConductorScannerScreen, ConductorManifestScreen, ReviewSubmissionScreen & shared BLoCs
    ├── disruption/         # DisruptionAlertScreen, DisruptionAlertCard, DisruptionApiService
    └── settings/           # PassengerSettingsScreen
```

### Feature Modules
- [`journey/`](lib/features/journey/README.md) — Journey search, comparison, filters, and AI journey recommendation widgets.
- [`booking/`](lib/features/booking/README.md) — Seat selection, 10-minute hold countdown, sandbox payment checkout, QR ticket wallet, and booking history.
- [`fleet/`](lib/features/fleet/README.md) — Conductor QR ticket scanner, passenger manifest, service reviews, and shared fleet/booking BLoCs.
- [`disruption/`](lib/features/disruption/README.md) — Disruption alerts, rebooking consent, and replacement bus options.

---

## 3. Setup & Local Development

### Prerequisites
- Flutter SDK (`3.x`) & Dart 3
- Android Studio / Xcode / VS Code with Flutter extension

### Run Locally
```bash
cd mobile
flutter pub get
flutter run
```

API base URL defaults in `lib/core/constants/api_constants.dart` to `http://10.0.2.2:5010/api/v1` (Android emulator localhost). Override at build/run time:
```bash
flutter run --dart-define=API_BASE_URL=http://localhost:5010/api/v1
```

### Build Android APK (Testing / Preview)
```bash
flutter build apk --release --dart-define=API_BASE_URL=http://localhost:5010/api/v1
```

---

## 4. Automated Testing & Static Analysis

```bash
cd mobile
flutter analyze
flutter test
```

Tests under `mobile/test/` cover core widgets/storage, auth gate, onboarding, journey search & AI widgets, seat picker, payment checkout, ticket wallet, disruption alerts, conductor tools, reviews, and navigation shells.

---

## 5. In Progress / Known WIP Notes

- [ ] **Direct Tab Seat Picker Binding**: In `PassengerNavigationShell`, opening the `Seats` tab directly currently passes a sample `serviceId` (`srv-colombo-galle-01`); navigating from journey comparison passes the selected service.
- [ ] **Role Routing Alignment**: `AuthGate` checks for `conductor`, `transitmanager`, or `driver` roles to launch `ConductorNavigationShell`, which is still being aligned with the backend `UserRoleType` enum (`Passenger`, `Operator`, `TransportManager`, `Admin`).
- [ ] **Duplicate Review Screen Cleanup**: Consolidate `features/fleet/presentation/pages/review_submission_screen.dart` and `features/fleet/presentation/screens/review_submission_screen.dart`.
- [ ] **Sandbox Payment & Offline Sync**: Checkout calls `/api/v1/payments/confirm-sandbox-charge`; real payment gateway and full offline ticket sync are pending.
