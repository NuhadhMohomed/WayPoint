"""
WayPoint AI — Unit Tests & Evaluation Benchmarks
Student 3 (Mithila): Booking & Policy Agent

Coverage:
  1. Tool Registry & Allow-List Enforcement (BR-AITOOL-001)
  2. Pydantic Tool Schema Validations (BR-AITOOL-002)
  3. Fare Difference Calculation Logic & Arithmetic Validation
  4. Tiered Cancellation Refund Schedule Validation (BR-REFUND-001)
  5. Robust JSON Extraction & Markdown Parsing
  6. Tool Call Recording for AiToolCall DB Persistence (ADR-004, FR-AI-005)
  7. Safe Failure Guardrail & Resilience (FR-AI-004)
  8. End-to-End Agent Node Execution with State Transitions

Run:
  cd ai
  python -m pytest tests/test_booking_agent.py -v
"""

from __future__ import annotations

import asyncio
import json
import pytest
from unittest.mock import AsyncMock, MagicMock, patch

from schemas.tools import (
    CalculateFareDifferenceInput,
    CalculateFareDifferenceOutput,
    SendPassengerNotificationInput,
    SendPassengerNotificationOutput,
)
from tools.registry import (
    ALLOWED_TOOLS,
    BOOKING_AGENT_TOOLS,
    get_tool,
    is_allowed,
    ToolNotAllowedError,
)
from agents.booking_agent import (
    _extract_json,
    _execute_tool_call,
    booking_agent_node,
)
from guardrails.output_validator import (
    validate_fare_difference_arithmetic,
    validate_cancellation_refund_schedule,
    run_all_validators,
)
from guardrails.input_sanitizer import sanitize_user_input, wrap_user_input


# ============================================================================
# 1. Tool Registry & Allow-List Enforcement (BR-AITOOL-001)
# ============================================================================

class TestBookingAgentToolRegistry:
    """Verifies that Booking Agent tools comply with BR-AITOOL-001 allow-list."""

    def test_booking_agent_tools_in_registry(self):
        """All assigned Booking tools must be registered and resolve callable."""
        for tool_name in ["CalculateFareDifference", "SendPassengerNotification"]:
            assert is_allowed(tool_name)
            tool_fn = get_tool(tool_name)
            assert tool_fn is not None
            assert hasattr(tool_fn, "invoke") or hasattr(tool_fn, "ainvoke") or callable(tool_fn)

    def test_booking_agent_tool_subset_count(self):
        """Booking Agent may ONLY bind its 2 assigned tools."""
        assert len(BOOKING_AGENT_TOOLS) == 2
        tool_names = [t.name for t in BOOKING_AGENT_TOOLS]
        assert "calculate_fare_difference" in tool_names
        assert "send_passenger_notification" in tool_names

    def test_unregistered_tools_rejected(self):
        """Attempting to access non-allow-listed tool must raise ToolNotAllowedError."""
        with pytest.raises(ToolNotAllowedError):
            get_tool("ConfirmPaymentDirectly")

        with pytest.raises(ToolNotAllowedError):
            get_tool("DirectDatabaseAccess")

        with pytest.raises(ToolNotAllowedError):
            get_tool("BypassManagerApproval")


# ============================================================================
# 2. Pydantic Tool Schema Validation (BR-AITOOL-002)
# ============================================================================

class TestBookingAgentSchemaValidation:
    """Verifies Pydantic schema contracts for Booking Agent tools."""

    def test_calculate_fare_difference_input_valid(self):
        dto = CalculateFareDifferenceInput(
            original_service_id="00000000-0000-0000-0000-000000000001",
            replacement_service_id="00000000-0000-0000-0000-000000000002",
        )
        assert dto.original_service_id == "00000000-0000-0000-0000-000000000001"
        assert dto.replacement_service_id == "00000000-0000-0000-0000-000000000002"

    def test_calculate_fare_difference_output_valid(self):
        out = CalculateFareDifferenceOutput(
            original_fare=2850.0,
            replacement_fare=3200.0,
            fare_difference=350.0,
            passenger_pays_extra=True,
            note="Passenger pays LKR 350.00 extra",
        )
        assert out.fare_difference == 350.0
        assert out.passenger_pays_extra is True

    def test_send_passenger_notification_input_valid(self):
        dto = SendPassengerNotificationInput(
            passenger_ids=["p-1", "p-2"],
            title="Service Update",
            message="Your bus departure time has changed.",
        )
        assert len(dto.passenger_ids) == 2
        assert dto.title == "Service Update"

    def test_send_passenger_notification_output_valid(self):
        out = SendPassengerNotificationOutput(
            notifications_sent=2,
            success=True,
            message="Sent notifications to 2 passenger(s)",
        )
        assert out.notifications_sent == 2
        assert out.success is True


# ============================================================================
# 3. Deterministic Fare Arithmetic Validator
# ============================================================================

class TestFareArithmeticValidation:
    """Tests for deterministic validation of fare difference calculation."""

    def test_fare_difference_passenger_pays_extra(self):
        passed, err = validate_fare_difference_arithmetic(
            original_fare=1450.0,
            replacement_fare=2850.0,
            reported_fare_diff=1400.0,
        )
        assert passed is True
        assert err == ""

    def test_fare_difference_passenger_saves(self):
        passed, err = validate_fare_difference_arithmetic(
            original_fare=2850.0,
            replacement_fare=1450.0,
            reported_fare_diff=-1400.0,
        )
        assert passed is True
        assert err == ""

    def test_fare_difference_equal_fares(self):
        passed, err = validate_fare_difference_arithmetic(
            original_fare=2000.0,
            replacement_fare=2000.0,
            reported_fare_diff=0.0,
        )
        assert passed is True
        assert err == ""

    def test_fare_difference_arithmetic_mismatch_detected(self):
        passed, err = validate_fare_difference_arithmetic(
            original_fare=1000.0,
            replacement_fare=2500.0,
            reported_fare_diff=500.0,  # Incorrect difference!
        )
        assert passed is False
        assert "Fare difference mismatch" in err
        assert "1500.00" in err


# ============================================================================
# 4. Tiered Cancellation Refund Schedule Validation (BR-REFUND-001)
# ============================================================================

class TestTieredRefundScheduleValidation:
    """Verifies departure offset tiers under BR-REFUND-001."""

    def test_refund_greater_than_24h_is_90_percent(self):
        passed, expected, err = validate_cancellation_refund_schedule(
            hours_until_departure=36.0,
            reported_refund_percent=0.90,
        )
        assert passed is True
        assert expected == 0.90
        assert err == ""

    def test_refund_between_12_and_24h_is_50_percent(self):
        passed, expected, err = validate_cancellation_refund_schedule(
            hours_until_departure=18.0,
            reported_refund_percent=0.50,
        )
        assert passed is True
        assert expected == 0.50
        assert err == ""

    def test_refund_less_than_12h_is_0_percent(self):
        passed, expected, err = validate_cancellation_refund_schedule(
            hours_until_departure=5.0,
            reported_refund_percent=0.0,
        )
        assert passed is True
        assert expected == 0.0
        assert err == ""

    def test_refund_schedule_violation_flagged(self):
        passed, expected, err = validate_cancellation_refund_schedule(
            hours_until_departure=6.0,
            reported_refund_percent=0.90,  # Violation: <12h cannot be 90%
        )
        assert passed is False
        assert expected == 0.0
        assert "Refund schedule violation" in err

    def test_batch_validator_includes_fare_and_refund_rules(self):
        sample_output = {
            "original_fare": 2000.0,
            "replacement_fare": 2500.0,
            "fare_difference": 500.0,
            "hours_until_departure": 28.0,
            "refund_percentage": 0.90,
        }
        results = run_all_validators(sample_output)
        rule_names = [r["rule_name"] for r in results]
        assert "FareDifferenceArithmetic" in rule_names
        assert "TieredRefundScheduleBR001" in rule_names
        assert all(r["passed"] for r in results)


# ============================================================================
# 5. Robust JSON Extraction & Markdown Parsing
# ============================================================================

class TestBookingAgentJsonExtraction:
    """Verifies that _extract_json parses structured content across formatting styles."""

    def test_direct_json_object(self):
        raw = '{"fare_difference": 350.0, "passenger_pays_extra": true}'
        data = _extract_json(raw)
        assert data.get("fare_difference") == 350.0
        assert data.get("passenger_pays_extra") is True

    def test_markdown_code_fence(self):
        raw = """Here is the fare delta analysis:
```json
{
  "fare_difference": 500.0,
  "original_fare": 2000.0,
  "replacement_fare": 2500.0
}
```
Please let me know if you need notifications sent."""
        data = _extract_json(raw)
        assert data.get("fare_difference") == 500.0
        assert data.get("replacement_fare") == 2500.0

    def test_embedded_braces_in_commentary(self):
        raw = 'The computed result is {"fare_difference": 0.0, "note": "Equivalent fare"} according to catalog.'
        data = _extract_json(raw)
        assert data.get("fare_difference") == 0.0
        assert data.get("note") == "Equivalent fare"

    def test_invalid_text_fallback(self):
        raw = "Unable to compute fares due to missing route data."
        data = _extract_json(raw)
        assert "raw_response" in data
        assert data["raw_response"] == raw


# ============================================================================
# 6. Tool Call Recording for Database Persistence (ADR-004)
# ============================================================================

class TestBookingToolExecutionRecording:
    """Verifies that executed tool calls produce records matching AiToolCall schema."""

    @pytest.mark.asyncio
    async def test_tool_call_record_attributes(self):
        mock_tool_call = {
            "name": "calculate_fare_difference",
            "args": {
                "original_service_id": "srv-1",
                "replacement_service_id": "srv-2",
            },
            "id": "call-test-123",
        }

        # Mock the underlying tool invoke
        with patch("agents.booking_agent.BOOKING_AGENT_TOOLS") as mock_tools:
            mock_tool = AsyncMock()
            mock_tool.name = "calculate_fare_difference"
            mock_tool.ainvoke.return_value = json.dumps({
                "fare_difference": 400.0,
                "passenger_pays_extra": True,
            })
            mock_tools.__iter__.return_value = [mock_tool]

            result_str, record = await _execute_tool_call(mock_tool_call)

            assert "tool_name" in record
            assert record["tool_name"] == "calculate_fare_difference"
            assert "arguments_json" in record
            assert "result_json" in record
            assert "duration_ms" in record
            assert isinstance(record["duration_ms"], int)
            assert record["duration_ms"] >= 0


# ============================================================================
# 7. Safe Failure & Node Execution (FR-AI-004)
# ============================================================================

class TestBookingAgentNodeExecution:
    """Verifies agent state transitions, tool execution loops, and safe failure."""

    @pytest.mark.asyncio
    async def test_safe_failure_on_unhandled_exception(self):
        state = {
            "objective": "Evaluate replacement bus fares",
            "workflow_type": "disruption_rebooking",
            "retry_count": 2,  # 3rd attempt reaches MAX_RETRIES (3)
            "step_order": 2,
            "steps": [],
        }

        # Simulate exception during execution
        with patch("agents.booking_agent._booking_core", side_effect=RuntimeError("LLM API rate limit exceeded")):
            result = await booking_agent_node(state)

            assert result.get("workflow_status") == "SafeFailure"
            assert "RuntimeError" in result.get("error", "")
            assert result.get("retry_count") == 3

    @pytest.mark.asyncio
    async def test_safe_failure_short_circuit_when_already_failed(self):
        state = {
            "objective": "Evaluate replacement bus fares",
            "workflow_type": "disruption_rebooking",
            "workflow_status": "SafeFailure",
            "step_order": 2,
            "steps": [],
        }
        result = await booking_agent_node(state)
        assert result.get("workflow_status") == "SafeFailure"

    @pytest.mark.asyncio
    async def test_successful_node_populates_steps_and_tool_calls(self):
        state = {
            "objective": "Compare fare difference for disruption recovery",
            "workflow_type": "disruption_rebooking",
            "candidate_routes": [{"service_id": "srv-candidate-1"}],
            "feasibility_result": {"status": "Feasible"},
            "step_order": 2,
            "steps": [],
        }

        mock_llm_response = MagicMock()
        mock_llm_response.tool_calls = []
        mock_llm_response.content = json.dumps({
            "original_fare": 2000.0,
            "replacement_fare": 2400.0,
            "fare_difference": 400.0,
            "passenger_pays_extra": True,
            "policy_applied": "Disruption Alternative Guarantee",
        })

        with patch("agents.booking_agent.ChatGoogleGenerativeAI") as mock_chat_cls:
            mock_llm = MagicMock()
            mock_llm_with_tools = AsyncMock()
            mock_llm_with_tools.ainvoke.return_value = mock_llm_response
            mock_llm.bind_tools.return_value = mock_llm_with_tools
            mock_chat_cls.return_value = mock_llm

            result = await booking_agent_node(state)

            assert result["current_agent"] == "BookingPolicyAgent"
            assert result["step_order"] == 3
            assert "fare_analysis" in result
            assert result["fare_analysis"]["fare_difference"] == 400.0

            # Verify steps and tool_calls structure
            assert len(result["steps"]) == 1
            step = result["steps"][0]
            assert step["agent_name"] == "BookingPolicyAgent"
            assert "tool_calls" in step
            assert isinstance(step["tool_calls"], list)
            assert "validation_results" in step
            assert len(step["validation_results"]) >= 3
