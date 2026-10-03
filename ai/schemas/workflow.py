"""
WayPoint AI — Workflow State Pydantic Schemas.

Models for workflow requests/responses and execution tracking.
Maps to the AiWorkflow / AiWorkflowStep / AiToolCall / AiValidationResult
and AuditLog entities defined in AiEntities.cs and AuditLog.cs (ADR-004, FR-AI-005, FR-AI-007).
Ensures clean JSON serialization to PostgreSQL JSONB columns.
"""

import json
from typing import Any
from pydantic import BaseModel, ConfigDict, Field, field_validator


# ===========================================================================
# FastAPI Request / Response Models
# ===========================================================================


class WorkflowRequest(BaseModel):
    """Inbound request to trigger an AI workflow."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    disruption_case_id: str | None = Field(
        None,
        alias="disruptionCaseId",
        description="UUID of the disruption case (for disruption-rebooking)",
    )
    objective: str = Field(
        default="",
        alias="objective",
        description="Natural language objective describing the task",
    )


class WorkflowResult(BaseModel):
    """Structured result returned from a completed AI workflow."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    workflow_id: str = Field(default="", alias="workflowId")
    workflow_type: str = Field(default="", alias="workflowType")
    status: str = Field(
        default="Running", alias="status"
    )  # Running | PendingManagerApproval | Completed | SafeFailure
    steps_completed: int = Field(default=0, alias="stepsCompleted")
    candidate_routes: list[dict] = Field(
        default_factory=list, alias="candidateRoutes"
    )
    feasibility_result: dict | None = Field(
        default=None, alias="feasibilityResult"
    )
    fare_analysis: dict | None = Field(default=None, alias="fareAnalysis")
    impact_assessment: dict | None = Field(
        default=None, alias="impactAssessment"
    )
    approval_status: str | None = Field(default=None, alias="approvalStatus")
    error: str | None = Field(default=None, alias="error")


# ===========================================================================
# Execution Tracking Models (for persistence via backend API)
# Maps to: AiToolCall entity (AiEntities.cs L31-L42, PostgreSQL jsonb)
# ===========================================================================


class ToolCallRecord(BaseModel):
    """Record of a single tool invocation for AiToolCall persistence."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    tool_name: str = Field(alias="toolName")
    arguments_json: str = Field(default="{}", alias="argumentsJson")
    result_json: str = Field(default="{}", alias="resultJson")
    duration_ms: int = Field(default=0, ge=0, alias="durationMs")
    executed_at: str = Field(default="", alias="executedAt")
    id: str | None = Field(default=None, alias="id")
    ai_workflow_step_id: str | None = Field(
        default=None, alias="aiWorkflowStepId"
    )

    @field_validator("arguments_json", "result_json", mode="before")
    @classmethod
    def validate_json_field(cls, v: Any) -> str:
        """Ensure clean serialization into PostgreSQL JSONB columns."""
        if v is None:
            return "{}"
        if isinstance(v, (dict, list)):
            return json.dumps(v)
        if isinstance(v, str):
            v_str = v.strip()
            if not v_str:
                return "{}"
            try:
                json.loads(v_str)
                return v_str
            except json.JSONDecodeError as exc:
                raise ValueError(
                    f"Invalid JSON string for JSONB column: {exc}"
                ) from exc
        return str(v)

    def get_arguments(self) -> dict[str, Any]:
        """Parse arguments_json back into a dictionary."""
        try:
            return json.loads(self.arguments_json)
        except Exception:
            return {}

    def get_result(self) -> Any:
        """Parse result_json back into Python object."""
        try:
            return json.loads(self.result_json)
        except Exception:
            return self.result_json


# Maps to: AiValidationResult entity (AiEntities.cs L44-L53)
class ValidationRecord(BaseModel):
    """Record of a deterministic rule validation for AiValidationResult persistence."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    rule_name: str = Field(alias="ruleName")
    passed: bool = Field(alias="passed")
    validation_details: str | None = Field(
        default=None, alias="validationDetails"
    )
    id: str | None = Field(default=None, alias="id")
    ai_workflow_step_id: str | None = Field(
        default=None, alias="aiWorkflowStepId"
    )

    @field_validator("validation_details", mode="before")
    @classmethod
    def validate_details_field(cls, v: Any) -> str | None:
        """Ensure validation details serialize cleanly to text or JSON."""
        if v is None:
            return None
        if isinstance(v, (dict, list)):
            return json.dumps(v)
        return str(v)

    @classmethod
    def create_with_state_capture(
        cls,
        rule_name: str,
        passed: bool,
        before_state: dict[str, Any] | None = None,
        after_state: dict[str, Any] | None = None,
        reason: str | None = None,
    ) -> "ValidationRecord":
        """
        Create a validation record capturing before/after state per ADR-004 audit schema.
        """
        details_obj = {
            "rule": rule_name,
            "passed": passed,
            "before_state": before_state or {},
            "after_state": after_state or {},
            "reason": reason or "",
        }
        return cls(
            rule_name=rule_name,
            passed=passed,
            validation_details=json.dumps(details_obj),
        )


# Maps to: AiWorkflowStep entity (AiEntities.cs L17-L29)
class AgentStepRecord(BaseModel):
    """Record of a single agent step for AiWorkflowStep persistence."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    agent_name: str = Field(alias="agentName")
    step_order: int = Field(default=0, ge=0, alias="stepOrder")
    step_description: str = Field(default="", alias="stepDescription")
    executed_at: str = Field(default="", alias="executedAt")
    tool_calls: list[ToolCallRecord] = Field(
        default_factory=list, alias="toolCalls"
    )
    validation_results: list[ValidationRecord] = Field(
        default_factory=list, alias="validationResults"
    )
    id: str | None = Field(default=None, alias="id")
    ai_workflow_id: str | None = Field(default=None, alias="aiWorkflowId")

    def add_tool_call(self, record: ToolCallRecord | dict[str, Any]) -> None:
        """Append a tool call record to this step."""
        if isinstance(record, dict):
            record = ToolCallRecord(**record)
        self.tool_calls.append(record)

    def add_validation(self, record: ValidationRecord | dict[str, Any]) -> None:
        """Append a validation record to this step."""
        if isinstance(record, dict):
            record = ValidationRecord(**record)
        self.validation_results.append(record)


# ===========================================================================
# Immutable Relational Audit Log Model (ADR-004, AuditLog.cs)
# ===========================================================================


class AuditLogRecord(BaseModel):
    """
    Immutable relational audit log record for PostgreSQL AuditLogs table (ADR-004).
    Captures before/after state jsonb for deterministic operational auditability.
    """

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    actor_id: str = Field(alias="actorId")
    action_type: str = Field(alias="actionType")
    entity_name: str = Field(alias="entityName")
    entity_id: str = Field(alias="entityId")
    before_state_json: str | None = Field(
        default=None, alias="beforeStateJson"
    )
    after_state_json: str | None = Field(
        default=None, alias="afterStateJson"
    )
    timestamp: str = Field(default="", alias="timestamp")
    id: str | None = Field(default=None, alias="id")

    @field_validator("before_state_json", "after_state_json", mode="before")
    @classmethod
    def validate_audit_json(cls, v: Any) -> str | None:
        """Ensure audit state fields serialize cleanly to PostgreSQL JSONB."""
        if v is None:
            return None
        if isinstance(v, (dict, list)):
            return json.dumps(v)
        if isinstance(v, str):
            v_str = v.strip()
            if not v_str:
                return None
            try:
                json.loads(v_str)
                return v_str
            except json.JSONDecodeError as exc:
                raise ValueError(
                    f"Invalid JSON string for audit state: {exc}"
                ) from exc
        return str(v)


# ===========================================================================
# Full AI Workflow Trace Model (AiWorkflowResponseDto)
# ===========================================================================


class AiWorkflowRecord(BaseModel):
    """Full AI workflow execution trace mapping to AiWorkflowResponseDto."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    id: str = Field(default="", alias="id")
    objective: str = Field(default="", alias="objective")
    status: str = Field(default="Running", alias="status")
    started_at: str = Field(default="", alias="startedAt")
    completed_at: str | None = Field(default=None, alias="completedAt")
    steps: list[AgentStepRecord] = Field(
        default_factory=list, alias="steps"
    )


# ===========================================================================
# Backend Persistence DTOs
# (Match the AiWorkflowController endpoint contracts — Phase 5)
# ===========================================================================


class CreateWorkflowDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    objective: str = Field(alias="objective")


class CreateWorkflowStepDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows/{id}/steps."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    agent_name: str = Field(alias="agentName")
    step_order: int = Field(default=0, ge=0, alias="stepOrder")
    step_description: str = Field(default="", alias="stepDescription")


class CreateToolCallDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows/steps/{stepId}/tool-calls."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    tool_name: str = Field(alias="toolName")
    arguments_json: str = Field(default="{}", alias="argumentsJson")
    result_json: str = Field(default="{}", alias="resultJson")
    duration_ms: int = Field(default=0, ge=0, alias="durationMs")

    @field_validator("arguments_json", "result_json", mode="before")
    @classmethod
    def validate_json_field(cls, v: Any) -> str:
        if v is None:
            return "{}"
        if isinstance(v, (dict, list)):
            return json.dumps(v)
        if isinstance(v, str):
            v_str = v.strip()
            if not v_str:
                return "{}"
            try:
                json.loads(v_str)
                return v_str
            except json.JSONDecodeError as exc:
                raise ValueError(f"Invalid JSON string: {exc}") from exc
        return str(v)


class CreateValidationResultDto(BaseModel):
    """DTO for POST /api/v1/ai/workflows/steps/{stepId}/validations."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    rule_name: str = Field(alias="ruleName")
    passed: bool = Field(alias="passed")
    validation_details: str | None = Field(
        default=None, alias="validationDetails"
    )

    @field_validator("validation_details", mode="before")
    @classmethod
    def validate_details_field(cls, v: Any) -> str | None:
        if v is None:
            return None
        if isinstance(v, (dict, list)):
            return json.dumps(v)
        return str(v)


class UpdateWorkflowStatusDto(BaseModel):
    """DTO for PUT /api/v1/ai/workflows/{id}/status."""

    model_config = ConfigDict(populate_by_name=True, extra="ignore")

    status: str | int = Field(alias="status")
