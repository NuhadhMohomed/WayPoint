"""
WayPoint AI — Workflow Logger (FR-AI-005, FR-AI-007).

Persists AI workflow execution traces back to the ASP.NET Core backend
via the AiWorkflowController endpoints. Each workflow execution produces:
- 1 AiWorkflow record
- N AiWorkflowStep records (one per agent)
- M AiToolCall records (one per tool invocation)
- K AiValidationResult records (one per business rule check)

Usage::

    from persistence.workflow_logger import WorkflowLogger

    logger = WorkflowLogger()
    workflow_id = await logger.create_workflow("Find route Colombo to Ella")
    step_id = await logger.add_step(workflow_id, "JourneyAgent", 1, "Searched routes")
    await logger.add_tool_call(step_id, "SearchRoutes", '{"origin":"Colombo"}', '{}', 250)
    await logger.add_validation(step_id, "TransferWindow", True, "25 min OK")
    await logger.update_status(workflow_id, "Completed")
"""

import logging
from typing import Any

from tools.http_client import make_tool_request

logger = logging.getLogger("waypoint.ai.persistence")

# Backend endpoints (relative to API_BASE_URL configured in http_client)
_WORKFLOWS_PATH = "/ai/workflows"


class WorkflowLogger:
    """
    Persists workflow execution traces to the backend via HTTP.

    All methods are fire-and-forget with error logging -- persistence
    failures do NOT crash the workflow (the AI microservice continues
    to function even if the backend is unreachable).
    """

    async def create_workflow(self, objective: str) -> str | None:
        """
        Create a new AiWorkflow record.

        Args:
            objective: Natural language task description.

        Returns:
            The workflow UUID string, or None on failure.
        """
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
        return workflow_id

    async def get_workflow(self, workflow_id: str) -> dict[str, Any]:
        """
        Retrieve the full execution trace for a workflow.

        Returns:
            Full workflow response with nested steps, tool calls, validations.
        """
        result = await make_tool_request(
            "GET",
            f"{_WORKFLOWS_PATH}/{workflow_id}",
        )

        if isinstance(result, dict) and result.get("error"):
            logger.error("Failed to get workflow %s: %s", workflow_id, result)

        return result

    async def update_status(
        self,
        workflow_id: str,
        status: str,
    ) -> dict[str, Any] | None:
        """
        Update the workflow status.

        Args:
            workflow_id: UUID of the workflow.
            status: New status (Running, PendingManagerApproval, Completed, SafeFailure).

        Returns:
            Updated workflow response, or None on failure.
        """
        # Map string status to the enum integer value expected by ASP.NET
        status_map = {
            "Running": 0,
            "PendingManagerApproval": 1,
            "Completed": 2,
            "SafeFailure": 3,
        }

        result = await make_tool_request(
            "PUT",
            f"{_WORKFLOWS_PATH}/{workflow_id}/status",
            json_body={"status": status_map.get(status, 0)},
        )

        if isinstance(result, dict) and result.get("error"):
            logger.error(
                "Failed to update workflow %s status to %s: %s",
                workflow_id,
                status,
                result,
            )
            return None

        logger.info("Workflow %s status -> %s", workflow_id, status)
        return result

    async def add_step(
        self,
        workflow_id: str,
        agent_name: str,
        step_order: int,
        step_description: str = "",
    ) -> str | None:
        """
        Add a step to an existing workflow.

        Returns:
            The step UUID string, or None on failure.
        """
        result = await make_tool_request(
            "POST",
            f"{_WORKFLOWS_PATH}/{workflow_id}/steps",
            json_body={
                "agentName": agent_name,
                "stepOrder": step_order,
                "stepDescription": step_description,
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
            step_order,
        )
        return step_id

    async def add_tool_call(
        self,
        step_id: str,
        tool_name: str,
        arguments_json: str = "{}",
        result_json: str = "{}",
        duration_ms: int = 0,
    ) -> str | None:
        """
        Add a tool call record to an existing step.

        Returns:
            The tool call UUID string, or None on failure.
        """
        result = await make_tool_request(
            "POST",
            f"{_WORKFLOWS_PATH}/steps/{step_id}/tool-calls",
            json_body={
                "toolName": tool_name,
                "argumentsJson": arguments_json,
                "resultJson": result_json,
                "durationMs": duration_ms,
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
            duration_ms,
        )
        return tc_id

    async def add_validation(
        self,
        step_id: str,
        rule_name: str,
        passed: bool,
        validation_details: str | None = None,
    ) -> str | None:
        """
        Add a validation result to an existing step.

        Returns:
            The validation result UUID string, or None on failure.
        """
        result = await make_tool_request(
            "POST",
            f"{_WORKFLOWS_PATH}/steps/{step_id}/validations",
            json_body={
                "ruleName": rule_name,
                "passed": passed,
                "validationDetails": validation_details,
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
        return vr_id

    async def log_full_workflow(
        self,
        objective: str,
        steps: list[dict],
        final_status: str,
    ) -> str | None:
        """
        Convenience method to persist an entire workflow execution in one call.

        Creates the workflow, adds all steps with their tool calls and
        validations, and sets the final status.

        Args:
            objective: Workflow objective string.
            steps: List of step dicts from WorkflowState['steps'].
            final_status: Terminal status string.

        Returns:
            The workflow UUID, or None on failure.
        """
        workflow_id = await self.create_workflow(objective)
        if not workflow_id:
            return None

        for step_data in steps:
            step_id = await self.add_step(
                workflow_id,
                step_data.get("agent_name", "Unknown"),
                step_data.get("step_order", 0),
                step_data.get("step_description", ""),
            )

            if not step_id:
                continue

            # Persist tool calls
            for tc in step_data.get("tool_calls", []):
                await self.add_tool_call(
                    step_id,
                    tc.get("tool_name", ""),
                    tc.get("arguments_json", "{}"),
                    tc.get("result_json", "{}"),
                    tc.get("duration_ms", 0),
                )

            # Persist validation results
            for vr in step_data.get("validation_results", []):
                await self.add_validation(
                    step_id,
                    vr.get("rule_name", ""),
                    vr.get("passed", False),
                    vr.get("validation_details"),
                )

        await self.update_status(workflow_id, final_status)
        logger.info(
            "Full workflow persisted: %s (status=%s, steps=%d)",
            workflow_id,
            final_status,
            len(steps),
        )
        return workflow_id
