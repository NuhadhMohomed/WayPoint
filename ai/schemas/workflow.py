"""
WayPoint AI — Workflow State Pydantic Schemas.

Models for workflow requests/responses and execution tracking.
Maps to the AiWorkflow / AiWorkflowStep / AiToolCall / AiValidationResult
entities defined in AiEntities.cs (ADR-004).
"""

from pydantic import BaseModel, Field


# ===========================================================================
# FastAPI Request / Response Models
# ===========================================================================


class WorkflowRequest(BaseModel):
    """Inbound request to trigger an AI workflow."""

    disruption_case_id: str | None = Field(
        None,
        description="UUID of the disruption case (for disruption-rebooking)",
    )
    objective: str = Field(
        default="",
        description="Natural language objective describing the task",
    )


class WorkflowResult(BaseModel):
    """Structured result returned from a completed AI workflow."""

    workflow_id: str = ""
    workflow_type: str = ""
    status: str = "Running"  # Running | PendingManagerApproval | Completed | SafeFailure
    steps_completed: int = 0
    candidate_routes: list[dict] = Field(default_factory=list)
    feasibility_result: dict | None = None
    fare_analysis: dict | None = None
    impact_assessment: dict | None = None
    approval_status: str | None = None
    error: str | None = None


# ===========================================================================
# Execution Tracking Models (for persistence via backend API)
# Maps to: AiToolCall entity (AiEntities.cs L31-L42)
# ===========================================================================


class ToolCallRecord(BaseModel):
    """Record of a single tool invocation for AiToolCall persistence."""

    tool_name: str
    arguments_json: str = "{}"
    result_json: str = "{}"
    duration_ms: int = 0
    executed_at: str = ""


# Maps to: AiValidationResult entity (AiEntities.cs L44-L53)
class ValidationRecord(BaseModel):
    """Record of a deterministic rule validation for AiValidationResult persistence."""

    rule_name: str
    passed: bool
    validation_details: str | None = None


# Maps to: AiWorkflowStep entity (AiEntities.cs L17-L29)
class AgentStepRecord(BaseModel):
    """Record of a single agent step for AiWorkflowStep persistence."""

    agent_name: str
    step_order: int
    step_description: str = ""
    executed_at: str = ""
    tool_calls: list[ToolCallRecord] = Field(default_factory=list)
    validation_results: list[ValidationRecord] = Field(default_factory=list)


# ===========================================================================
# Backend Persistence DTOs
# (Match the new AiWorkflowController endpoint contracts — Phase 5)
# ===========================================================================


class CreateWorkflowDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows."""

    objective: str


class CreateWorkflowStepDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows/{id}/steps."""

    agent_name: str
    step_order: int
    step_description: str = ""


class CreateToolCallDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows/steps/{stepId}/tool-calls."""

    tool_name: str
    arguments_json: str = "{}"
    result_json: str = "{}"
    duration_ms: int = 0


class CreateValidationResultDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows/steps/{stepId}/validations."""

    rule_name: str
    passed: bool
    validation_details: str | None = None


class UpdateWorkflowStatusDto(BaseModel):
    """DTO for PUT /api/v1/ai/workflows/{id}/status."""

    status: str  # Running | PendingManagerApproval | Completed | SafeFailure
