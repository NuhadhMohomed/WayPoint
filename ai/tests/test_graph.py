"""
WayPoint AI — Test Suite: LangGraph Workflow Graph.

Tests:
- Graph compiles successfully
- State factory creates valid initial state
- All 5 agent nodes are callable
- Deterministic impact classification (output validator)
- Output validator functions
- WorkflowLogger instantiation
"""

import sys
import pytest

sys.path.insert(0, ".")


class TestGraphCompilation:
    """Tests for the LangGraph workflow graph."""

    def test_graph_compiles(self):
        from agents.graph import workflow_graph

        assert workflow_graph is not None

    def test_graph_type(self):
        from agents.graph import workflow_graph

        type_name = type(workflow_graph).__name__
        assert "CompiledStateGraph" in type_name


class TestWorkflowState:
    """Tests for the workflow state factory."""

    def test_initial_state_defaults(self):
        from agents.state import create_initial_state

        state = create_initial_state(
            objective="test",
            workflow_type="disruption_rebooking",
        )
        assert state["objective"] == "test"
        assert state["workflow_type"] == "disruption_rebooking"
        assert state["workflow_status"] == "Running"
        assert state["step_order"] == 0
        assert state["retry_count"] == 0
        assert state["messages"] == []
        assert state["candidate_routes"] == []
        assert state["requires_approval"] is False

    def test_initial_state_with_disruption_id(self):
        from agents.state import create_initial_state

        state = create_initial_state(
            objective="test",
            workflow_type="disruption_rebooking",
            disruption_case_id="abc-123",
            workflow_id="wf-001",
        )
        assert state["disruption_case_id"] == "abc-123"
        assert state["workflow_id"] == "wf-001"


class TestAgentNodes:
    """Tests that all agent node functions exist and are callable."""

    def test_planner_node_callable(self):
        from agents.planner import planner_node

        assert callable(planner_node)

    def test_journey_node_callable(self):
        from agents.journey_agent import journey_agent_node

        assert callable(journey_agent_node)

    def test_resource_node_callable(self):
        from agents.resource_agent import resource_agent_node

        assert callable(resource_agent_node)

    def test_booking_node_callable(self):
        from agents.booking_agent import booking_agent_node

        assert callable(booking_agent_node)

    def test_safety_node_callable(self):
        from agents.safety_agent import safety_agent_node

        assert callable(safety_agent_node)

    def test_all_agents_use_safe_failure(self):
        """Verify all agents are wrapped with safe failure guardrail."""
        import inspect
        from agents.planner import planner_node
        from agents.journey_agent import journey_agent_node
        from agents.resource_agent import resource_agent_node
        from agents.booking_agent import booking_agent_node
        from agents.safety_agent import safety_agent_node

        for name, fn in [
            ("planner", planner_node),
            ("journey", journey_agent_node),
            ("resource", resource_agent_node),
            ("booking", booking_agent_node),
            ("safety", safety_agent_node),
        ]:
            source = inspect.getsource(fn)
            assert "execute_with_safe_failure" in source, (
                f"{name} not wrapped with safe failure"
            )


class TestOutputValidators:
    """Tests for the deterministic output validators."""

    def test_impact_cancellation_overrides_low(self):
        from guardrails.output_validator import validate_impact_classification

        result = validate_impact_classification(
            "Low", 0, 0, is_cancellation=True
        )
        assert result == "High"

    def test_impact_delay_overrides_low(self):
        from guardrails.output_validator import validate_impact_classification

        result = validate_impact_classification(
            "Low", 0, 20, is_cancellation=False
        )
        assert result == "High"

    def test_impact_normal_low(self):
        from guardrails.output_validator import validate_impact_classification

        result = validate_impact_classification(
            "Low", 5, 10, is_cancellation=False
        )
        assert result == "Low"

    def test_uuid_valid(self):
        from guardrails.output_validator import validate_uuid

        valid, _ = validate_uuid("550e8400-e29b-41d4-a716-446655440000")
        assert valid is True

    def test_uuid_invalid(self):
        from guardrails.output_validator import validate_uuid

        valid, msg = validate_uuid("not-a-uuid")
        assert valid is False

    def test_seat_counts_valid(self):
        from guardrails.output_validator import validate_seat_counts

        valid, _ = validate_seat_counts(50, 30, 5, 15)
        assert valid is True

    def test_seat_counts_invalid(self):
        from guardrails.output_validator import validate_seat_counts

        valid, _ = validate_seat_counts(50, 30, 5, 10)
        assert valid is False

    def test_transfer_window_valid(self):
        from guardrails.output_validator import validate_transfer_window

        valid, mins, _ = validate_transfer_window(
            "2026-10-15T08:00:00", "2026-10-15T08:25:00"
        )
        assert valid is True
        assert mins == 25.0

    def test_transfer_window_invalid(self):
        from guardrails.output_validator import validate_transfer_window

        valid, mins, _ = validate_transfer_window(
            "2026-10-15T08:00:00", "2026-10-15T08:10:00"
        )
        assert valid is False
        assert mins == 10.0


class TestWorkflowLogger:
    """Tests for the persistence workflow logger."""

    def test_logger_instantiates(self):
        from persistence.workflow_logger import WorkflowLogger

        logger = WorkflowLogger()
        assert logger is not None

    def test_logger_has_required_methods(self):
        from persistence.workflow_logger import WorkflowLogger

        logger = WorkflowLogger()
        assert hasattr(logger, "create_workflow")
        assert hasattr(logger, "get_workflow")
        assert hasattr(logger, "update_status")
        assert hasattr(logger, "add_step")
        assert hasattr(logger, "add_tool_call")
        assert hasattr(logger, "add_validation")
        assert hasattr(logger, "log_full_workflow")


class TestMainApp:
    """Tests for the FastAPI application integration."""

    def test_app_imports(self):
        from main import app

        assert app is not None

    def test_endpoints_registered(self):
        from main import app

        routes = [r.path for r in app.routes]
        assert "/api/ai/health" in routes
        assert "/api/ai/disruption-rebooking" in routes
        assert "/api/ai/journey-recommendation" in routes
