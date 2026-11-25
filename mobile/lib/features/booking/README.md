# Booking & Ticketing Feature Module (`mobile/lib/features/booking/`)

> **Status: Active Development (WIP)**

Implements passenger seat selection, temporary hold timers, sandbox checkout, QR ticket wallet, and booking history:

- **Screens**: `SeatPickerScreen`, `PaymentCheckoutScreen`, `TicketWalletScreen`, `BookingHistoryScreen`
- **Widgets**: `HoldCountdownBar`, `QrTicketPassCard`, `TieredRefundModal`, `AuthPromptModal`
- **Services & Models**: `BookingApiService`, `booking_models.dart`

### Current WIP Notes
- Payment checkout currently integrates with the backend sandbox endpoint (`/api/v1/payments/confirm-sandbox-charge`).
- Direct navigation via the bottom `Seats` tab currently opens with a sample `serviceId` (`srv-colombo-galle-01`) while deep-link/context wiring continues.
