"""
WayPoint AI — LangGraph Workflow State Definition.

Shared TypedDict state passed between all agent nodes in the graph.
Tracks inputs, execution progress, agent outputs, and control flow.
"""

from typing import Annotated, TypedDict

from langgraph.graph.message import add_messages


class WorkflowState(TypedDict):
    """
    Shared state flowing through the multi-agent LangGraph workflow.

    All agent nodes read from and write to this state dict. LangGraph
    manages state transitions automatically via graph edges.
    """

    # --- Input Fields (set at workflow start) ---
    objective: str
    """Natural language objective describing the task."""

    workflow_type: str
    """'journey_recommendation' or 'disruption_rebooking'."""

    disruption_case_id: str
    """UUID of the disruption case (empty for journey recommendation)."""

    workflow_id: str
    """UUID of the AiWorkflow record in the backend."""

    # --- LangGraph Message History ---
    messages: Annotated[list, add_messages]
    """Accumulated message history for agent reasoning."""

    # --- Execution Tracking ---
    current_agent: str
    """Name of the currently active agent."""

    step_order: int
    """Current step counter (incremented per agent)."""

    steps: list[dict]
    """Accumulated step records for workflow persistence."""

    # --- Agent Outputs (populated by each agent in sequence) ---
    candidate_routes: list[dict]
    """Journey Analysis Agent output: ranked candidate routes/services."""

    feasibility_result: dict
    """Resource Feasibility Agent output: bus/driver/seat evaluation."""

    fare_analysis: dict
    """Booking & Policy Agent output: fare difference and policy outcome."""

    impact_assessment: dict
    """Validation & Safety Agent output: passenger impact metrics."""

    # --- Control Flow & Decision State ---
    impact_classification: str
    """Impact level: 'Low' (auto-execute) or 'High' (requires approval)."""

    requires_approval: bool
    """Whether the workflow needs Transport Manager sign-off."""

    approval_status: str
    """'' | 'PendingManagerApproval' | 'Approved' | 'Rejected'."""

    workflow_status: str
    """'Running' | 'PendingManagerApproval' | 'Completed' | 'SafeFailure'."""

    error: str
    """Error message if workflow enters SafeFailure state."""

    # --- Retry Tracking (BR-AIVAL-002: max 3 retries) ---
    retry_count: int
    """Current retry counter. Workflow enters SafeFailure at >= 3."""


def create_initial_state(
    objective: str,
    workflow_type: str,
    disruption_case_id: str = "",
    workflow_id: str = "",
) -> WorkflowState:
    """
    Create the initial workflow state for a new execution.

    Args:
        objective: Natural language task description.
        workflow_type: 'journey_recommendation' or 'disruption_rebooking'.
        disruption_case_id: UUID of the disruption case (rebooking only).
        workflow_id: UUID of the AiWorkflow record.

    Returns:
        A fully initialised WorkflowState dict.
    """
    return WorkflowState(
        objective=objective,
        workflow_type=workflow_type,
        disruption_case_id=disruption_case_id,
        workflow_id=workflow_id,
        messages=[],
        current_agent="",
        step_order=0,
        steps=[],
        candidate_routes=[],
        feasibility_result={},
        fare_analysis={},
        impact_assessment={},
        impact_classification="",
        requires_approval=False,
        approval_status="",
        workflow_status="Running",
        error="",
        retry_count=0,
    )
