"""
WayPoint AI — Safe Failure Guardrail (FR-AI-004, BR-AIVAL-002).

Wraps agent node functions with try/except and retry logic.
After 3 consecutive failures the workflow transitions to SafeFailure
with a structured error response (no crash, no unhandled exception).

Usage in agent nodes::

    from guardrails.safe_failure import execute_with_safe_failure

    async def my_agent_node(state: WorkflowState) -> dict:
        async def _core_logic(s: WorkflowState) -> dict:
            ...  # agent implementation
            return { ... }

        return await execute_with_safe_failure(
            agent_name="MyAgent",
            core_fn=_core_logic,
            state=state,
        )
"""

import logging
import traceback
from typing import Callable, Awaitable

from agents.state import WorkflowState
from config import AI_MAX_RETRIES as MAX_RETRIES

logger = logging.getLogger("waypoint.ai.guardrails.safe_failure")


class SafeFailureTriggered(Exception):
    """Raised when the maximum retry count is reached."""

    pass


async def execute_with_safe_failure(
    agent_name: str,
    core_fn: Callable[[WorkflowState], Awaitable[dict]],
    state: WorkflowState,
) -> dict:
    """
    Wrap an agent node's core logic with safe failure handling.

    - On success: returns the core function's result dict.
    - On failure (retry_count < MAX_RETRIES): increments retry_count,
      logs the error, and returns a partial state update that keeps the
      workflow running so the *next* node can still execute.
    - On failure (retry_count >= MAX_RETRIES): transitions the entire
      workflow to ``SafeFailure`` status.

    Args:
        agent_name: Human-readable agent name for logging.
        core_fn: The async function containing the agent's real logic.
                 Signature: ``async (WorkflowState) -> dict``.
        state: The current workflow state.

    Returns:
        State update dict (either from core_fn or a safe failure dict).
    """
    current_retries = state.get("retry_count", 0)

    # Already in SafeFailure — short circuit
    if state.get("workflow_status") == "SafeFailure":
        logger.warning(
            "%s skipped — workflow already in SafeFailure", agent_name
        )
        return {
            "current_agent": agent_name,
            "workflow_status": "SafeFailure",
        }

    try:
        result = await core_fn(state)
        return result

    except Exception as exc:
        new_retry_count = current_retries + 1
        error_detail = (
            f"{agent_name} failed (attempt {new_retry_count}/{MAX_RETRIES}): "
            f"{type(exc).__name__}: {exc}"
        )
        tb = traceback.format_exc()
        logger.error("%s\n%s", error_detail, tb)

        if new_retry_count >= MAX_RETRIES:
            # BR-AIVAL-002: Max retries reached → SafeFailure
            logger.critical(
                "%s: Max retries (%d) reached — entering SafeFailure",
                agent_name,
                MAX_RETRIES,
            )
            return {
                "current_agent": agent_name,
                "step_order": state.get("step_order", 0) + 1,
                "retry_count": new_retry_count,
                "workflow_status": "SafeFailure",
                "error": error_detail,
                "steps": state.get("steps", [])
                + [
                    {
                        "agent_name": agent_name,
                        "step_order": state.get("step_order", 0) + 1,
                        "step_description": (
                            f"SafeFailure after {new_retry_count} attempts: "
                            f"{type(exc).__name__}"
                        ),
                        "tool_calls": [],
                        "validation_results": [
                            {
                                "rule_name": "SafeFailureGuardrail",
                                "passed": False,
                                "validation_details": error_detail,
                            }
                        ],
                    }
                ],
            }
        else:
            # Below max retries — log and continue with degraded state
            logger.warning(
                "%s: Retry %d/%d — continuing with degraded state",
                agent_name,
                new_retry_count,
                MAX_RETRIES,
            )
            return {
                "current_agent": agent_name,
                "step_order": state.get("step_order", 0) + 1,
                "retry_count": new_retry_count,
                "error": error_detail,
                "steps": state.get("steps", [])
                + [
                    {
                        "agent_name": agent_name,
                        "step_order": state.get("step_order", 0) + 1,
                        "step_description": (
                            f"Error (attempt {new_retry_count}): "
                            f"{type(exc).__name__} — continuing"
                        ),
                        "tool_calls": [],
                        "validation_results": [
                            {
                                "rule_name": "SafeFailureGuardrail",
                                "passed": False,
                                "validation_details": error_detail,
                            }
                        ],
                    }
                ],
            }
