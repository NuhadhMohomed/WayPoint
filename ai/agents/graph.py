"""
WayPoint AI — Multi-Agent LangGraph Workflow Graph (FR-AI-001).

Defines the StateGraph wiring:
  START → planner → journey → resource → booking → safety → END

The safety agent node sets the terminal workflow_status:
  - 'PendingManagerApproval' (High-Impact)  → halts for manager
  - 'Completed' (Low-Impact)                → auto-completes
  - 'SafeFailure'                           → safe failure state
"""

import logging

from langgraph.graph import StateGraph, START, END

from agents.state import WorkflowState
from agents.planner import planner_node
from agents.journey_agent import journey_agent_node
from agents.resource_agent import resource_agent_node
from agents.booking_agent import booking_agent_node
from agents.safety_agent import safety_agent_node

logger = logging.getLogger("waypoint.ai.graph")


def _route_after_booking(state: WorkflowState) -> str:
    """
    Conditional edge after the Booking & Policy Agent.

    Journey recommendations skip the Safety Agent (it has no disruption
    to classify).  Disruption rebooking workflows proceed to Safety.
    """
    if state.get("workflow_type") == "journey_recommendation":
        logger.info("Skipping safety agent for journey_recommendation")
        return END
    return "safety"


def _route_after_safety(state: WorkflowState) -> str:
    """
    Conditional edge after the Validation & Safety Agent.

    Routes to END in all cases — the terminal status is already
    set in workflow_status by the safety agent:
    - 'PendingManagerApproval' for high-impact
    - 'Completed' for low-impact
    - 'SafeFailure' for error cases (wired in Phase 4)
    """
    status = state.get("workflow_status", "Completed")
    logger.info("Workflow routing | status=%s", status)
    return END


def build_graph() -> StateGraph:
    """
    Build and return the compiled multi-agent LangGraph workflow.

    Graph structure:
        START → planner → journey → resource → booking
          → (journey_recommendation) END
          → (disruption_rebooking)   safety → END

    The safety agent sets the workflow_status field to control
    what happens after the graph completes:
    - PendingManagerApproval: Response indicates manager review needed
    - Completed: Response indicates success
    - SafeFailure: Response indicates failure with safe fallback
    """
    graph = StateGraph(WorkflowState)

    # Register agent nodes
    graph.add_node("planner", planner_node)
    graph.add_node("journey", journey_agent_node)
    graph.add_node("resource", resource_agent_node)
    graph.add_node("booking", booking_agent_node)
    graph.add_node("safety", safety_agent_node)

    # Sequential edges: START → planner → journey → resource → booking
    graph.add_edge(START, "planner")
    graph.add_edge("planner", "journey")
    graph.add_edge("journey", "resource")
    graph.add_edge("resource", "booking")

    # Conditional edge after booking: skip safety for journey recommendations
    graph.add_conditional_edges("booking", _route_after_booking)

    # Conditional edge after safety → END
    graph.add_conditional_edges("safety", _route_after_safety)

    return graph.compile()


# Module-level compiled graph singleton
workflow_graph = build_graph()
