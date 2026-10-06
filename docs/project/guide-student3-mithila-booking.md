# Student 3 Implementation Guide: Booking, Ticketing & Passenger Options

- **Assigned Student**: **Mithila** (Student 3)
- **Component**: **Component 3 — Booking, Ticketing & Passenger Options**
- **Core Domain Focus**: Temporary seat holds, payment sandbox checkout, QR e-tickets, cancellations, and tiered refund processing.
- **Assigned Feature Branch**: `feature/booking-ticketing`
- **Architecture**: **Integrated Full-Stack Architecture** ([SPEC-2026-10-05-FRONTEND-RECONSTRUCTION](../superpowers/specs/2026-10-05-frontend-full-stack-reconstruction-design.md))

---

## 1. Executive Component Overview

As the owner of **Component 3**, you build the core revenue and transactional engine of WayPoint:
1. **ASP.NET Core Web API**: 10-minute temporary seat hold concurrency locks (`IDbContextTransaction`), payment sandbox checkout integration, HMAC-SHA256 digital QR ticket signing, and tiered refund calculations (`BR-REFUND-001`).
2. **PostgreSQL Relational DB**: Schemas and EF Core migrations for `SeatHolds`, `Bookings`, `Tickets`, `PaymentAttempts`, and `Refunds`.
3. **React Web Application (`web/src/features/bookings/`)**: Operator departure boards, live occupancy KPIs, and passenger manifest monitor with real-time boarding status (`OperatorDashboardPage`, `BookingManifestMonitorPage`).
4. **Flutter Mobile Application (`mobile/lib/features/booking/`)**: Passenger payment checkout screen, offline-capable digital QR ticket wallet, and booking cancellation history (`PaymentCheckoutScreen`, `TicketWalletScreen`, `BookingHistoryScreen`).
5. **Agentic AI**: Specialized **Booking & Policy Agent** (`ai/agents/booking_agent.py`) with allow-listed tools (`CalculateFareDifference`, `SendPassengerNotification`).

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
   - Authoritative API base URL: `http://localhost:5010/api/v1`.
   - Swagger Documentation: `http://localhost:5010/swagger`.
2. **Run Backend API**:
   ```bash
   dotnet restore backend/WayPoint.sln
   dotnet run --project backend/WayPoint.API -- --seed
   ```
3. **Run React Web Application**:
   ```bash
   cd web && npm install && npm run dev
   ```
4. **Run Flutter Mobile Application**:
   ```bash
   cd mobile && flutter pub get && flutter run
   ```

---

## 3. Client Presentation Tier Implementations

### 3.1 React Web Pages (`web/src/features/bookings/`)
- **`OperatorDashboardPage.jsx`**: Departure summary boards, service occupancy KPIs, revenue analytics, and quick operational action links.
- **`BookingManifestMonitorPage.jsx`**: Real-time passenger manifest table, boarding verification statuses, passenger name search, and CSV export.
- State: TanStack Query v5 hooks (`useBookings`, `useManifest`), Zustand `manifestStore`, Vitest component tests.

### 3.2 Flutter Mobile Screens (`mobile/lib/features/booking/`)
- **`PaymentCheckoutScreen.dart`**: Payment sandbox checkout integration, card input validation, total fare breakdown, and instant booking confirmation.
- **`TicketWalletScreen.dart`**: Digital ticket wallet rendering active e-tickets with HMAC-SHA256 cryptographically signed QR codes and offline access.
- **`BookingHistoryScreen.dart`**: Passenger booking records, tiered cancellation refund calculation preview (`BR-REFUND-001`), and cancellation execution.
- State: `TicketWalletBloc` and `CheckoutBloc` with Dio HTTP client, unit & widget tests.

---

## 4. Authoritative Request & Response DTO Specifications

### 4.1 Seat Hold Reservation Request DTO (`CreateSeatHoldDto`)
```json
{
  "serviceId": "e1a90c12-3456-789a-bcde-f0123456789a",
  "seatNumbers": ["1A", "1B"],
  "passengerId": "USR-8821"
}
```

### 4.2 Hold Reservation Response DTO (`SeatHoldResponseDto`)
```json
{
  "holdId": "hld-99210-ab34",
  "serviceId": "e1a90c12-3456-789a-bcde-f0123456789a",
  "seatNumbers": ["1A", "1B"],
  "heldAt": "2026-10-15T08:00:00Z",
  "heldUntil": "2026-10-15T08:10:00Z",
  "expiresInSeconds": 600,
  "status": "Held"
}
```

### 4.3 QR Boarding Verification Request DTO (`VerifyTicketQrDto`)
```json
{
  "ticketId": "tkt-001234-xyz",
  "qrPayload": "WP|tkt-001234-xyz|SRV-CLKDY-01|1A| Kamalan |HMAC_SIGNATURE_HEX"
}
```

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
Controllers reside under `backend/WayPoint.API/Controllers/`:
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

- **Assigned Agent**: **Booking & Policy Agent** (`ai/agents/booking_agent.py`, `ADR-003`).
- **Domain Purpose**: Evaluates bookable journey alternatives, fare difference calculations, seat availability rules, and booking cancellation/refund policy outcomes.
- **Allow-Listed Tools**:
  - `CalculateFareDifference` / `CheckCancellationPolicy`
  - `SendPassengerNotification`
- **Safety Rule**: AI cannot directly confirm payment transactions or initiate refunds without deterministic server-side payment execution (`REQ-TECH-06`).

---

## 8. Testing Requirements

1. **Backend Unit & Integration Tests (`backend/WayPoint.Tests/BookingTests.cs`)**:
   - Test tiered cancellation refund percentage calculations across various departure offset timestamps (>24h, 12-24h, <12h).
   - Test HMAC-SHA256 signature generation and tampering detection on QR code payload.
   - Concurrency test: Fire two simultaneous hold requests for the same seat; assert exactly 1 returns `200 OK` and 1 returns `409 Conflict`.
   - Transaction rollback test: Simulate payment failure and verify no booking or ticket records are inserted in PostgreSQL.
   ```bash
   dotnet test backend/WayPoint.sln --filter "FullyQualifiedName~Booking|FullyQualifiedName~Payment|FullyQualifiedName~Ticket"
   ```
2. **React Web Tests (`web/src/features/bookings/__tests__/`)**:
   - Vitest component tests for `OperatorDashboardPage` and `BookingManifestMonitorPage`.
   ```bash
   cd web && npm test
   ```
3. **Flutter Mobile Tests (`mobile/test/features/booking/`)**:
   - Widget tests for `PaymentCheckoutScreen` and `TicketWalletScreen`.
   ```bash
   cd mobile && flutter test test/features/booking/
   ```

---

## 9. Viva Examination Defense Cheatsheet

Be prepared to explain and demonstrate live without AI tools:
- **Concurrency & Double Booking Defense**: Show how `IDbContextTransaction` and the seat status check prevent two users from booking the same seat.
- **Temporary Hold Expiration**: Explain how the 10-minute window is validated server-side (`HeldUntil > DateTime.UtcNow`) and not trusted from the client clock.
- **Cryptographic QR Security**: Explain how the HMAC secret signature prevents passengers from forging their own QR boarding tickets.
