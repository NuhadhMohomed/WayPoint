# Fleet & Conductor Operations Feature Module (`mobile/lib/features/fleet/`)

> **Status: Active Development (WIP)**

Houses conductor boarding tools, service reviews, and shared fleet/booking BLoCs:

- **Screens**: `ConductorScannerScreen` (camera QR + manual ticket code entry), `ConductorManifestScreen`, `ReviewSubmissionScreen`
- **BLoCs**: `ConductorScannerBloc`, `ConductorManifestBloc`, `SeatPickerBloc`, `PaymentBloc`, `TicketWalletBloc`, `BookingHistoryBloc`, `ReviewBloc`
- **Data**: `FleetApiService`

### Current WIP Notes
- Consolidating duplicate `ReviewSubmissionScreen` files under `presentation/pages/` and `presentation/screens/`.
- Aligning conductor role checks in `AuthGate` with backend `UserRoleType` definitions.
