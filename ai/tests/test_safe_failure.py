"""
WayPoint AI — Test Suite: Safe Failure Guardrail (FR-AI-004, BR-AIVAL-002).

Tests:
- 3 consecutive failures trigger SafeFailure
- Workflow status transitions correctly
- Success path returns core function result
- SafeFailure short-circuits subsequent calls
- Retry count increments correctly
"""

import sys
import pytest

sys.path.insert(0, ".")


@pytest.fixture
def initial_state():
    from agents.state import create_initial_state

    return create_initial_state(
        objective="test", workflow_type="disruption_rebooking"
    )


class TestSafeFailure:
    """Tests for execute_with_safe_failure guardrail."""

    @pytest.mark.asyncio
    async def test_success_returns_result(self, initial_state):
        from guardrails.safe_failure import execute_with_safe_failure

        async def _succeeds(state):
            return {"current_agent": "TestAgent", "step_order": 42}

        result = await execute_with_safe_failure(
            "TestAgent", _succeeds, initial_state
        )
        assert result["current_agent"] == "TestAgent"
        assert result["step_order"] == 42

    @pytest.mark.asyncio
    async def test_single_failure_increments_retry(self, initial_state):
        from guardrails.safe_failure import execute_with_safe_failure

        async def _fails(state):
            raise RuntimeError("Boom")

        result = await execute_with_safe_failure(
            "TestAgent", _fails, initial_state
        )
        assert result["retry_count"] == 1
        assert result.get("workflow_status") != "SafeFailure"

    @pytest.mark.asyncio
    async def test_two_failures_not_safe_failure(self, initial_state):
        from guardrails.safe_failure import execute_with_safe_failure

        async def _fails(state):
            raise RuntimeError("Boom")

        state = {**initial_state, "retry_count": 1}
        result = await execute_with_safe_failure("TestAgent", _fails, state)
        assert result["retry_count"] == 2
        assert result.get("workflow_status") != "SafeFailure"

    @pytest.mark.asyncio
    async def test_three_failures_triggers_safe_failure(self, initial_state):
        from guardrails.safe_failure import execute_with_safe_failure

        async def _fails(state):
            raise RuntimeError("Boom")

        state = {**initial_state, "retry_count": 2}
        result = await execute_with_safe_failure("TestAgent", _fails, state)
        assert result["retry_count"] == 3
        assert result["workflow_status"] == "SafeFailure"
        assert "error" in result
        assert "SafeFailureGuardrail" in str(result["steps"])

    @pytest.mark.asyncio
    async def test_short_circuits_in_safe_failure(self, initial_state):
        from guardrails.safe_failure import execute_with_safe_failure

        async def _should_not_run(state):
            raise AssertionError("Should not have been called")

        state = {**initial_state, "workflow_status": "SafeFailure"}
        result = await execute_with_safe_failure(
            "TestAgent", _should_not_run, state
        )
        assert result["workflow_status"] == "SafeFailure"

    @pytest.mark.asyncio
    async def test_error_message_includes_agent_name(self, initial_state):
        from guardrails.safe_failure import execute_with_safe_failure

        async def _fails(state):
            raise ValueError("Custom error message")

        result = await execute_with_safe_failure(
            "MyCustomAgent", _fails, initial_state
        )
        assert "MyCustomAgent" in result["error"]
        assert "Custom error message" in result["error"]
