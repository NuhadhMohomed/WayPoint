"""
WayPoint AI — Test Suite: Tool Registry (BR-AITOOL-001, BR-AITOOL-002).

Tests:
- Exactly 10 tools in the allow-list
- get_tool raises ToolNotAllowedError for unlisted tools
- Agent subsets have correct counts
- All tools have LangChain .name attribute
- DTO schema validation (Input models reject missing required fields)
"""

import sys
import pytest

sys.path.insert(0, ".")


class TestToolRegistry:
    """Tests for the allow-listed tool registry."""

    def test_allowed_tools_has_10_entries(self):
        from tools.registry import ALLOWED_TOOLS

        assert len(ALLOWED_TOOLS) == 10, (
            f"Expected 10 tools, got {len(ALLOWED_TOOLS)}"
        )

    def test_all_10_tool_names_match(self):
        from tools.registry import ALLOWED_TOOLS

        expected = sorted([
            "SearchRoutes",
            "GetBoardingPoints",
            "CheckTransferFeasibility",
            "CheckSeatAvailability",
            "CalculateFareDifference",
            "CreateRebookingProposal",
            "CalculatePassengerImpact",
            "RequestManagerApproval",
            "ApplyApprovedOperationalChange",
            "SendPassengerNotification",
        ])
        actual = sorted(ALLOWED_TOOLS.keys())
        assert actual == expected

    def test_get_tool_returns_langchain_tool(self):
        from tools.registry import get_tool

        tool = get_tool("SearchRoutes")
        assert hasattr(tool, "name")
        assert hasattr(tool, "invoke")

    def test_get_tool_raises_for_unlisted(self):
        from tools.registry import get_tool, ToolNotAllowedError

        with pytest.raises(ToolNotAllowedError):
            get_tool("FakeToolName")

    def test_is_allowed_true(self):
        from tools.registry import is_allowed

        assert is_allowed("SearchRoutes") is True

    def test_is_allowed_false(self):
        from tools.registry import is_allowed

        assert is_allowed("NotATool") is False

    def test_journey_agent_tools_count(self):
        from tools.registry import JOURNEY_AGENT_TOOLS

        assert len(JOURNEY_AGENT_TOOLS) == 3

    def test_resource_agent_tools_count(self):
        from tools.registry import RESOURCE_AGENT_TOOLS

        assert len(RESOURCE_AGENT_TOOLS) == 2

    def test_booking_agent_tools_count(self):
        from tools.registry import BOOKING_AGENT_TOOLS

        assert len(BOOKING_AGENT_TOOLS) == 2

    def test_safety_agent_tools_count(self):
        from tools.registry import SAFETY_AGENT_TOOLS

        assert len(SAFETY_AGENT_TOOLS) == 4

    def test_all_tools_have_langchain_name(self):
        from tools.registry import ALLOWED_TOOLS

        for name, tool_fn in ALLOWED_TOOLS.items():
            assert hasattr(tool_fn, "name"), (
                f"{name} missing LangChain .name attribute"
            )


class TestToolSchemaValidation:
    """Tests for Pydantic DTO validation (BR-AITOOL-002)."""

    def test_search_routes_input_validates(self):
        from schemas.tools import SearchRoutesInput

        si = SearchRoutesInput(
            origin_city="Colombo", destination_city="Ella"
        )
        assert si.origin_city == "Colombo"
        assert si.destination_city == "Ella"

    def test_search_routes_input_rejects_missing_fields(self):
        from schemas.tools import SearchRoutesInput
        from pydantic import ValidationError

        with pytest.raises(ValidationError):
            SearchRoutesInput()

    def test_check_seat_availability_input(self):
        from schemas.tools import CheckSeatAvailabilityInput

        ci = CheckSeatAvailabilityInput(
            service_id="550e8400-e29b-41d4-a716-446655440000"
        )
        assert ci.service_id == "550e8400-e29b-41d4-a716-446655440000"

    def test_check_seat_availability_rejects_empty(self):
        from schemas.tools import CheckSeatAvailabilityInput
        from pydantic import ValidationError

        with pytest.raises(ValidationError):
            CheckSeatAvailabilityInput()

    def test_workflow_request_validates(self):
        from schemas.workflow import WorkflowRequest

        wr = WorkflowRequest(objective="test")
        assert wr.objective == "test"

    def test_workflow_result_defaults(self):
        from schemas.workflow import WorkflowResult

        result = WorkflowResult()
        assert result.status == "Running"
        assert result.steps_completed == 0
        assert result.candidate_routes == []
