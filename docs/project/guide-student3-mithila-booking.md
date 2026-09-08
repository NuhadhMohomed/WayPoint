# Student 3 Implementation Guide: Booking, Ticketing & Passenger Options

- **Assigned Student**: **Mithila** (Student 3)
- **Component**: **Component 3 — Booking, Ticketing & Passenger Options**
- **Core Domain Focus**: Temporary seat holds, payment sandbox checkout, digital QR e-tickets, passenger cancellations, and refund eligibility processing.
- **Assigned Feature Branch**: `feature/booking-ticketing`

---

## 1. Executive Component Overview

As the owner of **Component 3**, you control transactional revenue and passenger ticketing. Your work ensures that passengers can securely reserve temporary 10-minute seat holds without race conditions, execute simulated checkout charges via the payment sandbox, receive cryptographically signed QR e-tickets on mobile, and process tiered cancellations and refunds.

### Assigned User Stories
- `US-PASS-003` (Temporary Seat Hold)
- `US-PASS-004` (Payment Sandbox Checkout & QR E-Ticket Issuance)
- `US-PASS-005` (Booking Cancellation & Refund Request)

---

## 2. Local Setup & Environment Checklist

1. **Environment Configuration**:
   ```bash
   cp .env.example .env
   ```
   - Ensure `DATABASE_URL` points to your active PostgreSQL instance.
   - Authoritative API base URL: `http://localhost:5010/api/v1` (`ASPNETCORE_URLS=http://localhost:5010`).
   - Web development server connects via `VITE_API_URL=http://localhost:5010/api/v1`.
   - Mobile app connects via `FLUTTER_API_URL=http://localhost:5010/api/v1`.
2. **Restore & Seed Database**:
   ```bash
   dotnet restore backend/WayPoint.sln
   dotnet run --project backend/WayPoint.API -- --seed
   ```
3. **Branch Workflow**:
   ```bash
   git checkout -b feature/booking-ticketing
   ```

---

## 3. Design System & UI Contract (`docs/design/DESIGN.md`)

All UI screens must strictly comply with [`docs/design/DESIGN.md`](docs/design/DESIGN.md):
- **Brand Tokens**: Lanka Blue (`#0056D2`), Sunset Amber (`#FEB300`), Jungle Green (`#005312`), Surface (`#F8F9FA` / `#FFFFFF`).
- **Typography Pairing**: **Plus Jakarta Sans** (headings) and **Inter** (body, payment forms, and ticket passes).
- **Reusable Primitives**:
  - Web: Use `Button`, `Card`, and `TransitBadge` in `web/src/components/ui/`.
  - Mobile: Use `WayPointButton`, `WayPointCard`, and `TransitBadge` in `mobile/lib/core/widgets/`.

---

## 4. Google Stitch UI Screen Specifications

Reference your assigned pre-designed screens in [`docs/design/stitch-screens-index.md`](docs/design/stitch-screens-index.md):

| Screen Code | Screen Title | Stitch Screen ID | Platform | Target File |
| :--- | :--- | :--- | :--- | :--- |
| **MOB-06** | Payment Sandbox Checkout & Hold Bar | `01076854fa0e41d299d8fc02ab224ad1` | Mobile | `mobile/lib/features/booking/screens/payment_checkout_screen.dart` |
| **MOB-07** | Digital QR Ticket Wallet & HMAC Pass | `ce33fd7d93b94ddf8f262655cc1ff1b1` | Mobile | `mobile/lib/features/booking/screens/ticket_wallet_screen.dart` |
| **MOB-08** | Booking History & Tiered Refund Modal | `8a55332576044e36901d87cf3a14e772` | Mobile | `mobile/lib/features/booking/screens/booking_history_screen.dart` |
| **WEB-01** | Operator Overview Dashboard | `b35108ca98ec4eb89ba8b0b9e464fb95` | Web | `web/src/features/bookings/OperatorDashboardPage.jsx` |
| **WEB-11** | Booking Manifest & Payment Sandbox | `2828cbc93fdf4d3db64e00a5fd242cd3` | Web | `web/src/features/bookings/BookingManifestMonitorPage.jsx` |

---

## 5. Domain Entities & Database Schema

Your component directly interacts with the following entities in `WayPoint.Domain.Entities.Booking` (`backend/WayPoint.Domain/Entities/Booking/BookingEntities.cs`):

| Entity | Key Attributes | Notes |
| :--- | :--- | :--- |
| **`SeatHold`** | `Id`, `ServiceId`, `SeatId`, `PassengerId`, `HeldAt`, `HeldUntil`, `Status` | 10-minute temporary seat reservation (`HeldUntil = UtcNow + 10m`) |
| **`Booking`** | `Id`, `BookingReference`, `PassengerId`, `ServiceId`, `SeatNumbers`, `TotalFareAmount`, `Status` | Confirmed passenger booking (`Confirmed`, `Cancelled`) |
| **`Ticket`** | `Id`, `BookingId`, `QrCodePayload`, `Status`, `IsBoarded`, `BoardedAt` | Digital e-ticket and boarding check |
| **`PaymentAttempt`** | `Id`, `BookingId`, `GatewayTransactionId`, `Amount`, `Status`, `ProcessedAt` | Payment sandbox transaction log |
| **`Refund`** | `Id`, `BookingId`, `RefundAmount`, `Percentage`, `Reason`, `ProcessedAt` | Tiered cancellation refund record |

---

## 6. Backend Implementation Blueprint (`backend/`)

### 6.1 Controllers to Implement
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

### 6.2 Complex Business Operation (Beyond CRUD)
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

## 7. Agentic AI Responsibilities (Student 3)

- **Assigned Agent**: **Booking Options Agent** (`ADR-003`).
- **Domain Purpose**: Evaluates bookable journey alternatives, fare difference calculations, seat availability rules, and booking cancellation/refund policy outcomes.
- **Allow-Listed Tools**:
  - `CalculateFareDifference` / `CheckCancellationPolicy`
  - `SendPassengerNotification`
- **Safety Rule**: AI cannot directly confirm payment transactions or initiate refunds without deterministic server-side payment execution (`REQ-TECH-06`).

---

## 8. Testing Requirements

1. **Unit Tests (`WayPoint.Tests/BookingTests.cs`)**:
   - Test tiered cancellation refund percentage calculations across various departure offset timestamps (>24h, 12-24h, <12h).
   - Test HMAC-SHA256 signature generation and tampering detection on QR code payload.
2. **Integration Tests**:
   - Concurrency test: Fire two simultaneous hold requests for the same seat; assert exactly 1 returns `200 OK` and 1 returns `409 Conflict`.
   - Transaction rollback test: Simulate payment failure and verify no booking or ticket records are inserted in PostgreSQL.

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Concurrency & Double Booking Defense**: Show how `IDbContextTransaction` and the seat status check prevent two users from booking the same seat.
- **Temporary Hold Expiration**: Explain how the 10-minute window is validated server-side (`HeldUntil > DateTime.UtcNow`) and not trusted from the client clock.
- **Cryptographic QR Security**: Explain how the HMAC secret signature prevents passengers from forging their own QR boarding tickets.
