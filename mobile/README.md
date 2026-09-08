# WayPoint Mobile (Passenger Flutter Application)

The cross-platform mobile client application for **WayPoint** (**SE3090 Assignment 1**), providing passengers with intercity journey search, preference filtering, interactive seat selection, payment, digital QR ticketing, and real-time disruption alerts.

Built with **Flutter 3.x**, **Dart 3.x**, **flutter_bloc**, **Dio**, and strictly adhering to the **Google Stitch Design System** (`docs/design/DESIGN.md`).

---

## 1. Directory Structure

```text
mobile/
├── lib/
│   ├── core/
│   │   ├── network/             # Dio HTTP client with interceptors & auth tokens
│   │   ├── theme/               # AppTheme, ColorPalette (Lanka Blue, Sunset Amber), Typography
│   │   └── widgets/             # Authoritative Design Primitives (WayPointButton, WayPointCard, TransitBadge)
│   ├── features/
│   │   ├── auth/                # Passenger login, registration & profile
│   │   ├── journey/             # Corridor search, date picker, preference sheet (Student 1)
│   │   ├── booking/             # Interactive bus seat selection & checkout (Student 3)
│   │   ├── tickets/             # Digital ticket wallet with offline QR codes (Student 3)
│   │   └── disruptions/         # Passenger disruption alerts & rebooking action sheet (Student 4)
│   └── main.dart                # Application entry point & service providers
├── pubspec.yaml                 # Dependencies & asset declarations
└── README.md
```

---

## 2. Design System & Tokens (`docs/design/DESIGN.md`)

The mobile UI implements the design system tokens via `AppTheme` in `lib/core/theme/app_theme.dart`:

- **Primary / Lanka Blue**: `#0056D2` (`AppTheme.primaryColor`)
- **Accent / Sunset Amber**: `#FEB300` (`AppTheme.accentColor`)
- **Operational / Jungle Green**: `#005312` (`AppTheme.successColor`)
- **Surface**: `#F8F9FA` / `#FFFFFF`
- **Typography Pairing**:
  - **Plus Jakarta Sans** for screen headers, hero section, and sheet titles.
  - **Inter** for timetable rows, prices in LKR, and body copy.

### Shared UI Primitives (`lib/core/widgets/`)
- `WayPointButton`: Primary, secondary, outline, and icon buttons.
- `WayPointCard`: Surface container with standard rounded corners and elevation.
- `TransitBadge`: Badges for travel classes, bus categories, and status tags.

---

## 3. Pre-Designed Stitch Screens Reference

Reference your assigned screens in [`docs/design/stitch-screens-index.md`](../docs/design/stitch-screens-index.md):

| Screen Code | Screen Name | Assigned Student | Stitch Screen ID |
| :--- | :--- | :--- | :--- |
| **MOB-01** | Passenger Dashboard & Active Journey Card | Student 1 (Sethum) | `5f03d5fae16d4cfa9760775d71c223c2` |
| **MOB-02** | Journey Search, Corridors & Dates | Student 1 (Sethum) | `4baf1853d7a14d7abd597916567b5370` |
| **MOB-03** | Preference Filter Sheet & Sliders | Student 1 (Sethum) | `fb4b74ea904c435b93f05e9dc324e989` |
| **MOB-04** | Interactive Bus Seat Selection | Student 3 (Mithila) | `ba846b0a72ad41ecbf0a116b47c617b0` |
| **MOB-05** | Checkout, Fare Breakdown & Payment Sheet | Student 3 (Mithila) | `ff34d193d56f4d2f8cb573752e259e51` |
| **MOB-06** | Digital Ticket Wallet with Offline QR Code | Student 3 (Mithila) | `9719356d2b4546eeae9eeef94b05531d` |
| **MOB-07** | Disruption Alert Banner & Rebooking Sheet | Student 4 (Dineth) | `26ba19d84c134aa89617d91d09e86337` |

---

## 4. Setup & Running Locally

### Step 1: Install Dependencies
```bash
flutter pub get
```

### Step 2: Configure API Endpoint
The mobile app communicates with the backend via HTTP. When running on:
- **Physical Android Device (via USB/Wi-Fi)**: Set `FLUTTER_API_URL` to your machine's LAN IP, e.g. `http://192.168.1.50:5010/api/v1`.
- **Android Emulator**: Use `http://10.0.2.2:5010/api/v1`.
- **Windows / Desktop**: Use `http://localhost:5010/api/v1`.

### Step 3: Run the App
```bash
flutter run
```

### Step 4: Build Release APK
```bash
flutter build apk --release
```
The deliverable APK will be placed in `build/app/outputs/flutter-apk/app-release.apk`.
