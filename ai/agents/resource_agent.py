"""
WayPoint AI — Resource Feasibility Agent Node (Student 2 / Nuhadh).

Evaluates replacement bus/driver/seat availability for candidate
services identified by the Journey Analysis Agent.

Tools: CheckSeatAvailability, CheckTransferFeasibility.

Phase 1 Enhancements:
  - Tool execution loop: LLM tool_calls are actually dispatched and
    results fed back for final reasoning.
  - Disruption workflow integration: calls check_replacement_resources()
    for disruption_rebooking workflows.
  - Robust JSON parsing: handles markdown-fenced and raw JSON responses.
  - Output guardrail integration: deterministic validators for
    BR-RESOURCE-001 (bus capacity), BR-RESOURCE-002 (driver rest),
    and seat count arithmetic (FR-AI-003).
  - Tool call recording: captures tool name, args, result, and duration
    for AiToolCall persistence (ADR-004, FR-AI-005).
"""

import json
import logging
import re
import time

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.messages import SystemMessage, HumanMessage, ToolMessage

from agents.state import WorkflowState
from config import GEMINI_API_KEY, LLM_MODEL, LLM_TEMPERATURE
from guardrails.safe_failure import execute_with_safe_failure
from guardrails.input_sanitizer import sanitize_user_input, wrap_user_input
from guardrails.output_validator import (
    validate_bus_capacity,
    validate_seat_counts,
    validate_driver_rest,
)
from prompts.agent_prompts import RESOURCE_AGENT_PROMPT
from tools.registry import RESOURCE_AGENT_TOOLS, get_tool
from tools.resource_tools import check_replacement_resources

logger = logging.getLogger("waypoint.ai.resource_agent")

# Maximum number of tool-call round-trips before stopping
_MAX_TOOL_ROUNDS = 3


# ---------------------------------------------------------------------------
# Helper: Robust JSON Extraction (Step 1.3)
# ---------------------------------------------------------------------------
def _extract_json(text: str) -> dict:
    """Extract a JSON object from LLM response text.

    Handles:
    - Direct JSON (starts with ``{``)
    - Markdown code fences (````` ```json {...} ``` `````)
    - Falls back to ``{"raw_response": text}`` on failure
    """
    if not text or not isinstance(text, str):
        return {"raw_response": str(text) if text else ""}

    stripped = text.strip()

    # Try direct parse
    if stripped.startswith("{"):
        try:
            return json.loads(stripped)
        except json.JSONDecodeError:
            pass

    # Try extracting from markdown code fences
    fence_match = re.search(
        r"```(?:json)?\s*(\{.*?\})\s*```", text, re.DOTALL
    )
    if fence_match:
        try:
            return json.loads(fence_match.group(1))
        except json.JSONDecodeError:
            pass

    # Try finding any JSON object in the text
    brace_match = re.search(r"\{[^{}]*\}", text, re.DOTALL)
    if brace_match:
        try:
            return json.loads(brace_match.group(0))
        except json.JSONDecodeError:
            pass

    return {"raw_response": text}


# ---------------------------------------------------------------------------
# Helper: Execute a single LLM-requested tool call (Step 1.1)
# ---------------------------------------------------------------------------
async def _execute_tool_call(
    tool_call: dict,
) -> tuple[str, dict]:
    """Execute a single tool call from the LLM response.

    Uses ``get_tool()`` from the allow-list registry (BR-AITOOL-001).

    Args:
        tool_call: LangChain tool call dict with ``name``, ``args``, ``id``.

    Returns:
        Tuple of (result_string, tool_call_record_dict).
    """
    tool_name = tool_call["name"]
    tool_args = tool_call.get("args", {})
    start_time = time.time()

    try:
        tool_fn = get_tool(tool_name)
        result_str = await tool_fn.ainvoke(tool_args)
    except Exception as exc:
        logger.warning(
            "Tool execution failed: %s — %s: %s",
            tool_name,
            type(exc).__name__,
            exc,
        )
        result_str = json.dumps({
            "error": True,
            "detail": f"{type(exc).__name__}: {exc}",
        })

    duration_ms = int((time.time() - start_time) * 1000)

    record = {
        "tool_name": tool_name,
        "arguments_json": json.dumps(tool_args),
        "result_json": (
            result_str
            if isinstance(result_str, str)
            else json.dumps(result_str)
        ),
        "duration_ms": duration_ms,
    }

    logger.info(
        "Tool executed: %s | duration=%dms", tool_name, duration_ms
    )
    return result_str, record


# ---------------------------------------------------------------------------
# Helper: Fetch replacement resource data for disruption workflows (Step 1.2)
# ---------------------------------------------------------------------------
async def _get_replacement_context(state: WorkflowState) -> str:
    """Call the replacement resource feasibility solver for disruption workflows.

    Only invoked when ``workflow_type == 'disruption_rebooking'``.
    Gracefully degrades on failure (does not crash the agent).

    Returns:
        Context string to inject into the LLM prompt, or empty string.
    """
    if state.get("workflow_type") != "disruption_rebooking":
        return ""

    candidate_routes = state.get("candidate_routes", [])
    disruption_case_id = state.get("disruption_case_id", "")

    if not disruption_case_id:
        return ""

    try:
        # Use first candidate's capacity info as the baseline requirement
        first_candidate = candidate_routes[0] if candidate_routes else {}
        required_capacity = first_candidate.get(
            "total_seats",
            first_candidate.get("totalSeats", 40),
        )
        departure_time = first_candidate.get(
            "departure_time",
            first_candidate.get("departureTime", ""),
        )

        replacement_result = await check_replacement_resources(
            disrupted_service_id=disruption_case_id,
            required_seat_capacity=required_capacity,
            required_departure_time=departure_time,
        )

        logger.info(
            "Replacement resource check completed | result_keys=%s",
            list(replacement_result.keys())
            if isinstance(replacement_result, dict)
            else "non-dict",
        )

        return (
            f"Replacement resource feasibility (backend solver):\n"
            f"{json.dumps(replacement_result, indent=2)}\n\n"
        )

    except Exception as exc:
        logger.warning(
            "Replacement resource check failed: %s: %s",
            type(exc).__name__,
            exc,
        )
        return (
            "Replacement resource check: unavailable "
            f"({type(exc).__name__})\n\n"
        )


# ---------------------------------------------------------------------------
# Helper: Run output guardrails on the feasibility result (Phase 2, Step 2.1)
# ---------------------------------------------------------------------------
def _run_resource_validators(feasibility_result: dict) -> list[dict]:
    """Run deterministic business-rule validators on the agent's output.

    Validators:
    - ``validate_seat_counts``: available + held + booked == total
    - ``validate_bus_capacity``: BR-RESOURCE-001 (capacity >= required)
    - ``validate_driver_rest``: BR-RESOURCE-002 (>= 8h rest)

    Returns:
        List of validation result dicts for AiValidationResult persistence.
    """
    validation_results: list[dict] = []

    # --- Seat count arithmetic check ---
    seat_keys = ("total_seats", "available_seats", "held_seats", "booked_seats")
    # Support both snake_case and camelCase keys from backend/LLM
    seat_data = {}
    for key in seat_keys:
        camel = key.replace("_s", "S").replace("_h", "H").replace("_b", "B")
        seat_data[key] = feasibility_result.get(
            key, feasibility_result.get(camel, None)
        )

    if all(v is not None for v in seat_data.values()):
        passed, detail = validate_seat_counts(
            int(seat_data["total_seats"]),
            int(seat_data["available_seats"]),
            int(seat_data["held_seats"]),
            int(seat_data["booked_seats"]),
        )
        validation_results.append({
            "rule_name": "SeatCountArithmetic",
            "passed": passed,
            "validation_details": detail if not passed else "OK",
        })

    # --- Bus capacity check (BR-RESOURCE-001) ---
    available = feasibility_result.get(
        "available_seats",
        feasibility_result.get("availableSeats", 0),
    )
    required = feasibility_result.get(
        "required_seats",
        feasibility_result.get("requiredSeats", 0),
    )
    if available or required:
        passed, detail = validate_bus_capacity(int(available), int(required))
        validation_results.append({
            "rule_name": "BusCapacityCheck_BR-RESOURCE-001",
            "passed": passed,
            "validation_details": detail,
        })

    # --- Driver rest hours check (BR-RESOURCE-002) ---
    last_end = feasibility_result.get(
        "driver_last_shift_end",
        feasibility_result.get("driverLastShiftEnd", ""),
    )
    next_start = feasibility_result.get(
        "driver_next_shift_start",
        feasibility_result.get("driverNextShiftStart", ""),
    )
    if last_end and next_start:
        passed, hours, detail = validate_driver_rest(last_end, next_start)
        validation_results.append({
            "rule_name": "DriverRestHours_BR-RESOURCE-002",
            "passed": passed,
            "validation_details": detail,
        })

    return validation_results


# ---------------------------------------------------------------------------
# Core Agent Logic
# ---------------------------------------------------------------------------
async def _resource_core(state: WorkflowState) -> dict:
    """Core resource feasibility logic — isolated for safe failure wrapping.

    Pipeline:
    1. Build context from Journey Agent output + replacement resources
    2. Invoke LLM with bound tools
    3. Execute any LLM-requested tool calls (loop up to _MAX_TOOL_ROUNDS)
    4. Parse structured feasibility result from final LLM response
    5. Run deterministic output validators (FR-AI-003)
    6. Return state update with tool call records and validation results
    """
    logger.info(
        "Resource Feasibility Agent started | workflow=%s | type=%s",
        state.get("workflow_id", ""),
        state.get("workflow_type", ""),
    )

    llm = ChatGoogleGenerativeAI(
        model=LLM_MODEL,
        google_api_key=GEMINI_API_KEY,
        temperature=LLM_TEMPERATURE,
    )

    llm_with_tools = llm.bind_tools(RESOURCE_AGENT_TOOLS)

    # -----------------------------------------------------------------------
    # 1. Build context from Journey Analysis Agent output
    # -----------------------------------------------------------------------
    candidate_routes = state.get("candidate_routes", [])
    candidate_ctx = (
        f"Candidate routes from Journey Analysis Agent:\n"
        f"{json.dumps(candidate_routes, indent=2)}\n\n"
        if candidate_routes
        else "No candidate routes provided yet.\n\n"
    )

    # Step 1.2: Fetch replacement resource data for disruption workflows
    replacement_ctx = await _get_replacement_context(state)

    # Sanitize user-provided objective (FR-AI-008)
    objective = sanitize_user_input(state.get("objective", ""))

    messages = [
        SystemMessage(content=RESOURCE_AGENT_PROMPT),
        HumanMessage(
            content=(
                f"Objective: {wrap_user_input(objective)}\n\n"
                f"Workflow type: {state.get('workflow_type', '')}\n\n"
                f"{candidate_ctx}"
                f"{replacement_ctx}"
                f"Please check seat availability for the candidate "
                f"services and evaluate resource feasibility."
            )
        ),
    ]

    # -----------------------------------------------------------------------
    # 2. Invoke LLM and execute tool calls (Step 1.1)
    # -----------------------------------------------------------------------
    tool_call_records: list[dict] = []
    response = await llm_with_tools.ainvoke(messages)

    for round_idx in range(_MAX_TOOL_ROUNDS):
        # Check if the LLM requested tool calls
        if not (hasattr(response, "tool_calls") and response.tool_calls):
            break  # No tool calls — LLM gave a final answer

        logger.info(
            "Tool call round %d: %d call(s) requested",
            round_idx + 1,
            len(response.tool_calls),
        )

        # Add the AI's tool-request message to the conversation
        messages.append(response)

        # Execute each requested tool call
        for tc in response.tool_calls:
            result_str, record = await _execute_tool_call(tc)
            tool_call_records.append(record)

            # Feed the tool result back to the LLM
            messages.append(
                ToolMessage(
                    content=result_str,
                    tool_call_id=tc["id"],
                )
            )

        # Get next LLM response (may request more tools or give final answer)
        response = await llm_with_tools.ainvoke(messages)

    # -----------------------------------------------------------------------
    # 3. Parse feasibility result from final response (Step 1.3)
    # -----------------------------------------------------------------------
    content = (
        response.content
        if isinstance(response.content, str)
        else str(response.content)
    )
    feasibility_result = _extract_json(content)

    # -----------------------------------------------------------------------
    # 4. Run deterministic output validators (Phase 2, Step 2.1)
    # -----------------------------------------------------------------------
    validation_results = _run_resource_validators(feasibility_result)

    if validation_results:
        logger.info(
            "Output validation completed | total=%d | passed=%d | failed=%d",
            len(validation_results),
            sum(1 for v in validation_results if v["passed"]),
            sum(1 for v in validation_results if not v["passed"]),
        )

    # -----------------------------------------------------------------------
    # 5. Build and return state update
    # -----------------------------------------------------------------------
    step_order = state.get("step_order", 0) + 1

    logger.info(
        "Resource Feasibility Agent completed | feasible=%s | "
        "tools_called=%d | validations=%d",
        feasibility_result.get("isFeasible", "unknown"),
        len(tool_call_records),
        len(validation_results),
    )

    return {
        "current_agent": "ResourceFeasibilityAgent",
        "step_order": step_order,
        "messages": [response],
        "feasibility_result": feasibility_result,
        "steps": state.get("steps", [])
        + [
            {
                "agent_name": "ResourceFeasibilityAgent",
                "step_order": step_order,
                "step_description": (
                    f"Evaluated seat and resource feasibility | "
                    f"Tools called: {len(tool_call_records)} | "
                    f"Validations: "
                    f"{sum(1 for v in validation_results if v['passed'])}"
                    f"/{len(validation_results)} passed"
                ),
                "tool_calls": tool_call_records,
                "validation_results": validation_results,
            }
        ],
    }


# ---------------------------------------------------------------------------
# Public Node (Graph Entry Point)
# ---------------------------------------------------------------------------
async def resource_agent_node(state: WorkflowState) -> dict:
    """Resource Feasibility Agent node — wrapped with safe failure (FR-AI-004)."""
    return await execute_with_safe_failure(
        agent_name="ResourceFeasibilityAgent",
        core_fn=_resource_core,
        state=state,
    )
