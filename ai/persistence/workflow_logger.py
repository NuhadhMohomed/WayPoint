"""
WayPoint AI — Workflow Logger (FR-AI-005, FR-AI-007, ADR-004).

Persists AI workflow execution traces back to the ASP.NET Core backend
via the AiWorkflowController endpoints. Each workflow execution produces:
- 1 AiWorkflow record
- N AiWorkflowStep records (one per agent invocation)
- M AiToolCall records (one per tool invocation with latency & I/O)
- K AiValidationResult records (one per deterministic rule validation)

Usage::

    from persistence.workflow_logger import WorkflowLogger

    logger = WorkflowLogger()
    workflow_id = await logger.create_workflow("Find route Colombo to Ella")
    step_id = await logger.add_step(workflow_id, "ValidationSafetyAgent", 1, "Evaluated safety")
    await logger.add_tool_call(step_id, "CalculatePassengerImpact", '{"service_id":"srv-1"}', '{"affected":28}', 120)
    await logger.add_validation(step_id, "HumanApprovalBoundary", True, "Requires manager sign-off: True")
    await logger.update_status(workflow_id, "PendingManagerApproval")
"""

import json
import logging
from typing import Any

from schemas.workflow import (
    AgentStepRecord,
    AuditLogRecord,
    ToolCallRecord,
    ValidationRecord,
)
from tools.http_client import make_tool_request

logger = logging.getLogger("waypoint.ai.persistence")

# Backend endpoints (relative to API_BASE_URL configured in http_client)
_WORKFLOWS_PATH = "/ai/workflows"

# Enum integer mapping for ASP.NET AiWorkflowStatus
STATUS_MAP: dict[str, int] = {
    "Running": 0,
    "PendingManagerApproval": 1,
    "Completed": 2,
    "SafeFailure": 3,
}


class WorkflowLogger:
    """
    Persists workflow execution traces to the backend via HTTP.

    All methods are designed to be resilient with safe failure handling:
    persistence failures do NOT crash the workflow runtime.
    """

    async def create_workflow(self, objective: str) -> str | None:
        """
        Create a new AiWorkflow record.
        POST /api/v1/ai/workflows

        Args:
            objective: Natural language task description.

        Returns:
            The workflow UUID string, or None on failure.
        """
        try:
            result = await make_tool_request(
                "POST",
                _WORKFLOWS_PATH,
                json_body={"objective": objective},
            )

            if isinstance(result, dict) and result.get("error"):
                logger.error("Failed to create workflow: %s", result)
                return None

            workflow_id = str(result.get("id", ""))
            logger.info("Workflow created: %s", workflow_id)
            return workflow_id if workflow_id else None
        except Exception as exc:
            logger.error("Exception creating workflow: %s", exc)
            return None

    async def get_workflow(self, workflow_id: str) -> dict[str, Any]:
        """
        Retrieve the full execution trace for a workflow.
        GET /api/v1/ai/workflows/{id}

        Returns:
            Full workflow response with nested steps, tool calls, validations.
        """
        try:
            result = await make_tool_request(
                "GET",
                f"{_WORKFLOWS_PATH}/{workflow_id}",
            )

            if isinstance(result, dict) and result.get("error"):
                logger.error("Failed to get workflow %s: %s", workflow_id, result)

            return result
        except Exception as exc:
            logger.error("Exception getting workflow %s: %s", workflow_id, exc)
            return {"error": True, "detail": str(exc)}

    async def update_status(
        self,
        workflow_id: str,
        status: str | int,
    ) -> dict[str, Any] | None:
        """
        Update the workflow status.
        PUT /api/v1/ai/workflows/{id}/status

        Args:
            workflow_id: UUID of the workflow.
            status: New status ("Running", "PendingManagerApproval", "Completed", "SafeFailure", or int).

        Returns:
            Updated workflow response, or None on failure.
        """
        try:
            if isinstance(status, str):
                status_val = STATUS_MAP.get(status, 0)
            else:
                status_val = int(status)

            result = await make_tool_request(
                "PUT",
                f"{_WORKFLOWS_PATH}/{workflow_id}/status",
                json_body={"status": status_val},
            )

            if isinstance(result, dict) and result.get("error"):
                logger.error(
                    "Failed to update workflow %s status to %s: %s",
                    workflow_id,
                    status,
                    result,
                )
                return None

            logger.info("Workflow %s status -> %s (%d)", workflow_id, status, status_val)
            return result
        except Exception as exc:
            logger.error("Exception updating workflow %s status: %s", workflow_id, exc)
            return None

    async def add_step(
        self,
        workflow_id: str,
        agent_name: str,
        step_order: int,
        description: str = "",
        *,
        step_description: str | None = None,
    ) -> str | None:
        """
        Add a step to an existing workflow.
        POST /api/v1/ai/workflows/{id}/steps

        Args:
            workflow_id: Workflow UUID.
            agent_name: Name of the agent executing the step.
            step_order: Execution sequence index (>= 0).
            description: Description of the step action.
            step_description: Alias for description.

        Returns:
            The step UUID string, or None on failure.
        """
        try:
            desc = step_description if step_description is not None else description
            step_order_clean = max(0, int(step_order))

            result = await make_tool_request(
                "POST",
                f"{_WORKFLOWS_PATH}/{workflow_id}/steps",
                json_body={
                    "agentName": agent_name,
                    "stepOrder": step_order_clean,
                    "stepDescription": desc,
                },
            )

            if isinstance(result, dict) and result.get("error"):
                logger.error(
                    "Failed to add step to workflow %s: %s",
                    workflow_id,
                    result,
                )
                return None

            step_id = str(result.get("id", ""))
            logger.info(
                "Step added: %s (agent=%s, order=%d)",
                step_id,
                agent_name,
                step_order_clean,
            )
            return step_id if step_id else None
        except Exception as exc:
            logger.error("Exception adding step to workflow %s: %s", workflow_id, exc)
            return None

    async def add_tool_call(
        self,
        step_id: str,
        tool_name: str,
        args_json: str | dict[str, Any] = "{}",
        result_json: str | dict[str, Any] = "{}",
        duration_ms: int = 0,
        *,
        arguments_json: str | dict[str, Any] | None = None,
    ) -> str | None:
        """
        Add a tool call record to an existing step.
        POST /api/v1/ai/workflows/steps/{id}/tools

        Args:
            step_id: Step UUID.
            tool_name: Name of the executed tool.
            args_json: Tool input arguments JSON or dictionary.
            result_json: Tool execution result JSON or dictionary.
            duration_ms: Execution duration in milliseconds (must be >= 0).
            arguments_json: Alias for args_json.

        Returns:
            The tool call UUID string, or None on failure.
        """
        try:
            # Handle args aliases and serialization to PostgreSQL JSONB
            raw_args = arguments_json if arguments_json is not None else args_json
            if isinstance(raw_args, (dict, list)):
                clean_args = json.dumps(raw_args)
            elif isinstance(raw_args, str) and raw_args.strip():
                clean_args = raw_args.strip()
            else:
                clean_args = "{}"

            if isinstance(result_json, (dict, list)):
                clean_result = json.dumps(result_json)
            elif isinstance(result_json, str) and result_json.strip():
                clean_result = result_json.strip()
            else:
                clean_result = "{}"

            # Enforce non-negative duration metric (FR-AI-005)
            duration_clean = max(0, int(duration_ms))

            result = await make_tool_request(
                "POST",
                f"{_WORKFLOWS_PATH}/steps/{step_id}/tools",
                json_body={
                    "toolName": tool_name,
                    "argumentsJson": clean_args,
                    "resultJson": clean_result,
                    "durationMs": duration_clean,
                },
            )

            if isinstance(result, dict) and result.get("error"):
                logger.error(
                    "Failed to add tool call to step %s: %s",
                    step_id,
                    result,
                )
                return None

            tc_id = str(result.get("id", ""))
            logger.info(
                "Tool call added: %s (tool=%s, duration=%dms)",
                tc_id,
                tool_name,
                duration_clean,
            )
            return tc_id if tc_id else None
        except Exception as exc:
            logger.error("Exception adding tool call to step %s: %s", step_id, exc)
            return None

    async def add_validation(
        self,
        step_id: str,
        rule_name: str,
        passed: bool,
        details: str | dict[str, Any] | None = None,
        *,
        validation_details: str | dict[str, Any] | None = None,
    ) -> str | None:
        """
        Add a validation result to an existing step.
        POST /api/v1/ai/workflows/steps/{id}/validations

        Args:
            step_id: Step UUID.
            rule_name: Name of the business rule validated.
            passed: Whether the validation passed.
            details: Validation details string or structured dict (ADR-004).
            validation_details: Alias for details.

        Returns:
            The validation result UUID string, or None on failure.
        """
        try:
            raw_details = validation_details if validation_details is not None else details
            if isinstance(raw_details, (dict, list)):
                clean_details = json.dumps(raw_details)
            elif raw_details is not None:
                clean_details = str(raw_details)
            else:
                clean_details = None

            result = await make_tool_request(
                "POST",
                f"{_WORKFLOWS_PATH}/steps/{step_id}/validations",
                json_body={
                    "ruleName": rule_name,
                    "passed": bool(passed),
                    "validationDetails": clean_details,
                },
            )

            if isinstance(result, dict) and result.get("error"):
                logger.error(
                    "Failed to add validation to step %s: %s",
                    step_id,
                    result,
                )
                return None

            vr_id = str(result.get("id", ""))
            logger.info(
                "Validation added: %s (rule=%s, passed=%s)",
                vr_id,
                rule_name,
                passed,
            )
            return vr_id if vr_id else None
        except Exception as exc:
            logger.error("Exception adding validation to step %s: %s", step_id, exc)
            return None

    async def log_full_workflow(
        self,
        objective: str,
        steps: list[dict[str, Any]],
        final_status: str,
    ) -> str | None:
        """
        Convenience method to persist an entire workflow execution in order.

        Creates the workflow, adds all steps with their tool calls and
        validations, and sets the final status.

        Args:
            objective: Workflow objective string.
            steps: List of step dicts from WorkflowState['steps'].
            final_status: Terminal status string ("Completed", "PendingManagerApproval", "SafeFailure").

        Returns:
            The workflow UUID, or None on failure.
        """
        try:
            workflow_id = await self.create_workflow(objective)
            if not workflow_id:
                return None

            for step_data in steps:
                agent_name = step_data.get("agent_name") or step_data.get("agentName", "Unknown")
                step_order = step_data.get("step_order") or step_data.get("stepOrder", 0)
                step_description = (
                    step_data.get("step_description")
                    or step_data.get("stepDescription")
                    or step_data.get("description", "")
                )

                step_id = await self.add_step(
                    workflow_id,
                    agent_name=agent_name,
                    step_order=step_order,
                    description=step_description,
                )

                if not step_id:
                    continue

                # Persist tool calls
                tool_calls = step_data.get("tool_calls") or step_data.get("toolCalls", [])
                for tc in tool_calls:
                    tc_name = tc.get("tool_name") or tc.get("toolName", "")
                    tc_args = (
                        tc.get("arguments_json")
                        or tc.get("argumentsJson")
                        or tc.get("args_json")
                        or tc.get("args", "{}")
                    )
                    tc_res = (
                        tc.get("result_json")
                        or tc.get("resultJson")
                        or tc.get("result", "{}")
                    )
                    tc_dur = tc.get("duration_ms") or tc.get("durationMs", 0)

                    await self.add_tool_call(
                        step_id,
                        tool_name=tc_name,
                        args_json=tc_args,
                        result_json=tc_res,
                        duration_ms=tc_dur,
                    )

                # Persist validation results
                validations = step_data.get("validation_results") or step_data.get("validationResults", [])
                for vr in validations:
                    rule_name = vr.get("rule_name") or vr.get("ruleName", "")
                    passed = vr.get("passed", False)
                    details = (
                        vr.get("validation_details")
                        or vr.get("validationDetails")
                        or vr.get("details")
                    )

                    await self.add_validation(
                        step_id,
                        rule_name=rule_name,
                        passed=passed,
                        details=details,
                    )

            await self.update_status(workflow_id, final_status)
            logger.info(
                "Full workflow persisted: %s (status=%s, steps=%d)",
                workflow_id,
                final_status,
                len(steps),
            )
            return workflow_id
        except Exception as exc:
            logger.error("Exception during log_full_workflow: %s", exc)
            return None

    def create_audit_record(
        self,
        actor_id: str,
        action_type: str,
        entity_name: str,
        entity_id: str,
        before_state: dict[str, Any] | str | None = None,
        after_state: dict[str, Any] | str | None = None,
    ) -> AuditLogRecord:
        """
        Create a typed AuditLogRecord for PostgreSQL AuditLogs schema (ADR-004).
        """
        return AuditLogRecord(
            actor_id=actor_id,
            action_type=action_type,
            entity_name=entity_name,
            entity_id=entity_id,
            before_state_json=before_state if isinstance(before_state, str) else json.dumps(before_state) if before_state else None,
            after_state_json=after_state if isinstance(after_state, str) else json.dumps(after_state) if after_state else None,
        )
