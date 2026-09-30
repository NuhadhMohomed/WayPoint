# Booking & Policy Agent — AI Architecture & Evaluation Report Section

**Student 3 / Mithila**
**Component**: Component 3 — Booking, Ticketing & Passenger Options
**Agent**: BookingPolicyAgent (Node 4 of 5)

---

## 1. Agent Architecture

The **Booking & Policy Agent** is the fourth node in WayPoint's five-stage LangGraph multi-agent disruption recovery and journey planning workflow:

```text
Planner → Journey Analysis → Resource Feasibility → [Booking & Policy] → Validation & Safety
  (1)          (2)                   (3)                    (4)                 (5)
```

### Position in Workflow

| Property | Value |
|:---------|:------|
| **Node Position** | 4 of 5 |
| **Agent Name** | `BookingPolicyAgent` |
| **Input Context** | `candidate_routes` (Journey Agent) & `feasibility_result` (Resource Agent) |
| **Output State** | `fare_analysis` (Passed to Validation & Safety Agent) |
| **Framework** | LangGraph StateGraph (`agents/graph.py`, ADR-003) |
| **LLM Model** | Google Gemini 1.5 Flash via `langchain-google-genai` |
| **Safe Failure** | Wrapped with `execute_with_safe_failure` (FR-AI-004) |

### Core Pipeline (`_booking_core`)

1. **Context Aggregation**: Collects candidate routes and replacement bus resources from earlier nodes; sanitizes user objective via `sanitize_user_input()` (FR-AI-008).
2. **LLM Invocation**: Invokes Gemini with system prompt `BOOKING_AGENT_PROMPT` and bound tools `BOOKING_AGENT_TOOLS`.
3. **Iterative Tool Loop**: Executes requested tool calls up to `_MAX_TOOL_ROUNDS = 3`, captures execution times, and feeds results back as `ToolMessage`.
4. **Structured JSON Extraction**: Robustly extracts JSON (`_extract_json`) from direct JSON, markdown code fences, or embedded text.
5. **Deterministic Output Validation**: Validates fare difference arithmetic (`validate_fare_difference_arithmetic`) and tiered refund policies (`validate_cancellation_refund_schedule`).
6. **State & Trace Recording**: Populates `AiWorkflowStep` and `AiToolCall` data structures with duration, arguments, and validation outputs for PostgreSQL persistence (ADR-004).

---

## 2. Tool Integration & Allow-List Enforcement

The Booking & Policy Agent binds **exactly 2 tools** from the mandatory 10 allow-listed tools (BR-AITOOL-001):

### Tool 1: CalculateFareDifference

| Property | Value |
|:---------|:------|
| **Registry Name** | `CalculateFareDifference` |
| **LangChain Name** | `calculate_fare_difference` |
| **Backend Endpoint** | `GET /api/v1/services/{id}` (Fetched for original & replacement services) |
| **Input Schema** | `CalculateFareDifferenceInput(original_service_id, replacement_service_id)` |
| **Output Schema** | `CalculateFareDifferenceOutput(original_fare, replacement_fare, fare_difference, passenger_pays_extra, note)` |
| **Business Rule** | Evaluates fare delta between disrupted service and replacement bus corridor |

### Tool 2: SendPassengerNotification

| Property | Value |
|:---------|:------|
| **Registry Name** | `SendPassengerNotification` |
| **LangChain Name** | `send_passenger_notification` |
| **Backend Endpoint** | `POST /api/v1/notifications` |
| **Input Schema** | `SendPassengerNotificationInput(passenger_ids, title, message)` |
| **Output Schema** | `SendPassengerNotificationOutput(notifications_sent, success, message)` |
| **Policy Constraint** | Notifications are sent ONLY after a rebooking plan is formally approved |

---

## 3. Tool Execution & Audit Recording (ADR-004)

Every tool call dispatched by `BookingPolicyAgent` records complete execution audit telemetry:

```python
record = {
    "tool_name": tool_name,
    "arguments_json": json.dumps(tool_args),
    "result_json": result_str,
    "duration_ms": duration_ms,
}
```

This telemetry is mapped directly to `AiToolCall` database records in PostgreSQL via `workflow_persistence.py`, enabling complete visual auditability in the **AI Multi-Agent Observability Screen (`WEB-10`)**.

---

## 4. Deterministic Guardrails & Output Validation (FR-AI-003)

To prevent LLM hallucination and ensure strict mathematical and financial compliance:

1. **`validate_fare_difference_arithmetic`**:
   Ensures that:
   $$\text{fare\_difference} = \text{round}(\text{replacement\_fare} - \text{original\_fare}, 2)$$
   If the LLM outputs an inconsistent number, the guardrail detects the discrepancy and logs an arithmetic violation.

2. **`validate_cancellation_refund_schedule` (`BR-REFUND-001`)**:
   Enforces the exact Sri Lankan transit refund schedule:
   * $> 24$ hours departure offset: **90% refund** (10% platform fee retained).
   * $12 \text{ to } 24$ hours departure offset: **50% refund**.
   * $< 12$ hours departure offset: **0% refund** (non-refundable).

3. **`PaymentConfirmationBoundary` (`REQ-TECH-06`)**:
   Asserts that the AI agent **never confirms payments, executes credit card transactions, or initiates unauthorized refunds**. Financial execution remains strictly gated within the authoritative ASP.NET Core API layer.

---

## 5. Safe Failure & Resilience (FR-AI-004)

`booking_agent_node` is wrapped with `execute_with_safe_failure()`:
* **Timeout & Retry Limits**: Handled gracefully without blocking subsequent nodes.
* **Fallback Behavior**: If Gemini encounters an outage or rate limit, a baseline `fare_analysis` state is produced, recording `safe_failure` diagnostics while allowing human dispatchers to review the case.
* **No Database Corruption**: Operational booking records in PostgreSQL cannot be corrupted by AI execution failures.

---

## 6. Evaluation Test Suite (`ai/tests/test_booking_agent.py`)

The evaluation test suite covers 8 benchmark categories:

1. **Tool Registry & Allow-List Enforcement**: Asserts that `CalculateFareDifference` and `SendPassengerNotification` resolve callable and unlisted tools are rejected with `ToolNotAllowedError`.
2. **Pydantic Tool Schema Validations**: Verifies type validation on inputs and outputs.
3. **Fare Difference Calculation & Arithmetic**: Validates positive, negative (credit), and zero fare deltas.
4. **Tiered Cancellation Refund Schedule**: Asserts exact percentage assignments across 36h, 18h, and 5h departure offsets.
5. **Robust JSON Extraction**: Tests direct JSON, markdown code fences, embedded JSON, and text fallback.
6. **Tool Call Audit Recording**: Tests duration tracking and audit schema compatibility for `AiToolCall`.
7. **Safe Failure Execution**: Tests graceful degradation during simulated LLM errors.
8. **End-to-End Node Execution**: Tests mocked graph node invocation and step record population.
