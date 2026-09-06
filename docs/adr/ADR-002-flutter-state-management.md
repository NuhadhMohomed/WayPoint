# ADR-002: Flutter State Management Architecture

## Title
ADR-002: Selection of Flutter State Management Approach for Passenger Application

## Status
`DECISION REQUIRED` (Pending final team confirmation between Flutter BLoC/Cubit vs. Riverpod)

---

## Context
The **WayPoint Flutter Mobile Application** provides the passenger user interface. Key interactive mobile workflows include:
- User registration, login, JWT token persistence, and protected screen navigation.
- Journey search with dynamic preference filters (AC, Wi-Fi, arrival deadlines).
- Interactive **Bus Seat Picker** map rendering seat availability with a live 10-minute hold countdown timer.
- Payment sandbox checkout hand-off and digital QR e-ticket display.
- Push/in-app service disruption alerts and rebooking acceptance.

We require a robust, predictable state management pattern that cleanly separates UI widgets from business logic and supports comprehensive widget and unit testing for SE3090 evaluation.

---

## Decision
We propose using **Flutter BLoC (Business Logic Component) / Cubit** for mobile state management.

---

## Alternatives Considered

1. **Option A: Provider / `ChangeNotifier`**
   - *Pros*: Simple, officially recommended for small Flutter apps.
   - *Cons*: Implicit state mutations, difficult to track complex asynchronous events (e.g., hold countdown timers, concurrent payment responses).

2. **Option B: Flutter BLoC / Cubit [RECOMMENDED]**
   - *Pros*: Strict event-driven architecture (`Event -> BLoC -> State`). Explicit immutable states (`SeatHoldInitial`, `SeatHoldInProgress`, `SeatHoldSuccess`, `SeatHoldExpired`). Outstanding testability with `bloc_test` library. Perfect for countdown timers and API streams.
   - *Cons*: Slightly verbose event definitions.

3. **Option C: Riverpod**
   - *Pros*: Modern compile-safe evolution of Provider, no BuildContext requirement.
   - *Cons*: Team unfamiliarity with Riverpod syntax compared to standard BLoC patterns taught in SE3090 labs.

---

## Reasons for Decision
- **Explicit Immutable States**: BLoC forces explicit state classes, making loading, success, error, and timeout states trivial to render in Flutter UI.
- **Countdown Timer Support**: BLoC Cubit seamlessly manages ticker streams for the 10-minute temporary seat hold expiration countdown (`FR-BOOKING-001`).
- **Testability & Viva Compliance**: `bloc_test` makes unit testing state transitions straightforward, providing verifiable test evidence for the 70-mark individual viva evaluation.

---

## Consequences

### Positive
- Strict separation of Flutter UI widgets from ASP.NET Core API calling logic.
- Deterministic state stream testing using `bloc_test`.
- Predictable handling of 10-minute seat hold timeout tickers and QR ticket rendering.

### Negative
- Requires boilerplate event and state class declarations for each feature module.

---

## Risks & Mitigation
- **Risk**: Over-complicating simple screens (e.g., static about/help screens).
- **Mitigation**: Use lightweight **Cubit** for simple state transitions and full **BLoC** only for complex event streams (Seat Hold, Payment, Journey Search).
