# WayPoint Stitch UI Screen Master Index

This document provides a comprehensive, production-ready inventory of all **23 UI screens** designed and generated in Google Stitch for the **WayPoint Intercity Transit Platform** (SE3090 Assignment / Research Project).

- **Stitch Project Name**: `projects/10246576744359980443`
- **Stitch Project Title**: "WayPoint Intercity Transit Platform"
- **Design System Asset**: `assets/0a9e5af03d7d4795a3ce2e1cd7f5d6f9` ("WayPoint Design System")
- **Visual Design Identity**: Lanka Blue (`#0056d2`), Sunset Amber (`#ffb300`), Jungle Green (`#005312`), Plus Jakarta Sans headings, and Inter body typography on `#f8f9fa` clean surfaces.

---

## 1. Mobile Passenger & Conductor App Screens (11 Screens)

| Screen Code | Screen Title | Stitch Screen ID | SE3090 Requirement | Student Owner | Target File | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **MOB-01** | Passenger Auth, Onboarding & Biometrics | `89e2f2f0370244b49c10c1d1e3921aca` | REQ-FE-01, REQ-FE-10 | Sethum / Nuhadh | `PassengerAuthScreen.dart` | Generated |
| **MOB-02** | Journey Search, Corridors & Dates | `4baf1853d7a14d7abd597916567b5370` | REQ-FE-01, REQ-FE-02 | Sethum | `JourneySearchScreen.dart` | Generated |
| **MOB-03** | Preference Filter Sheet & Sliders | `fb4b74ea904c435b93f05e9dc324e989` | REQ-FE-02 | Sethum | `PreferenceFilterSheet.dart` | Generated |
| **MOB-04** | Journey Comparison Cards & Safe Buffer | `aa124497024b48a3adc01888fed1a5a3` | REQ-FE-02, BR-TRANSFER-001 | Sethum | `JourneyComparisonScreen.dart` | Generated |
| **MOB-05** | Interactive Seat Picker & 10m Hold | `f823b1bf16b54795972e08b4d0813d5d` | REQ-FE-03, FR-BOOKING-001 | Nuhadh | `SeatPickerScreen.dart` | Generated |
| **MOB-06** | Payment Sandbox Checkout & Hold Bar | `01076854fa0e41d299d8fc02ab224ad1` | REQ-FE-04, FR-BOOKING-001 | Nuhadh | `PaymentCheckoutScreen.dart` | Generated |
| **MOB-07** | Digital QR Ticket Wallet & HMAC Pass | `ce33fd7d93b94ddf8f262655cc1ff1b1` | REQ-FE-04, REQ-FE-10 | Nuhadh | `TicketWalletScreen.dart` | Generated |
| **MOB-08** | Booking History & Tiered Refund Modal | `8a55332576044e36901d87cf3a14e772` | REQ-FE-04, BR-REFUND-001 | Nuhadh | `BookingHistoryScreen.dart` | Generated |
| **MOB-09** | Disruption Push Alert & Alternative Bus | `2bfd1cb2561245a99f17148299e4099d` | REQ-FE-09, FR-DISRUPTION-003 | Dineth | `DisruptionAlertScreen.dart` | Generated |
| **MOB-10** | Conductor QR Boarding Scanner | `4e6e32a8846441249f406afad5c51eef` | REQ-FE-10 | Nuhadh / Sethum | `ConductorScannerScreen.dart` | Generated |
| **MOB-11** | Conductor Passenger Manifest Roster | `c38c34595fa64b978deeb6f2db6c035d` | REQ-FE-10 | Nuhadh / Sethum | `ConductorManifestScreen.dart` | Generated |

---

## 2. Desktop Web Operator & Management Screens (12 Screens)

| Screen Code | Screen Title | Stitch Screen ID | SE3090 Requirement | Student Owner | Target File | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **WEB-01** | Operator Overview Dashboard | `b35108ca98ec4eb89ba8b0b9e464fb95` | REQ-FE-05, REQ-FE-12 | Mithila | `OperatorDashboardPage.tsx` | Generated |
| **WEB-02** | Route & Intermediate Stop Manager | `387e0fc877bb4f919e190d4c80b8ca56` | REQ-FE-05 | Mithila | `RouteManagerPage.tsx` | Generated |
| **WEB-03** | Tourist Corridor Destination Showcase | `b5395deeca344413844a9396e1ad0ace` | REQ-FE-05, REQ-FE-02 | Mithila | `TouristCorridorsPage.tsx` | Generated |
| **WEB-04** | Timetable & Service Departure Scheduler | `4b4d5f6dbac54679beebab9ca5585e07` | REQ-FE-05 | Mithila | `ServiceSchedulerPage.tsx` | Generated |
| **WEB-05** | Bus Fleet & Visual Seat Layout Matrix | `85fdc0e3b77c40e6877f1a994776abdd` | REQ-FE-06 | Mithila / Dineth | `FleetMatrixBuilderPage.tsx` | Generated |
| **WEB-06** | Driver Roster & Rest Compliance Gantt | `4110147a3a9a47dfb81c2d710e450b70` | REQ-FE-06 | Mithila / Dineth | `DriverRosteringPage.tsx` | Generated |
| **WEB-07** | Disruption Incident Intake & Impact | `91532418794f46b089e31a1f8125be4c` | REQ-FE-07, FR-DISRUPTION-001 | Dineth | `DisruptionIntakePage.tsx` | Generated |
| **WEB-08** | Transport Manager Approval Workbench | `36ac1789e4ef4627b0effb0951e2d402` | REQ-FE-07, BR-APPROVAL-001 | Dineth | `ManagerApprovalWorkbenchPage.tsx` | Generated |
| **WEB-09** | Public Service Alert Broadcast Center | `7b99b77b7518482c82ac9e795ca398ca` | REQ-FE-07, REQ-FE-09 | Dineth | `ServiceAlertBroadcastPage.tsx` | Generated |
| **WEB-10** | AI Multi-Agent Observability & Traces | `30769f8f61624dc694a42feedf4e9866` | REQ-FE-08, Agentic Rules | Dineth / Team | `AiObservabilityPage.tsx` | Generated |
| **WEB-11** | Booking Manifest & Payment Sandbox | `2828cbc93fdf4d3db64e00a5fd242cd3` | REQ-FE-12, REQ-FE-04 | Nuhadh / Mithila | `BookingManifestMonitorPage.tsx` | Generated |
| **WEB-12** | System Admin, RBAC & Immutable Audit | `e835400b1aba4b8d9735d6c168edb061` | REQ-FE-11, REQ-FE-13 | Team / Dineth | `AdminConsolePage.tsx` | Generated |

---

## 3. UI/UX Best Practice & Verification Matrix

All 23 screens incorporate verified transit domain patterns and real Sri Lankan transit data:

1. **LKR (Rs.) Localized Pricing**: Real rates reflecting genuine distances and bus tiers (e.g. Rs. 2,400 for Colombo–Ella Luxury, Rs. 1,200 for Colombo–Galle Expressway).
2. **Transfer Buffer Enforcement (`BR-TRANSFER-001`)**: Displays safe vs risky connection indicators (highlighting the mandatory minimum 20-minute buffer).
3. **10-Minute Seat Reservation Countdown (`FR-BOOKING-001`)**: Ticking hold countdown progress bar across both seat picker and payment checkout.
4. **Tiered Refund Breakdown (`BR-REFUND-001`)**: 90% (>24h), 50% (12-24h), and 0% (<12h) rules rendered with exact rupee refund amounts.
5. **Human Approval Gate for AI (`BR-APPROVAL-001`)**: Authoritative decision workbench where Transport Managers sign off on high-impact vehicle dispatch proposals before operational records change.
6. **Immutable SHA-256 Audit Trail**: Verified cryptographic hash signatures on every operational change, override, and blocked payment bypass.
