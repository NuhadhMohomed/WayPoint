"""Tests that WorkflowLogger is called after graph execution (ADR-004)."""

import pytest
from unittest.mock import AsyncMock, patch


@pytest.mark.asyncio
async def test_disruption_endpoint_calls_workflow_logger():
    """After graph execution, main.py must call WorkflowLogger.log_full_workflow."""
    mock_graph_result = {
        "workflow_status": "Completed",
        "step_order": 5,
        "candidate_routes": [],
        "feasibility_result": {},
        "fare_analysis": {},
        "impact_assessment": {},
        "approval_status": None,
        "steps": [{"agent_name": "PlannerAgent", "step_order": 1}],
    }

    with patch("main.workflow_graph") as mock_graph, \
         patch("main.WorkflowLogger") as MockLogger:
        mock_graph.ainvoke = AsyncMock(return_value=mock_graph_result)
        mock_instance = MockLogger.return_value
        mock_instance.log_full_workflow = AsyncMock(return_value="wf-123")

        from fastapi.testclient import TestClient
        from main import app

        client = TestClient(app)
        response = client.post("/api/ai/disruption-rebooking", json={
            "disruption_case_id": "case-001",
            "objective": "Recover disrupted service",
        })

        assert response.status_code == 200
        mock_instance.log_full_workflow.assert_called_once()


@pytest.mark.asyncio
async def test_persistence_failure_does_not_crash_workflow():
    """WorkflowLogger failure must NOT prevent the workflow from returning results."""
    mock_graph_result = {
        "workflow_status": "Completed",
        "step_order": 4,
        "candidate_routes": [{"id": "route-1"}],
        "feasibility_result": {},
        "fare_analysis": {},
        "steps": [],
    }

    with patch("main.workflow_graph") as mock_graph, \
         patch("main.WorkflowLogger") as MockLogger:
        mock_graph.ainvoke = AsyncMock(return_value=mock_graph_result)
        mock_instance = MockLogger.return_value
        mock_instance.log_full_workflow = AsyncMock(
            side_effect=Exception("Backend unreachable")
        )

        from fastapi.testclient import TestClient
        from main import app

        client = TestClient(app)
        response = client.post("/api/ai/journey-recommendation", json={
            "objective": "Morning bus to Ella",
        })

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "Completed"
