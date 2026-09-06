# WayPoint Security Architecture Specification

This document defines the authentication, authorization, secret protection, data privacy, and AI guardrail security architecture for the **WayPoint** platform (**SE3090 Assignment 1**).

---

## 1. Authentication & JWT Architecture

- **Mechanism**: JSON Web Token (JWT) Bearer Authentication.
- **Signing Algorithm**: HMAC-SHA256 (`HS256`).
- **Token Storage**:
  - Flutter Mobile: Encrypted platform storage (`flutter_secure_storage`).
  - React Web: In-memory state / secure HttpOnly session cookie.

### JWT Payload Claim Structure
```json
{
  "sub": "USR-8821",
  "email": "kamal@waypoint.lk",
  "role": "TransportManager",
  "iss": "WayPointAPI",
  "aud": "WayPointClients",
  "iat": 1788390000,
  "exp": "2026-10-16T08:00:00Z"
}
```

---

## 2. Role-Based Access Control (RBAC) Matrix

Access permissions are enforced server-side using ASP.NET Core `[Authorize(Roles = "...")]` attributes:

| Role Claim | Permitted Endpoint Scope | Key Restrictions |
| :--- | :--- | :--- |
| **Passenger** | Search journeys, place seat holds, confirm payment, view own e-tickets, cancel own bookings, view disruption alerts. | CANNOT access operator CRUD, manager approval queues, or admin settings. |
| **Operator / Dispatcher** | Manage routes, stops, timetables, bus fleet, drivers, log disruptions, view manifests, scan QR codes. | CANNOT execute Transport Manager approval decisions for high-impact proposals. |
| **Transport Manager** | Access Manager Approval Workbench, review before/after impact evidence, approve/reject rebooking proposals. | Full operational approval authority over ticketed service cancellations. |
| **Administrator** | Manage user roles, system config parameters, inspect full audit logs, monitor system health. | Administrative governance and audit access. |

---

## 3. Data Protection & Secret Governance

- **Password Hashing**: Passwords are salted and hashed using BCrypt prior to database insertion. Plaintext passwords are NEVER logged or saved (`BR-AUTH-001`).
- **Secret Management**: API keys, JWT secret keys, and database connection strings are stored strictly in environment variables (`.env`). Secrets are NEVER committed to Git (`NFR-SEC-002`).
- **Payment Sandbox Security**: Raw payment card numbers and CVVs are processed strictly inside the external Payment Sandbox gateway. WayPoint stores ONLY payment transaction IDs and authorization tokens (`BR-PAY-001`).
- **Sensitive AI Data Rule**: Hidden AI reasoning text, raw prompt chains, and intermediate model weights are NOT stored in PostgreSQL. Only structured workflow execution state and tool parameters are persisted (`REQ-DB-06`).

---

## 4. Agentic AI Security & Guardrails

To prevent prompt injection, unauthorized actions, or system compromise:

1. **Allow-Listed Tool Sandbox**: AI agents are restricted to calling 10 predefined backend tools. Direct database connection tools or system shell execution tools DO NOT EXIST (`BR-AITOOL-001`).
2. **Input DTO Schema Validation**: All tool parameters submitted by AI models MUST pass C# DTO validation before business logic runs. Invalid parameters cause immediate execution rejection (`BR-AITOOL-002`).
3. **Prompt Injection Resistance**: User input fields (search terms, feedback) are sanitized and wrapped in structured system prompt templates separating user text from system instructions.
4. **Human Approval Boundary**: High-impact operational changes MUST pause in `PendingManagerApproval` state. AI code CANNOT approve its own proposals (`BR-APPROVAL-001`).
5. **Safe Failure Fallback**: AI execution timeouts (10s limit) or repeated validation failures (max 3 retries) trigger graceful fallback to a `SafeFailure` state without corrupting operational database records (`BR-AIVAL-002`).
