# ADR-004: AI Workflow State Persistence Strategy

## Title
ADR-004: Relational PostgreSQL Strategy for AI Workflow Execution State and Audit Persistence

## Status
`Accepted`

---

## Context
SE3090 Assignment 1 mandates that the Level 4 Agentic AI Subsystem persist:
- Workflow execution IDs, travel objectives, and overall workflow status.
- Step-by-step agent execution logs (Planner, Journey Analysis, Resource, Safety Agents).
- Allow-listed tool call parameters, execution durations, and return payloads.
- Deterministic rule validation outputs.
- Transport Manager approval state transitions and final outcomes.
- Complete system audit logs.

We must choose a durable persistence strategy that satisfies the **Integrated System Rule** (using PostgreSQL), guarantees auditability, and avoids storing hidden reasoning or sensitive token data.

---

## Decision
We select a **Relational PostgreSQL Schema with `JSONB` Document Columns** managed via Entity Framework Core migrations.

---

## Architecture & Schema Overview

Four dedicated database tables manage AI workflow persistence:

1. **`AiWorkflows`**: Top-level workflow record (`Id`, `Objective`, `Status`, `StartedAt`, `CompletedAt`).
2. **`AiWorkflowSteps`**: Individual agent step logs (`Id`, `AiWorkflowId`, `AgentName`, `StepOrder`, `ExecutedAt`).
3. **`AiToolCalls`**: Tool execution logs (`Id`, `AiWorkflowStepId`, `ToolName`, `ArgumentsJson` [JSONB], `ResultJson` [JSONB], `DurationMs`).
4. **`AiValidationResults`**: Rule assertion checks (`Id`, `AiWorkflowStepId`, `RuleName`, `Passed`, `ValidationDetails`).

---

## Alternatives Considered

1. **Option A: In-Memory Workflow State (Redis / Memory Cache)**
   - *Pros*: Fast execution speed.
   - *Cons*: Violates assignment evaluation persistence requirements; state lost upon server restart or deployment redeploy; lacks auditable history for Transport Manager review.

2. **Option B: Separate NoSQL Document DB (MongoDB)**
   - *Pros*: Schema-less storage for variable AI tool JSON outputs.
   - *Cons*: Violates the single authoritative PostgreSQL database rule; increases deployment cost and infrastructure complexity.

3. **Option C: Relational PostgreSQL + `JSONB` Columns [ACCEPTED]**
   - *Pros*: 100% compliant with PostgreSQL mandate; uses native EF Core migrations; `JSONB` data type allows flexible storage of structured tool arguments and responses while retaining relational foreign key constraints (`AiWorkflowId` $\rightarrow$ `AiWorkflowStepId`).

---

## Reasons for Decision
- **Assignment Compliance**: Satisfies PostgreSQL storage rules and provides verifiable proof for the Agentic AI Evaluation Report (5–8 pages).
- **Audit Transparency**: Allows React Operator workspace to query and render real-time AI execution timelines.
- **Sensitive Data Rules Compliance**: Storing explicit DTO tool parameters in `JSONB` ensures raw LLM reasoning or prompt chains are NOT saved in PostgreSQL (`REQ-DB-06`).

---

## Consequences

### Positive
- Fully auditable AI execution history queryable via standard SQL.
- Integrates seamlessly with existing `WayPointDbContext` and EF Core migrations.
- Supports historical AI performance analytics (tool latency, error counts).

### Negative
- `JSONB` columns require structured serialization/deserialization logic in C#.

---

## Risks & Mitigation
- **Risk**: Database table growth from verbose tool execution JSON logs.
- **Mitigation**: Truncate or summarize large tool payloads; store only structured DTO parameters (`BR-AITOOL-001`).
