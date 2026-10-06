"""Tests for conditional graph routing — safety agent skipped for journey recommendations."""

import pytest
from unittest.mock import AsyncMock, patch, MagicMock
from agents.state import create_initial_state


@pytest.mark.asyncio
async def test_journey_recommendation_skips_safety_agent():
    """For journey_recommendation, the graph should NOT invoke the safety node."""
    with patch("agents.graph.planner_node", return_value={"step_order": 1}) as _, \
         patch("agents.graph.journey_agent_node", return_value={"step_order": 2}) as _, \
         patch("agents.graph.resource_agent_node", return_value={"step_order": 3}) as _, \
         patch("agents.graph.booking_agent_node", return_value={"step_order": 4}) as _, \
         patch("agents.graph.safety_agent_node", return_value={"step_order": 5, "workflow_status": "PendingManagerApproval"}) as mock_safety:

        from agents.graph import build_graph
        graph = build_graph()
        initial = create_initial_state(
            objective="Morning AC bus to Ella",
            workflow_type="journey_recommendation",
        )
        await graph.ainvoke(initial)
        mock_safety.assert_not_called()


@pytest.mark.asyncio
async def test_disruption_rebooking_runs_safety_agent():
    """For disruption_rebooking, the graph MUST invoke the safety node."""
    with patch("agents.graph.planner_node", return_value={"step_order": 1}) as _, \
         patch("agents.graph.journey_agent_node", return_value={"step_order": 2}) as _, \
         patch("agents.graph.resource_agent_node", return_value={"step_order": 3}) as _, \
         patch("agents.graph.booking_agent_node", return_value={"step_order": 4}) as _, \
         patch("agents.graph.safety_agent_node", return_value={"step_order": 5, "workflow_status": "PendingManagerApproval"}) as mock_safety:

        from agents.graph import build_graph
        graph = build_graph()
        initial = create_initial_state(
            objective="Disruption recovery for case abc",
            workflow_type="disruption_rebooking",
            disruption_case_id="abc",
        )
        await graph.ainvoke(initial)
        mock_safety.assert_called_once()
