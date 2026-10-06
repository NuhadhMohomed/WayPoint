# WayPoint Mobile Application (Flutter 3.x / Dart 3)

The authoritative cross-platform mobile client application for **WayPoint** (**SE3090 Assignment 1**). Designed for passengers (booking, seat selection, ticketing, disruption alerts) and bus conductors (QR ticket scanning, passenger manifest management).

---

## 1. Overview & Technology Stack

The mobile application is engineered following Flutter and Dart enterprise standards:
- **Framework**: Flutter 3.x / Dart 3
- **Architecture**: BLoC / Cubit (`flutter_bloc` v8.1) ([ADR-002](../docs/adr/ADR-002-flutter-state-management.md))
- **Networking**: `dio` (v5.7) with token interceptor and retry policies
- **Secure Token Storage**: `flutter_secure_storage` (hardware keystore / iOS Keychain)
- **Offline Cache**: `shared_preferences` & local caching
- **UI & Iconography**: Google Fonts (`Inter`), `lucide_icons`
- **Testing**: `flutter_test`, `bloc_test`, `mocktail`

---

## 2. Feature Slices & Student Responsibilities

```text
mobile/lib/features/
├── journey/          # Student 1 (Sethum): Journey Search, Comparison & Filter Sheets
├── fleet/            # Student 4 (Dineth) / Student 2 (Nuhadh): Conductor Scanner & Manifest
├── booking/          # Student 2 & 3: Interactive Seat Picker (Nuhadh), Checkout & Wallet (Mithila)
├── disruption/       # Student 4 (Dineth): Real-Time Passenger Disruption Alerts
├── settings/         # Student 2 (Nuhadh): Passenger Settings, Fleet Reviews & Preferences
└── auth/             # Shared / Student 3: Mobile Authentication & Secure Keyring
```

### Detailed Student Screens & Capabilities

| Student Owner | Feature Folder | Screen Implementations | Key Capabilities |
| :--- | :--- | :--- | :--- |
| **Sethum** (Student 1) | `features/journey/` | `JourneySearchScreen`<br>`JourneyComparisonScreen`<br>`PreferenceFilterSheet` | Origin/destination picker, route card listing, multi-leg connecting transit comparisons (`BR-TRANSFER-001`), AC/luxury filters |
| **Nuhadh** (Student 2) | `features/booking/`<br>`features/settings/` | `SeatPickerScreen`<br>`PassengerSettingsScreen` | Interactive visual bus seat grid, seat status color codes (available, held, booked), 10-minute hold initiation, fleet review submission |
| **Mithila** (Student 3) | `features/booking/` | `PaymentCheckoutScreen`<br>`TicketWalletScreen`<br>`BookingHistoryScreen` | 10-minute hold countdown timer (`BR-HOLD-001`), payment checkout, cryptographic HMAC QR ticket rendering, historical bookings |
| **Dineth** (Student 4) | `features/disruption/`<br>`features/fleet/` | `DisruptionAlertScreen`<br>`ConductorScannerScreen`<br>`ConductorManifestScreen` | Push disruption notification display, one-tap reroute acceptance, simulated QR camera scanner, conductor boarding checklist |

---

## 3. Setup & Development

### 3.1 Prerequisites
- Flutter SDK (v3.24+)
- Dart 3 SDK
- Android Studio / Xcode (or VS Code with Flutter extension)

### 3.2 Installation & Startup
```bash
cd mobile

# Fetch pub dependencies
flutter pub get

# Launch on connected emulator or physical device
flutter run
```

### 3.3 Building Android Release APK
To compile the standalone production APK:
```bash
flutter build apk --release --dart-define=API_BASE_URL=https://waypoint-api-production.up.railway.app/api/v1
```

The APK binary will be created at:
```text
mobile/build/app/outputs/flutter-apk/app-release.apk
```

---

## 4. Automated Testing

The Flutter test suite validates widget rendering, BLoC state transitions, and user interactions:

```bash
cd mobile

# Run all automated tests
flutter test

# Run tests with verbose output
flutter test --reporter expanded
```

### Key Automated Test Suites
- `journey_search_test.dart`: Origin/destination query form, route cards rendering.
- `seat_picker_test.dart`: Visual seat grid, single-seat selection, hold status.
- `payment_checkout_test.dart`: Fare breakdown calculations, hold timer countdown.
- `ticket_wallet_test.dart`: QR ticket widget rendering, active/past tabs.
- `disruption_alert_test.dart`: Real-time incident banner, mitigation acceptance.
- `conductor_tools_test.dart`: Conductor ticket scanner simulation, boarding manifest.
- `settings_reviews_test.dart`: Review ratings submission, user preferences.
- `navigation_shells_test.dart`: Bottom navigation bar state transitions.

---

## 5. Security & Secure Storage Architecture

- **Token Protection**: JWT access and refresh tokens are persisted using `flutter_secure_storage`, encrypting credentials in Android Keystore / iOS Keychain.
- **Dio Interceptors**: Automatically injects Bearer tokens and handles 401 token refresh cycles.
- **Offline Mode**: Saved tickets and upcoming journeys remain cached locally for offline inspection.
