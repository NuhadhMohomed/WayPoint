# ADR-001: React State Management Architecture

## Title
ADR-001: Selection of React State Management Framework for Operator Workspace

## Status
`Accepted` (Confirmed: Zustand + TanStack Query with React/JavaScript and shadcn/ui on Tailwind CSS v3)

---

## Context
The **WayPoint React Web Application** serves as the primary workspace for transport operators, dispatchers, and transport managers. It includes complex interactive features:
- Role-based operator dashboards (occupancy, revenue, upcoming departures).
- CRUD interfaces for routes, stops, services, buses, seat layout templates, and drivers.
- Real-time disruption workbench and AI workflow monitoring.
- **Manager Approval Workbench**, requiring live inspection of passenger metrics, before/after impact evidence, and execution of `Approve`/`Reject`/`Revise` actions.

We require a predictable, testable, and maintainable state management approach that handles both client-side UI state (modals, filters, active tabs) and server-side asynchronous data fetching, caching, and cache invalidation.

---

## Decision
We propose using **Zustand** for lightweight global UI state management combined with **TanStack Query (React Query)** for server state management, data fetching, and automated cache invalidation.

---

## Alternatives Considered

1. **Option A: React Context API + `useReducer`**
   - *Pros*: Zero external dependencies, built into React.
   - *Cons*: Re-render performance issues across large component trees; requires custom boilerplate for caching, loading states, and API error handling.

2. **Option B: Redux Toolkit (RTK) + RTK Query**
   - *Pros*: Standard industry framework, centralized store, powerful DevTools.
   - *Cons*: Significant boilerplate code for 4 students to maintain within a 9-week period; steep learning curve for basic state updates.

3. **Option C: Zustand + TanStack Query (React Query) [RECOMMENDED]**
   - *Pros*: Extreme developer simplicity, tiny bundle size, zero boilerplate. TanStack Query automatically handles API caching, background refetching, and mutation invalidation for CRUD operations. Zustand provides clean hook-based access to global UI state (e.g., active modal state, selected disruption ID).

---

## Reasons for Decision
- **Developer Velocity**: 4-student team needs to implement React components rapidly without drowning in Redux boilerplate.
- **Server State Isolation**: 90% of state in WayPoint is server data (routes, services, bookings, approvals). TanStack Query specializes in server state management.
- **Testability**: Zustand stores and React Query hooks are easily mocked in React Testing Library unit and component tests.
- **Performance**: Zustand avoids unnecessary component re-renders through selective state subscription.

---

## Consequences

### Positive
- Drastically reduced lines of state management code compared to Redux.
- Automatic API cache invalidation when CRUD operations or approval decisions execute.
- Clean separation between server data (TanStack Query) and local UI state (Zustand).

### Negative
- Team members must learn TanStack Query query keys and mutation hooks (`useQuery`, `useMutation`).

---

## Risks & Mitigation
- **Risk**: Stale cache displaying outdated seat map or approval state.
- **Mitigation**: Configure aggressive cache invalidation rules (`staleTime: 5000`) on high-frequency operational endpoints (e.g., pending approvals, seat status).
