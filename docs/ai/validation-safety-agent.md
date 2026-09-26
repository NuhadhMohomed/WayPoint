# Validation & Safety Agent — AI Evaluation Report Section

**Student 4 / Dineth Sasmitha**  
**Component**: Disruption, Rebooking & Transport Manager Approval  
**Agent**: ValidationSafetyAgent (Node 5 of 5)  

---

## 1. Agent Architecture

The **Validation & Safety Agent** is the final authoritative governor (Node 5 of 5) in WayPoint's LangGraph multi-agent workflow:

```
Planner → Journey Analysis → Resource Feasibility → Booking & Policy → [Validation & Safety]
  (1)          (2)                   (3)                    (4)                 (5)
```

### Position in Workflow

| Property | Value |
|:---|:---|
| **Node Position** | 5 of 5 (Authoritative Terminal Governor) |
| **Agent Name** | `ValidationSafetyAgent` |
| **Input** | `candidate_routes`, `feasibility_result`, `fare_analysis`, and `disruption_case_id` |
| **Output** | `impact_assessment`, `impact_classification`, `requires_approval`, `workflow_status` |
| **Framework** | LangGraph (ADR-003) |
| **LLM** | Google Gemini 1.5 Flash via `langchain-google-genai` |
| **Safe Failure** | Wrapped with `execute_with_safe_failure` (FR-AI-004) |

### Core Pipeline

The agent executes a 6-step safety and validation pipeline within `_safety_core()`:

1. **Build Cross-Agent Context** — Ingests candidate routes from Journey Agent, resource feasibility checks from Resource Agent, and fare guarantees from Booking Agent.
2. **Invoke LLM with Bound Tools** — Dispatches the safety prompt to Gemini with the 4 allow-listed disruption tools.
3. **Execute Tool Iteration Loop** — Dispatches any LLM-requested tool calls via `get_tool()` from the allow-list registry (up to 3 rounds) and records execution duration and I/O.
4. **Parse Output with Fallbacks** — Extracts structured JSON using robust extraction (handles raw JSON, fenced markdown code blocks, and embedded payloads).
5. **Deterministic Rule Validation (FR-AI-003)** — Runs pure-function business rule validators (`validate_impact_classification`) that override LLM hallucinations:
   - Service Cancellation $\rightarrow$ Always **High Impact** (`BR-APPROVAL-001`).
   - Timetable shift $> 15$ minutes $\rightarrow$ Always **High Impact** (`BR-DISRUPT-001`).
   - Timetable shift $\le 15$ minutes $\rightarrow$ **Low Impact** (Can auto-execute).
6. **Enforce Human Approval Boundary & Return State** — If High Impact, halts workflow execution at `PendingManagerApproval` state. Creates step records with `tool_calls` and `validation_results` for relational audit persistence (`ADR-004`).

### Source Files

| File | Purpose |
|:---|:---|
| `ai/agents/safety_agent.py` | Safety agent node logic and tool execution loop |
| `ai/tools/disruption_tools.py` | Tool definitions: `create_rebooking_proposal`, `calculate_passenger_impact`, `request_manager_approval`, `apply_approved_operational_change` |
| `ai/prompts/agent_prompts.py` | `SAFETY_AGENT_PROMPT` system prompt |
| `ai/guardrails/output_validator.py` | Deterministic validators: `validate_impact_classification` |
| `ai/schemas/tools.py` | Pydantic schemas: `CreateRebookingProposalInput/Output`, `CalculatePassengerImpactInput/Output`, `RequestManagerApprovalInput/Output`, `ApplyApprovedChangeInput/Output` |

---

## 2. Tool Integration

The Validation & Safety Agent has access to **exactly 4 tools** from the 10-tool allow-list (`BR-AITOOL-001`):

### Tool 1: CalculatePassengerImpact
- **Registry Name**: `CalculatePassengerImpact`
- **LangChain Tool**: `calculate_passenger_impact`
- **Backend Endpoint**: `GET /api/v1/disruptions/{id}`
- **Purpose**: Computes affected passenger count, total delay, and revenue at risk from confirmed bookings.

### Tool 2: CreateRebookingProposal
- **Registry Name**: `CreateRebookingProposal`
- **LangChain Tool**: `create_rebooking_proposal`
- **Backend Endpoint**: `POST /api/v1/rebooking/generate-proposal`
- **Purpose**: Generates a rebooking proposal pairing the disrupted service with a replacement coach.

### Tool 3: RequestManagerApproval
- **Registry Name**: `RequestManagerApproval`
- **LangChain Tool**: `request_manager_approval`
- **Backend Endpoint**: `PUT /api/v1/approvals/{id}/request`
- **Purpose**: Halts automated processing and places the proposal into the Transport Manager's pending review queue.

### Tool 4: ApplyApprovedOperationalChange
- **Registry Name**: `ApplyApprovedOperationalChange`
- **LangChain Tool**: `apply_approved_operational_change`
- **Backend Endpoint**: `POST /api/v1/rebooking/{id}/execute`
- **Boundary Restriction**: Can **ONLY** be executed after a human Transport Manager has submitted an `Approved` decision via the backend API. Direct execution without manager sign-off is blocked (`BR-APPLY-001`).

---

## 3. Deterministic Safety Boundaries

### BR-APPROVAL-001: Transport Manager Approval Gate
AI agents are strictly forbidden from directly altering operational vehicle assignments, cancelling passenger tickets, or committing high-impact changes without human authorization.

```
Is Disruption High Impact?
  ├── Departure Shift > 15 min? ─────────────► YES ──┐
  ├── Service Cancellation? ─────────────────► YES ──┼──► HALT at PendingManagerApproval
  └── Critical Severity Collision/Failure? ──► YES ──┘     (Human Manager Must Review)
  └── Shift ≤ 15 min (Minor Delay) ──────────► NO ─────► Can auto-execute low-impact remedy
```

### FR-AI-003: Output Guardrail Override
If an LLM hallucinates and classifies a 45-minute timetable shift or a cancellation as "Low" impact, `validate_impact_classification()` intercepts and forcibly corrects the classification to **"High"**, preventing unauthorized automated execution.

---

## 4. Golden Test Case: Colombo–Ella Breakdown

### Scenario Inputs
- **Corridor**: Colombo (Bastion Hill) $\rightarrow$ Ella Town (Service `EX-08`)
- **Incident**: Bus ND-8821 Mechanical Breakdown near Kadawatha
- **Affected Passengers**: 28 confirmed ticket holders
- **Proposed Remedy**: Bus WP-CAD-4120 (Super Line Luxury Coach) departing at 07:15 AM
- **Timetable Shift**: 45 minutes ($> 15$ minutes)

### Expected & Verified Behavior
1. **Impact Classification**: `High` (Delay 45 min $> 15$ min threshold).
2. **Approval Gate**: `requires_approval = True`.
3. **Workflow Status**: `PendingManagerApproval`.
4. **Execution Halted**: Direct call to `ApplyApprovedOperationalChange` is blocked.
5. **Persistence Records**: Emits `AiWorkflowStep`, `AiToolCall`, and `AiValidationResult` records with before/after state captures (`ADR-004`).

---

## 5. Test Suite & Requirement Traceability

| Requirement | Description | Test Evidence |
|:---|:---|:---|
| **BR-AITOOL-001** | Exactly 4 allow-listed tools bound | `test_safety_tools_are_exactly_4` |
| **BR-AITOOL-002** | Strict Pydantic input/output schema validation | `test_create_rebooking_proposal_input_valid` |
| **BR-APPROVAL-001** | High-impact changes gate human manager approval | `test_golden_high_impact_overrides_and_gates_approval` |
| **BR-DISRUPT-001** | Timetable shift $> 15$ min triggers High impact | `test_timetable_shift_boundary_enforcement` |
| **BR-APPLY-001** | Automated execution blocked without approval | `BR_APPLY_001_ExecuteUnapprovedProposal_ThrowsInvalidOperationException` |
| **FR-AI-002** | Tool execution loop control | `test_safety_tools_contain_create_rebooking_proposal` |
| **FR-AI-003** | Deterministic output guardrails override LLM | `test_cancellation_always_overrides_to_high_impact` |
| **FR-AI-004** | `execute_with_safe_failure` error wrapping | `test_safety_agent_skips_when_prior_safe_failure` |
| **FR-AI-008** | Input sanitization against prompt injection | `test_prompt_injection_is_stripped_or_defused` |
| **ADR-004** | Step & tool records match persistence DTOs | `test_golden_step_record_structure_matches_adr004` |
| **REQ-TEST-06** | Golden scenario verification | `TestGoldenHighImpactScenario` |

---

## 6. Viva Defense Cheatsheet

1. **How is the Human Approval Boundary (`BR-APPROVAL-001`) enforced in code?**
   - The safety agent executes `validate_impact_classification()`. If `shiftMinutes > 15` or `is_cancellation == True`, the result is deterministically set to `High`.
   - The state transitions to `workflow_status = "PendingManagerApproval"` and stops. Automated dispatch tools are halted.
   - On the backend, `RebookingService.ExecuteApprovedRebookingAsync()` verifies `proposal.Status == RebookingStatus.Approved`, returning an error if an unapproved proposal is executed.

2. **Why does the AI not directly execute rebooking or refund operations?**
   - Per Agentic AI safety rules, AI agents only generate structured proposals (`RebookingProposal`).
   - The execution requires cryptographic and transactional guarantees handled exclusively by ASP.NET Core `IDbContextTransaction`.

3. **How does WayPoint protect against prompt injection trying to bypass approval?**
   - User inputs are passed through `sanitize_user_input()`, stripping injection attempts (e.g., `Ignore previous instructions`).
   - Inputs are wrapped with `<user_input>` delimiters.
   - Even if the LLM were tricked into saying "Low" impact, deterministic python validators override the classification before updating state.
