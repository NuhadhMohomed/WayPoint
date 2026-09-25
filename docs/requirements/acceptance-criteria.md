# WayPoint Acceptance Criteria Specification

This document defines the formal Acceptance Criteria using **Gherkin (Given-When-Then)** syntax for the **WayPoint** AI-Powered Intercity Journey Planner & Bus Operations Platform.

---

## 1. Authentication & Authorization (`FR-AUTH`)

### Scenario 1.1: Successful User Registration (`FR-AUTH-001`)
```gherkin
Given a user is on the registration screen
When they enter a valid email "passenger@waypoint.lk", full name "Kamal Perera", phone "+94771234567", and password "Pass@1234"
And tap the "Register" button
Then the backend validates the DTO parameters
And hashes the password using BCrypt
And stores the user in PostgreSQL with CreatedAt audit timestamp
And returns a 201 Created HTTP status code with user details
```

### Scenario 1.2: Failed Registration with Duplicate Email (`FR-AUTH-001`)
```gherkin
Given an existing user registered with email "existing@waypoint.lk"
When a new user attempts to register with email "existing@waypoint.lk"
Then the backend detects duplicate email in PostgreSQL
And returns a 409 Conflict HTTP status code
And displays error message "Email address already registered"
```

### Scenario 1.3: Role-Based Authorization Enforcement (`FR-AUTH-003`)
```gherkin
Given a passenger logged in with JWT containing Role "Passenger"
When they attempt to access GET "/api/v1/approvals/pending"
Then ASP.NET Core authorization middleware intercepts the request
And returns a 403 Forbidden HTTP status code
And blocks access to Transport Manager approval data
```

---

## 2. Journey Planning & Route Catalogue (`FR-JOURNEY`)

### Scenario 2.1: Intercity Journey Search Generation (`FR-JOURNEY-003`)
```gherkin
Given active bus services exist for Colombo to Ella on "2026-10-15"
When a passenger submits journey search for Origin "Colombo", Destination "Ella", Date "2026-10-15", Passengers 2
Then ASP.NET Core queries PostgreSQL route and timetable tables
And calculates feasible direct and connecting candidate options
And enforces a minimum 20-minute transfer window for connecting services
And returns a list of candidate journeys comparing departure time, arrival time, duration, fare, and amenities
```

### Scenario 2.2: Preference-Aware Journey Ranking (`FR-JOURNEY-004`)
```gherkin
Given multiple candidate journeys exist for Colombo to Kandy
When a passenger applies preference filter "Arrival Before 12:00 PM" and "AC Bus Required"
Then the ranking engine filters out non-AC services
And scores remaining options prioritizing earlier arrival times
And returns ordered journey cards displaying match scores
```

---

## 3. Fleet, Seat & Resource Feasibility (`FR-FLEET`)

### Scenario 3.1: Real-Time Seat Availability Calculation (`FR-FLEET-003`)
```gherkin
Given a scheduled bus service S-101 with 40 total seats
And 5 seats are currently held by active SeatHold records
And 20 seats are confirmed Booked in PostgreSQL
When a passenger requests seat map for service S-101
Then ASP.NET Core computes seat statuses server-side
And returns seat matrix array with 15 Available, 5 Held, and 20 Booked seats
```

### Scenario 3.2: Replacement Resource Feasibility Evaluation (`FR-FLEET-004`)
```gherkin
Given a disrupted service S-202 with 32 booked passengers
When the Resource Agent checks replacement fleet availability
Then system queries unassigned buses with SeatCapacity >= 32 and status "Active"
And checks available driver schedule rest hours
And returns Feasible = true with Bus ID "NC-4589" and Driver ID "D-88"
```

---

## 4. Booking, Ticketing & Passenger Options (`FR-BOOKING`)

### Scenario 4.1: Temporary Seat Hold Lock (`FR-BOOKING-001`)
```gherkin
Given seat 14B on service S-101 is in "Available" status
When a passenger selects seat 14B and initiates checkout
Then ASP.NET Core opens a database transaction
And verifies seat 14B is Available
And inserts a SeatHold record with HeldUntil set to 10 minutes in the future
And commits transaction and returns 200 OK with hold countdown payload
```

### Scenario 4.2: Concurrent Seat Hold Lock Conflict (`FR-BOOKING-001`)
```gherkin
Given seat 14B is currently Available
When Passenger A and Passenger B simultaneously request hold on seat 14B
Then database transaction isolation allows Passenger A to acquire hold
And Passenger B's transaction detects seat status is no longer Available
And Passenger B receives a 409 Conflict HTTP status code
And Passenger B is prompted to choose another seat
```

### Scenario 4.3: Transactional Payment & E-Ticket Issuance (`FR-BOOKING-002`, `FR-BOOKING-003`)
```gherkin
Given Passenger A has an active valid SeatHold for seat 14B
When Passenger A completes payment sandbox checkout successfully
Then ASP.NET Core opens a PostgreSQL transaction
And transitions SeatHold to Booking status "Confirmed"
And marks seat 14B as "Booked"
And generates digital QR e-ticket payload signed with server key
And commits database transaction
And returns confirmed booking details with scannable QR code
```

---

## 5. Disruption, Rebooking & Approval (`FR-DISRUPTION`)

### Scenario 5.1: High-Impact Disruption Manager Approval Boundary (`FR-DISRUPTION-003`)
```gherkin
Given a ticketed bus service S-303 is marked "Cancelled due to Engine Breakdown"
When the Validation & Safety Agent generates a rebooking proposal cancelling ticketed services
Then the agent classifies the operational change as "High-Impact"
And executes allow-listed tool RequestManagerApproval
And backend updates workflow status to "PendingManagerApproval"
And halts automated execution until Transport Manager review
```

### Scenario 5.2: Transport Manager Approval Execution (`FR-APPROVAL-001`, `FR-DISRUPTION-004`)
```gherkin
Given a rebooking proposal in status "PendingManagerApproval"
When Transport Manager reviews passenger impact evidence and clicks "Approve"
Then ASP.NET Core opens a PostgreSQL transaction
And updates original service to Cancelled
And rebooks affected passengers onto replacement service transactionally
And logs an immutable ApprovalDecision record with Manager ID and timestamp
And dispatches push notifications to affected passengers on Flutter
And commits transaction
```

---

## 6. Agentic AI Workflows (`FR-AI`)

### Scenario 6.1: Allow-Listed Tool Execution Control (`FR-AI-002`)
```gherkin
Given the Resource & Booking Agent is evaluating replacement options
When the agent invokes allow-listed tool CheckSeatAvailability with DTO { ServiceId: "S-303", MinSeats: 35 }
Then backend intercepts tool call and validates parameter DTO
And executes backend business logic against PostgreSQL
And returns structured JSON response to agent
And records tool invocation in AiToolCall table
```

### Scenario 6.2: Agent Safe Failure Execution (`FR-AI-004`)
```gherkin
Given an active AI rebooking workflow execution
When an LLM call times out or fails schema validation after 3 retries
Then backend catches execution error
And transitions workflow status to "SafeFailure"
And logs error trace in AiWorkflowStep table
And displays manual operational controls to operator without corrupting live data
```

---

## 7. Notifications (`FR-NOTIF`)

### Scenario 7.1: Disruption Alert Notification (`FR-NOTIF-001`)
```gherkin
Given a passenger has a confirmed booking on service S-303
When Transport Manager approves service rebooking proposal
Then backend dispatches push notification payload to passenger's device token
And passenger receives alert: "Service Update: Your journey on Colombo-Ella has been rebooked. Tap to view updated ticket."
```

---

## 8. Operator Management (`FR-OPERATOR`)

### Scenario 8.1: Passenger Manifest Inspection (`FR-OPERATOR-001`)
```gherkin
Given service S-101 departing at 08:00 AM
When Dispatcher requests manifest for service S-101
Then system returns list of confirmed passengers, seat numbers, boarding points, and payment statuses
```

---

## 9. Manager Approval (`FR-APPROVAL`)

### Scenario 9.1: Rejection of High-Impact Proposal (`FR-APPROVAL-001`)
```gherkin
Given a pending rebooking proposal in Manager Approval Workbench
When Transport Manager selects "Reject" and enters reason "Alternative fare too high; issue full refund"
Then backend updates proposal status to "Rejected"
And initiates automatic passenger refund processing
And logs manager rejection reason in audit trail
```

---

## 10. Audit Logging (`FR-AUDIT`)

### Scenario 10.1: Audit Log Generation (`FR-AUDIT-001`)
```gherkin
When any high-impact operational action or approval decision is executed
Then backend inserts an immutable row into AuditLogs table
And contains Timestamp, ActorId, ActionType, EntityId, BeforeState, and AfterState JSON
And record is read-only and cannot be updated or deleted via API
```

## 11. Reviews & Ratings (`FR-REVIEW`)

### Scenario 11.1: Submit Valid Review (`FR-REVIEW-001`)
```gherkin
Given a passenger has a "Confirmed" booking
And the trip's arrival time is in the past (completed)
When they submit a review with a 1-5 star rating
Then the review is saved successfully
And linked to the specified bus or driver
```

### Scenario 11.2: Enforce Profanity Filter (`FR-REVIEW-003`)
```gherkin
When a passenger submits a review containing banned words
Then the backend returns a 400 Bad Request
And the review is not saved to the database
```

### Scenario 11.3: Enforce 7-Day Window (`FR-REVIEW-004`)
```gherkin
Given a passenger has a review submitted 8 days ago
When they attempt to update the rating or comment
Then the system returns a 400 Bad Request
And displays "Review window has expired"
```
