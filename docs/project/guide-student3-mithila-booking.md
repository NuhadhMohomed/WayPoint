# Student 3 Implementation Guide: Booking, Ticketing & Passenger Options

- **Assigned Student**: **Mithila** (Student 3)
- **Component**: **Component 3 — Booking, Ticketing & Passenger Options**
- **Core Domain Focus**: Temporary seat holds, payment sandbox checkout, digital QR e-tickets, passenger cancellations, and refund eligibility processing.

---

## 1. Executive Component Overview

As the owner of **Component 3**, you control transactional revenue and passenger ticketing. Your work ensures that passengers can securely reserve temporary 10-minute seat holds without race conditions, execute simulated checkout charges via the payment sandbox, receive cryptographically signed QR e-tickets on mobile, and process tiered cancellations and refunds.

### Assigned User Stories
- `US-PASS-003` (Temporary Seat Hold)
- `US-PASS-004` (Payment Sandbox Checkout & QR E-Ticket Issuance)
- `US-PASS-005` (Booking Cancellation & Refund Request)

---

## 2. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Booking` (`backend/WayPoint.Domain/Entities/Booking/BookingEntities.cs`):

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`SeatHold`** | `Id`, `ServiceId`, `SeatId`, `PassengerId`, `HeldAt`, `HeldUntil`, `Status` | 10-minute temporary seat reservation (`HeldUntil = UtcNow + 10m`) |
| **`Booking`** | `Id`, `BookingReference`, `PassengerId`, `ServiceId`, `SeatNumbers`, `TotalFareAmount`, `Status` | Confirmed passenger booking (`Confirmed`, `Cancelled`) |
| **`Ticket`** | `Id`, `BookingId`, `QrCodePayload`, `Status`, `IsBoarded`, `BoardedAt` | Digital e-ticket and boarding check |
| **`PaymentAttempt`** | `Id`, `BookingId`, `GatewayTransactionId`, `Amount`, `Status`, `ProcessedAt` | Payment sandbox transaction log |
| **`Refund`** | `Id`, `BookingId`, `RefundAmount`, `Percentage`, `Reason`, `ProcessedAt` | Tiered cancellation refund record |

---

## 3. Backend Implementation Blueprint (`backend/`)

### 3.1 Controllers to Implement
Create these controllers under `backend/WayPoint.API/Controllers/`:
1. `SeatHoldController.cs` (`/api/v1/bookings/hold`):
   - `POST /api/v1/bookings/hold` (Attempt 10-minute temporary seat hold; return 409 Conflict if already held/booked)
   - `DELETE /api/v1/bookings/hold/{id}` (Release active hold before expiration)
2. `PaymentController.cs` (`/api/v1/payments`):
   - `POST /api/v1/payments/sandbox-charge` (Process simulated charge against mock gateway / Stripe proxy)
3. `BookingController.cs` (`/api/v1/bookings`):
   - `GET /api/v1/bookings` (`[Authorize]` - List passenger's own bookings or operator manifest)
   - `GET /api/v1/bookings/{id}` (Retrieve detailed booking summary with tickets)
   - `POST /api/v1/bookings/cancel` (Calculate refund eligibility and cancel booking)
4. `TicketController.cs` (`/api/v1/tickets`):
   - `GET /api/v1/tickets/{id}` (Get ticket with signed QR code payload)
   - `POST /api/v1/tickets/verify-qr` (`[Authorize(Roles = "Admin,Operator")]` - Scan & verify passenger ticket at boarding)

### 3.2 Complex Business Operation (Beyond CRUD)
- **Operation**: *Transactional Seat Hold & Payment Confirmation Operation*.
- **Logic**:
  1. Wrap the entire checkout flow in an `IDbContextTransaction`.
  2. Re-verify that the `SeatHold` is still valid (`HeldUntil > DateTime.UtcNow` and `Status == Held`).
  3. Execute payment sandbox transaction. If declined $\rightarrow$ rollback and throw error.
  4. Create `Booking` record with a unique 8-character reference code (e.g., `WP-7B92K1`).
  5. Generate HMAC-SHA256 cryptographically signed QR ticket payload.
  6. Transition `SeatHold.Status` to `ConvertedToBooking`.
  7. Commit transaction (`await transaction.CommitAsync()`).
  8. **Tiered Refund Engine (`BR-REFUND-001`)**:
     - Offset $> 24$ hours: 90% refund.
     - Offset $12 \text{ to } 24$ hours: 50% refund.
     - Offset $< 12$ hours: 0% refund (non-refundable).

---

## 4. Frontend Web Implementation Blueprint (`web/`)

- **Folder Location**: `web/src/features/bookings/`
- **API Client**: `bookingApi.js` (wraps `/api/v1/bookings`, `/api/v1/tickets`)
- **Key Views to Build**:
  1. `BookingManifestView.jsx`: Passenger manifest table for a specific service departure, filtering by boarding stop and boarding status (`Boarded` vs `Pending`).
  2. `PaymentTransactionMonitorView.jsx`: Log of payment sandbox transactions with status badges (`Success`, `Failed`, `Refunded`).
  3. `TicketVerificationModal.jsx`: Simulator allowing operators to paste/scan a QR payload to verify boarding validity.

---

## 5. Mobile Flutter Implementation Blueprint (`mobile/`)

- **Folder Location**: `mobile/lib/features/booking/`
- **Key Widgets & Screens**:
  1. `PaymentCheckoutScreen.dart`: Order summary, 10-minute hold countdown ticker, test credit card selection (Success card vs Decline card), and Pay button.
  2. `TicketWalletScreen.dart`: Digital wallet tab displaying upcoming journeys and e-ticket cards.
  3. `QrTicketDisplayScreen.dart`: Offline-ready high-contrast QR code widget generated using `qr_flutter`, with booking reference, seat number, and departure time.
- **BLoC State Management**:
  - Events: `HoldSeatsEvent`, `ProcessPaymentEvent`, `CancelBookingEvent`, `HoldTimerTickEvent`.
  - States: `SeatHoldActive` (with countdown seconds), `PaymentProcessing`, `BookingConfirmed`, `BookingFailed`, `HoldExpired`.

---

## 6. Testing Requirements

1. **Unit Tests (`WayPoint.Tests/BookingTests.cs`)**:
   - Test tiered cancellation refund percentage calculations across various departure offset timestamps.
   - Test HMAC-SHA256 signature generation and tampering detection on QR code payload.
2. **Integration Tests**:
   - Concurrency test: Fire two simultaneous hold requests for the same seat; assert exactly 1 returns `200 OK` and 1 returns `409 Conflict`.
   - Transaction rollback test: Simulate payment failure and verify no booking or ticket records are inserted in PostgreSQL.

---

## 7. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Concurrency & Double Booking Defense**: Show how `IDbContextTransaction` and the seat status check prevent two users from booking the same seat.
- **Temporary Hold Expiration**: Explain how the 10-minute window is validated server-side (`HeldUntil > DateTime.UtcNow`) and not trusted from the client clock.
- **Cryptographic QR Security**: Explain the HMAC secret signature prevents passengers from forging their own QR boarding tickets.
