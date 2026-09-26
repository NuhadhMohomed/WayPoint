# Resource Feasibility Agent — AI Evaluation Report Section

**Student 2 / Nuhadh Mohamed**
**Component**: Fleet, Seat & Resource Feasibility
**Agent**: ResourceFeasibilityAgent (Node 3 of 5)

---

## 1. Agent Architecture

The Resource Feasibility Agent is the third node in WayPoint's five-stage LangGraph multi-agent workflow:

```
Planner → Journey Analysis → [Resource Feasibility] → Booking & Policy → Validation & Safety
  (1)          (2)                   (3)                    (4)                (5)
```

### Position in Workflow

| Property | Value |
|:---------|:------|
| **Node Position** | 3 of 5 |
| **Agent Name** | `ResourceFeasibilityAgent` |
| **Input** | `candidate_routes` from Journey Analysis Agent |
| **Output** | `feasibility_result` for Booking & Policy Agent |
| **Framework** | LangGraph (ADR-003) |
| **LLM** | Google Gemini via `langchain-google-genai` |
| **Safe Failure** | Wrapped with `execute_with_safe_failure` (FR-AI-004) |

### Core Pipeline

The agent executes a 6-step pipeline within `_resource_core()`:

1. **Build Context** — Aggregates candidate routes from the Journey Agent and fetches replacement resource data from the backend for disruption workflows.
2. **Invoke LLM** — Sends the system prompt + context to Gemini with two bound tools.
3. **Execute Tool Calls** — Dispatches any LLM-requested tool calls via the allow-list registry, feeds results back to the LLM (up to 3 rounds).
4. **Parse Result** — Extracts structured JSON from the LLM's final response (handles direct JSON, markdown fences, and embedded JSON).
5. **Validate Output** — Runs deterministic business-rule validators on the parsed result (FR-AI-003).
6. **Return State** — Produces the state update with `feasibility_result`, `tool_calls`, and `validation_results` for ADR-004 persistence.

### Source Files

| File | Purpose |
|:-----|:--------|
| `ai/agents/resource_agent.py` | Agent node logic (451 lines) |
| `ai/tools/resource_tools.py` | Tool definitions: `check_seat_availability`, `check_replacement_resources` |
| `ai/prompts/agent_prompts.py` | `RESOURCE_AGENT_PROMPT` system prompt |
| `ai/guardrails/output_validator.py` | Deterministic validators: `validate_bus_capacity`, `validate_seat_counts`, `validate_driver_rest` |
| `ai/schemas/tools.py` | Pydantic schemas: `CheckSeatAvailabilityInput/Output`, `SeatInfo` |

---

## 2. Tool Integration

The Resource Feasibility Agent has access to **exactly 2 tools** from the 10-tool allow-list (BR-AITOOL-001):

### Tool 1: CheckSeatAvailability

| Property | Value |
|:---------|:------|
| **Registry Name** | `CheckSeatAvailability` |
| **LangChain Name** | `check_seat_availability` |
| **Backend Endpoint** | `GET /api/v1/services/{serviceId}/seats` |
| **Input Schema** | `CheckSeatAvailabilityInput(service_id: str)` |
| **Output Schema** | `CheckSeatAvailabilityOutput` (mirrors `ServiceSeatMatrixDto`) |
| **Business Rule** | BR-SEAT-001: Status derived from SeatHolds + Bookings |

The tool returns a complete seat matrix with per-seat status (`Available`, `Held`, `Booked`) and aggregate counts (`total_seats`, `available_seats`, `held_seats`, `booked_seats`).

### Tool 2: CheckTransferFeasibility

| Property | Value |
|:---------|:------|
| **Registry Name** | `CheckTransferFeasibility` |
| **LangChain Name** | `check_transfer_feasibility` |
| **Input Schema** | `CheckTransferFeasibilityInput(leg1_arrival_time, leg2_departure_time)` |
| **Output Schema** | `CheckTransferFeasibilityOutput` |
| **Business Rule** | BR-TRANSFER-001: Minimum 20-minute transfer window |

### Internal Helper: check_replacement_resources()

This is an **internal async helper** (NOT a LangChain tool) called during `disruption_rebooking` workflows:

| Property | Value |
|:---------|:------|
| **Backend Endpoint** | `POST /api/v1/resources/replacement-feasibility` |
| **Request DTO** | `ResourceFeasibilityRequestDto` |
| **Response DTO** | `ResourceFeasibilityResponseDto` |
| **Evaluates** | Unassigned buses, capacity matching, driver rest hours |
| **Error Handling** | Graceful degradation (returns "unavailable" context on failure) |

### Tool Execution Loop

The agent implements a proper tool execution loop (up to 3 rounds):

```python
for round_idx in range(_MAX_TOOL_ROUNDS):
    if not response.tool_calls:
        break  # LLM gave final answer

    for tc in response.tool_calls:
        tool_fn = get_tool(tc["name"])      # Allow-list enforcement
        result = await tool_fn.ainvoke(tc["args"])
        messages.append(ToolMessage(content=result, tool_call_id=tc["id"]))

    response = await llm_with_tools.ainvoke(messages)
```

Each tool execution is recorded with:
- `tool_name`: Registry name of the tool
- `arguments_json`: Serialised input arguments
- `result_json`: Serialised tool output
- `duration_ms`: Execution time in milliseconds

These records map directly to the `AiToolCall` entity in ADR-004.

---

## 3. Business Rule Enforcement

The agent enforces three deterministic business rules via output validators that **override the LLM** when constraints are violated (FR-AI-003):

### BR-RESOURCE-001: Bus Capacity Check

> *Bus capacity must be ≥ required seat count.*

```python
validate_bus_capacity(available_seats=35, required_seats=30)
# → (True, "Sufficient capacity: 35 available >= 30 required")

validate_bus_capacity(available_seats=10, required_seats=30)
# → (False, "Insufficient capacity: 10 available < 30 required (BR-RESOURCE-001)")
```

### BR-RESOURCE-002: Driver Rest Hours

> *Drivers must have ≥ 8 hours rest between assignments.*

```python
validate_driver_rest("2026-10-15T00:00:00", "2026-10-15T10:00:00")
# → (True, 10.0, "Rest period OK: 10.0h (>= 8h required)")

validate_driver_rest("2026-10-15T06:00:00", "2026-10-15T11:00:00")
# → (False, 5.0, "Insufficient rest: 5.0h (minimum 8h required, BR-RESOURCE-002)")
```

### Seat Count Arithmetic

> *available + held + booked must equal total.*

```python
validate_seat_counts(total=50, available=30, held=5, booked=15)
# → (True, "")  — 30+5+15 == 50 ✓

validate_seat_counts(total=50, available=30, held=5, booked=10)
# → (False, "Seat count mismatch: 30+5+10=45, but total=50")
```

### Validator Integration

The `_run_resource_validators()` function runs all applicable validators on the agent's output and returns structured results:

```python
validation_results = _run_resource_validators(feasibility_result)
# Returns: [
#   {"rule_name": "SeatCountArithmetic", "passed": True, "validation_details": "OK"},
#   {"rule_name": "BusCapacityCheck_BR-RESOURCE-001", "passed": True, "validation_details": "..."},
#   {"rule_name": "DriverRestHours_BR-RESOURCE-002", "passed": True, "validation_details": "..."},
# ]
```

These map directly to the `AiValidationResult` entity in ADR-004.

---

## 4. Guardrail Integration

### FR-AI-004: Safe Failure Wrapping

The public entry point `resource_agent_node()` wraps the core logic with `execute_with_safe_failure`:

```python
async def resource_agent_node(state: WorkflowState) -> dict:
    return await execute_with_safe_failure(
        agent_name="ResourceFeasibilityAgent",
        core_fn=_resource_core,
        state=state,
    )
```

Behaviour:
- **Retry cap**: 3 attempts (BR-AIVAL-002)
- **Short-circuit**: If `workflow_status == "SafeFailure"`, the agent returns immediately without invoking the LLM
- **Error recording**: Failure details are preserved in the `error` field

### FR-AI-008: Input Sanitization

User-provided objectives are sanitised before inclusion in the LLM prompt:

```python
objective = sanitize_user_input(state.get("objective", ""))
# Strips: "ignore previous instructions", "system:", "jailbreak", etc.
# Truncates to 2000 characters

wrapped = wrap_user_input(objective)
# Wraps with <user_input>...</user_input> delimiters
```

### Tool Allow-List Enforcement (BR-AITOOL-001)

Tool execution uses `get_tool()` from the registry, which raises `ToolNotAllowedError` for any tool not in the 10-tool allow-list:

```python
tool_fn = get_tool(tool_name)  # Raises ToolNotAllowedError if not allowed
```

---

## 5. Golden Test Case Results

### Scenario: Feasible Replacement

| Parameter | Value |
|:----------|:------|
| Disrupted Service | S-303 (Colombo → Ella) |
| Booked Passengers | 30 |
| Replacement Service | S-404 |
| Total Seats | 45 |
| Available Seats | 35 |
| Held Seats | 2 |
| Booked Seats | 8 |
| Required Seats | 30 |
| Driver Rest | 10 hours |

**Expected Result**: `isFeasible: True`

| Validator | Input | Result | Pass? |
|:----------|:------|:-------|:-----:|
| SeatCountArithmetic | 35+2+8 = 45 | Correct | ✅ |
| BusCapacity (BR-RESOURCE-001) | 35 ≥ 30 | Sufficient | ✅ |
| DriverRest (BR-RESOURCE-002) | 10h ≥ 8h | Sufficient | ✅ |

### Scenario: Infeasible Replacement

| Parameter | Value |
|:----------|:------|
| Replacement Service | S-505 (small bus) |
| Total Seats | 25 |
| Available Seats | 10 |
| Required Seats | 30 |
| Driver Rest | 5 hours |

**Expected Result**: `isFeasible: False`

| Validator | Input | Result | Pass? |
|:----------|:------|:-------|:-----:|
| SeatCountArithmetic | 10+5+10 = 25 | Correct | ✅ |
| BusCapacity (BR-RESOURCE-001) | 10 < 30 | Insufficient | ❌ |
| DriverRest (BR-RESOURCE-002) | 5h < 8h | Insufficient | ❌ |

### Boundary Conditions Tested

| Test | Values | Expected |
|:-----|:-------|:---------|
| Exact capacity match | 30 available = 30 required | PASS |
| Off-by-one below | 29 available < 30 required | FAIL |
| Off-by-one above | 31 available > 30 required | PASS |
| Exact 8h rest | 8.0h = 8.0h minimum | PASS |
| Just under 8h | 7h59m < 8.0h | FAIL |
| Just over 8h | 8h01m > 8.0h | PASS |

---

## 6. Tool Selection Evidence

### Tests Proving Correct Tool Binding

| Test | What It Asserts | Result |
|:-----|:----------------|:------:|
| `test_resource_tools_are_exactly_2` | Exactly 2 tools bound | ✅ |
| `test_resource_tools_contain_check_seat_availability` | CheckSeatAvailability included | ✅ |
| `test_resource_tools_contain_check_transfer_feasibility` | CheckTransferFeasibility included | ✅ |
| `test_resource_tools_exclude_search_routes` | SearchRoutes NOT included | ✅ |
| `test_resource_tools_exclude_create_rebooking_proposal` | CreateRebookingProposal NOT included | ✅ |
| `test_resource_tools_exclude_calculate_fare_difference` | CalculateFareDifference NOT included | ✅ |
| `test_get_tool_returns_check_seat_availability` | Registry resolves correctly | ✅ |
| `test_get_tool_rejects_unlisted_tool` | Unlisted tools raise error | ✅ |

### Tests Proving Schema Validation (BR-AITOOL-002)

| Test | What It Asserts | Result |
|:-----|:----------------|:------:|
| `test_seat_availability_input_accepts_valid_uuid` | Valid input passes | ✅ |
| `test_seat_availability_input_rejects_empty` | Missing required field raises ValidationError | ✅ |
| `test_seat_availability_output_defaults` | Output has correct zero defaults | ✅ |
| `test_seat_availability_output_serializes_to_json` | JSON round-trip works | ✅ |
| `test_seat_info_all_statuses` | Available/Held/Booked all valid | ✅ |

---

## 7. Constraint Assertion Evidence

### BR-RESOURCE-001: Bus Capacity Tests

| Test | Input | Expected | Result |
|:-----|:------|:---------|:------:|
| `test_bus_capacity_sufficient` | 40 avail ≥ 30 req | PASS | ✅ |
| `test_bus_capacity_insufficient` | 10 avail < 30 req | FAIL | ✅ |
| `test_bus_capacity_exact_match` | 30 avail = 30 req | PASS | ✅ |
| `test_bus_capacity_zero_required` | 10 avail ≥ 0 req | PASS | ✅ |

### BR-RESOURCE-002: Driver Rest Hours Tests

| Test | Input | Expected | Result |
|:-----|:------|:---------|:------:|
| `test_driver_rest_sufficient` | 10h ≥ 8h | PASS | ✅ |
| `test_driver_rest_insufficient` | 5h < 8h | FAIL | ✅ |
| `test_driver_rest_exact_8h` | 8h = 8h | PASS | ✅ |
| `test_driver_rest_invalid_datetime` | "not-a-date" | FAIL (graceful) | ✅ |

### Seat Count Arithmetic Tests

| Test | Input | Expected | Result |
|:-----|:------|:---------|:------:|
| `test_seat_counts_valid` | 30+5+15=50 ✓ | PASS | ✅ |
| `test_seat_counts_mismatch` | 30+5+10≠50 | FAIL | ✅ |
| `test_seat_counts_all_available` | 40+0+0=40 ✓ | PASS | ✅ |
| `test_seat_counts_all_booked` | 0+0+40=40 ✓ | PASS | ✅ |

---

## 8. Test Suite Summary

### Test Files

| File | Tests | Focus |
|:-----|------:|:------|
| `tests/test_resource_agent.py` | 63 | Tool selection, schema validation, output validators, JSON extraction, safe failure, sanitization, replacement context |
| `tests/test_golden_resource.py` | 42 | Golden scenario (feasible + infeasible), step record structure, workflow schema, boundary conditions |
| **Total (Resource Agent)** | **105** | **All pass (0 failures)** |

### Requirement Traceability

| Requirement | How It's Addressed | Test Evidence |
|:------------|:-------------------|:-------------|
| FR-AI-001 | Agent participates as Node 3 in LangGraph graph | `test_resource_node_callable` |
| FR-AI-002 | Tools dispatched via `get_tool()` from registry | `test_get_tool_returns_check_seat_availability` |
| FR-AI-003 | Output guardrails override LLM decisions | `test_golden_feasible_all_validators_pass` |
| FR-AI-004 | `execute_with_safe_failure` wrapping | `test_resource_agent_skips_when_safe_failure` |
| FR-AI-005 | Tool calls + validations recorded for persistence | `test_golden_step_tool_call_structure` |
| FR-AI-008 | Input sanitised + tagged delimiters | `test_objective_injection_stripped` |
| BR-RESOURCE-001 | `validate_bus_capacity()` enforced | `test_bus_capacity_*` (4 tests) |
| BR-RESOURCE-002 | `validate_driver_rest()` enforced | `test_driver_rest_*` (4 tests) |
| BR-SEAT-001 | Seat status from SeatHolds + Bookings | `test_seat_info_all_statuses` |
| BR-AITOOL-001 | Exactly 2 tools, allow-list enforced | `test_resource_tools_are_exactly_2` |
| BR-AITOOL-002 | Pydantic schema validation | `test_seat_availability_input_rejects_empty` |
| ADR-003 | LangGraph framework | `test_resource_node_is_async` |
| ADR-004 | Tool calls + validations persisted | `test_golden_tool_call_maps_to_record` |
| REQ-TEST-06 | Golden test case | `TestGoldenFeasibleScenario` (6 tests) |

---

## 9. Running the Tests

```bash
# Resource Agent tests only
python -m pytest tests/test_resource_agent.py -v

# Golden test case only
python -m pytest tests/test_golden_resource.py -v

# Full AI test suite
python -m pytest tests/ -v
```

All 166 tests pass with zero failures (as of 2026-09-26):

```
166 passed, 1 warning in 4.21s
```
