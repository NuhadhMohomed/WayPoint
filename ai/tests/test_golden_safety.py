"""
WayPoint AI — Golden Scenario Tests: Validation & Safety Agent (Student 4 / Dineth).

Golden Test Cases fulfilling Phase 6 requirements:
  - REQ-TEST-06: Golden scenario execution verification (Colombo–Ella Bus ND-8821 breakdown)
  - BR-APPROVAL-001: Human Transport Manager approval boundary
  - BR-DISRUPT-001: Impact classification (shift > 15 min or cancellation -> High)
  - BR-APPLY-001: Prohibition of direct automated execution of high-impact changes
  - BR-AITOOL-001: Allow-listed tool boundary (exactly 4 tools)
  - FR-AI-003: Deterministic output guardrails overriding LLM hallucination
  - FR-AI-004: Safe failure error trapping
  - FR-AI-005: Duration metrics & tool call tracking
  - FR-AI-008: Prompt injection sanitization and wrapping
  - ADR-004: Relational audit persistence structure matching Pydantic DTOs
"""

import json
import sys
from unittest.mock import AsyncMock, MagicMock, patch
import pytest

sys.path.insert(0, ".")

from schemas.workflow import (
    AgentStepRecord,
    ToolCallRecord,
    ValidationRecord,
)


# ============================================================================
# Golden Disruption State Fixtures
# ============================================================================

@pytest.fixture
def golden_high_impact_state():
    """Golden scenario state: Bus ND-8821 breakdown on Colombo-Ella line (EX-08)."""
    return {
        "workflow_id": "wf-golden-col-ella-01",
        "workflow_type": "disruption_rebooking",
        "disruption_case_id": "91532418-794f-46b0-89e3-1a1f8125be4c",
        "objective": "Emergency rebooking for broken down Bus ND-8821 near Kadawatha",
        "step_order": 4,
        "candidate_routes": [
            {
                "route_code": "RT-COL-ELLA",
                "original_service_code": "EX-08",
                "replacement_service_code": "EX-08-REPLACEMENT",
                "departure_time": "07:15 AM",
                "arrival_time": "12:20 PM",
                "delay_minutes": 45,
            }
        ],
        "feasibility_result": {
            "is_feasible": True,
            "bus_plate": "WP-CAD-4120",
            "bus_class": "Super Line Luxury Coach",
            "available_seats": 35,
            "required_seats": 28,
        },
        "fare_analysis": {
            "fare_difference": 0.0,
            "fare_status": "Fully Absorbed",
        },
        "steps": [
            {"agent_name": "PlannerAgent", "step_order": 1, "tool_calls": [], "validation_results": []},
            {"agent_name": "JourneyAgent", "step_order": 2, "tool_calls": [], "validation_results": []},
            {"agent_name": "ResourceAgent", "step_order": 3, "tool_calls": [], "validation_results": []},
            {"agent_name": "BookingAgent", "step_order": 4, "tool_calls": [], "validation_results": []},
        ],
    }


@pytest.fixture
def golden_low_impact_state():
    """Low impact golden state: Minor 10-minute timetable shift without cancellation."""
    return {
        "workflow_id": "wf-golden-low-impact-01",
        "workflow_type": "schedule_adjustment",
        "disruption_case_id": "dc-minor-delay-001",
        "objective": "Minor timetable shift of 10 minutes for morning Colombo commuter run",
        "step_order": 4,
        "candidate_routes": [
            {
                "route_code": "RT-COL-GAM",
                "original_service_code": "CM-01",
                "replacement_service_code": "CM-01-ADJ",
                "departure_time": "06:40 AM",
                "arrival_time": "07:35 AM",
                "delay_minutes": 10,
            }
        ],
        "feasibility_result": {
            "is_feasible": True,
            "bus_plate": "WP-NA-5510",
            "available_seats": 40,
            "required_seats": 25,
        },
        "fare_analysis": {
            "fare_difference": 0.0,
            "fare_status": "No Change",
        },
        "steps": [
            {"agent_name": "PlannerAgent", "step_order": 1, "tool_calls": [], "validation_results": []},
        ],
    }


# ============================================================================
# 1. Golden Scenario: High-Impact Disruption Case (BR-APPROVAL-001, REQ-TEST-06)
# ============================================================================

class TestGoldenHighImpactScenario:
    """Evaluates Safety Agent behavior on the authoritative Colombo-Ella breakdown."""

    def test_golden_high_impact_overrides_and_gates_approval(self, golden_high_impact_state):
        from guardrails.output_validator import validate_impact_classification

        # Given: 45 min delay in a disruption rebooking workflow
        delay_minutes = 45
        is_cancellation = True

        # Even if an LLM proposed "Low", the guardrail MUST enforce "High"
        classification = validate_impact_classification(
            llm_classification="Low",
            affected_passenger_count=28,
            delay_minutes=delay_minutes,
            is_cancellation=is_cancellation,
        )

        assert classification == "High"
        requires_approval = classification == "High"
        assert requires_approval is True
        workflow_status = "PendingManagerApproval" if requires_approval else "Completed"
        assert workflow_status == "PendingManagerApproval"

    def test_golden_step_record_structure_matches_adr004(self, golden_high_impact_state):
        """Proves the step output conforms to the Pydantic AgentStepRecord schema."""
        step_dict = {
            "agent_name": "ValidationSafetyAgent",
            "step_order": 5,
            "step_description": "Impact: High. Approval required: True (BR-APPROVAL-001).",
            "executed_at": "2026-09-26T22:30:00Z",
            "tool_calls": [
                {
                    "tool_name": "CalculatePassengerImpact",
                    "arguments_json": json.dumps({"disrupted_service_id": "srv-01"}),
                    "result_json": json.dumps({"affectedPassengerCount": 28}),
                    "duration_ms": 120,
                    "executed_at": "2026-09-26T22:30:01Z",
                },
                {
                    "tool_name": "RequestManagerApproval",
                    "arguments_json": json.dumps({"impact_classification": "High"}),
                    "result_json": json.dumps({"status": "PendingManagerApproval"}),
                    "duration_ms": 95,
                    "executed_at": "2026-09-26T22:30:02Z",
                },
            ],
            "validation_results": [
                {
                    "rule_name": "ImpactClassificationOverride",
                    "passed": True,
                    "validation_details": "Classification: High (LLM proposed 'Low'), Delay: 45m",
                },
                {
                    "rule_name": "HumanApprovalBoundary",
                    "passed": True,
                    "validation_details": "Requires manager sign-off: True (BR-APPROVAL-001)",
                },
            ],
        }

        # Validate with Pydantic model
        record = AgentStepRecord(**step_dict)
        assert record.agent_name == "ValidationSafetyAgent"
        assert record.step_order == 5
        assert len(record.tool_calls) == 2
        assert len(record.validation_results) == 2
        assert record.tool_calls[0].tool_name == "CalculatePassengerImpact"
        assert record.validation_results[1].rule_name == "HumanApprovalBoundary"

    @pytest.mark.asyncio
    async def test_golden_colombo_ella_end_to_end_node_execution(self, golden_high_impact_state):
        """
        Executes safety_agent_node with the Colombo–Ella Bus ND-8821 breakdown state.
        Simulates LLM proposing a 'Low' impact response.
        Proves deterministic override programmatically enforces High impact and gates approval.
        """
        from agents.safety_agent import safety_agent_node

        mock_tool_resp = MagicMock()
        mock_tool_resp.tool_calls = [
            {
                "id": "tc-golden-01",
                "name": "CalculatePassengerImpact",
                "args": {"disrupted_service_id": "91532418-794f-46b0-89e3-1a1f8125be4c"},
            }
        ]

        mock_final_resp = MagicMock()
        mock_final_resp.tool_calls = None
        mock_final_resp.content = json.dumps({
            "impact_classification": "Low",  # Hallucinated low impact
            "affected_passenger_count": 28,
            "total_delay_minutes": 45,
            "reasoning": "Minor vehicle replacement scheduled for morning departure.",
        })

        mock_llm = AsyncMock()
        mock_llm.ainvoke = AsyncMock(side_effect=[mock_tool_resp, mock_final_resp])
        mock_llm.bind_tools = MagicMock(return_value=mock_llm)

        mock_tool_record = {
            "tool_name": "CalculatePassengerImpact",
            "arguments_json": json.dumps({"disrupted_service_id": "91532418-794f-46b0-89e3-1a1f8125be4c"}),
            "result_json": json.dumps({"affectedPassengerCount": 28}),
            "duration_ms": 115,
        }

        with patch("agents.safety_agent.ChatGoogleGenerativeAI", return_value=mock_llm), \
             patch("agents.safety_agent._execute_tool_call", new_callable=AsyncMock) as mock_exec:

            mock_exec.return_value = (json.dumps({"affectedPassengerCount": 28}), mock_tool_record)
            result = await safety_agent_node(golden_high_impact_state)

            # Assert deterministic override of hallucinated "Low"
            assert result["impact_classification"] == "High"
            assert result["requires_approval"] is True
            assert result["workflow_status"] == "PendingManagerApproval"
            assert result["approval_status"] == "PendingManagerApproval"

            # Assert step record structure & ADR-004 compliance
            assert len(result["steps"]) == 5
            safety_step = result["steps"][-1]
            assert safety_step["agent_name"] == "ValidationSafetyAgent"
            assert safety_step["step_order"] == 5
            assert len(safety_step["tool_calls"]) == 1
            assert safety_step["tool_calls"][0]["tool_name"] == "CalculatePassengerImpact"
            assert safety_step["tool_calls"][0]["duration_ms"] == 115

            # Assert all 3 safety validations recorded
            rule_map = {v["rule_name"]: v for v in safety_step["validation_results"]}
            assert "ImpactClassificationOverride" in rule_map
            assert rule_map["ImpactClassificationOverride"]["passed"] is True
            assert "High" in rule_map["ImpactClassificationOverride"]["validation_details"]

            assert "HumanApprovalBoundary" in rule_map
            assert rule_map["HumanApprovalBoundary"]["passed"] is True
            assert "Requires manager sign-off: True" in rule_map["HumanApprovalBoundary"]["validation_details"]

            assert "OperationalSafetyBoundary" in rule_map
            assert rule_map["OperationalSafetyBoundary"]["passed"] is True

    @pytest.mark.asyncio
    async def test_golden_colombo_ella_trace_persistence(self, golden_high_impact_state):
        """Verifies that the golden scenario steps serialize and persist cleanly via WorkflowLogger."""
        from persistence.workflow_logger import WorkflowLogger

        logger = WorkflowLogger()

        with patch.object(logger, "create_workflow", new_callable=AsyncMock) as mock_create, \
             patch.object(logger, "add_step", new_callable=AsyncMock) as mock_step, \
             patch.object(logger, "add_tool_call", new_callable=AsyncMock) as mock_tc, \
             patch.object(logger, "add_validation", new_callable=AsyncMock) as mock_vr, \
             patch.object(logger, "update_status", new_callable=AsyncMock) as mock_status:

            mock_create.return_value = "wf-golden-col-ella-01"
            mock_step.return_value = "step-golden-05"
            mock_tc.return_value = "tc-golden-01"
            mock_vr.return_value = "vr-golden-01"
            mock_status.return_value = {"id": "wf-golden-col-ella-01", "status": 1}

            golden_steps = [
                {
                    "agent_name": "ValidationSafetyAgent",
                    "step_order": 5,
                    "step_description": "Impact: High. Approval required: True.",
                    "tool_calls": [
                        {
                            "tool_name": "CalculatePassengerImpact",
                            "arguments_json": '{"srv":"srv-01"}',
                            "result_json": '{"affected":28}',
                            "duration_ms": 115,
                        }
                    ],
                    "validation_results": [
                        {
                            "rule_name": "ImpactClassificationOverride",
                            "passed": True,
                            "validation_details": "High override",
                        }
                    ],
                }
            ]

            wf_id = await logger.log_full_workflow(
                objective=golden_high_impact_state["objective"],
                steps=golden_steps,
                final_status="PendingManagerApproval",
            )

            assert wf_id == "wf-golden-col-ella-01"
            mock_create.assert_called_once_with(golden_high_impact_state["objective"])
            mock_step.assert_called_once()
            mock_tc.assert_called_once()
            mock_vr.assert_called_once()
            mock_status.assert_called_once_with("wf-golden-col-ella-01", "PendingManagerApproval")


# ============================================================================
# 2. Golden Scenario: Low-Impact Case (Schedule Shift <= 15 min, No Cancellation)
# ============================================================================

class TestGoldenLowImpactScenario:
    """Evaluates Safety Agent behavior on a minor 10-minute departure delay."""

    def test_golden_low_impact_allows_automated_execution(self):
        from guardrails.output_validator import validate_impact_classification

        # Given: Minor 10-minute delay, NOT a cancellation
        delay_minutes = 10
        is_cancellation = False

        classification = validate_impact_classification(
            llm_classification="Low",
            affected_passenger_count=15,
            delay_minutes=delay_minutes,
            is_cancellation=is_cancellation,
        )

        assert classification == "Low"
        requires_approval = classification == "High"
        assert requires_approval is False

        workflow_status = "PendingManagerApproval" if requires_approval else "Completed"
        assert workflow_status == "Completed"

    @pytest.mark.asyncio
    async def test_golden_low_impact_end_to_end_node_execution(self, golden_low_impact_state):
        """Proves low-impact schedule adjustment completes with automated execution permitted."""
        from agents.safety_agent import safety_agent_node

        mock_final_resp = MagicMock()
        mock_final_resp.tool_calls = None
        mock_final_resp.content = json.dumps({
            "impact_classification": "Low",
            "affected_passenger_count": 15,
            "total_delay_minutes": 10,
        })

        mock_llm = AsyncMock()
        mock_llm.ainvoke = AsyncMock(return_value=mock_final_resp)
        mock_llm.bind_tools = MagicMock(return_value=mock_llm)

        with patch("agents.safety_agent.ChatGoogleGenerativeAI", return_value=mock_llm):
            result = await safety_agent_node(golden_low_impact_state)

            assert result["impact_classification"] == "Low"
            assert result["requires_approval"] is False
            assert result["workflow_status"] == "Completed"
            assert result["approval_status"] == ""


# ============================================================================
# 3. Boundary Condition Parameterization (Table 4.2 & BR-DISRUPT-001)
# ============================================================================

class TestGoldenBoundaryConditions:
    """Tests edge conditions around the 15-minute timetable shift boundary."""

    @pytest.mark.parametrize(
        "delay_minutes,expected_impact,expected_approval",
        [
            (0, "Low", False),
            (14, "Low", False),
            (15, "Low", False),   # Exact boundary (shift <= 15 min)
            (16, "High", True),   # First minute exceeding threshold
            (20, "High", True),
            (60, "High", True),
        ],
    )
    def test_timetable_shift_boundary_enforcement(
        self, delay_minutes, expected_impact, expected_approval
    ):
        from guardrails.output_validator import validate_impact_classification

        classification = validate_impact_classification(
            llm_classification="Low",
            affected_passenger_count=10,
            delay_minutes=delay_minutes,
            is_cancellation=False,
        )

        assert classification == expected_impact
        requires_approval = classification == "High"
        assert requires_approval is expected_approval

    @pytest.mark.parametrize(
        "shift_minutes,is_cancellation,expected_impact,expected_approval,expected_executable",
        [
            (0, False, "Low", False, True),
            (14, False, "Low", False, True),
            (15, False, "Low", False, True),  # Exact Boundary
            (16, False, "High", True, False), # Boundary Breach (>15 min)
            (20, False, "High", True, False),
            (45, False, "High", True, False), # Golden Scenario Breakdown
            (0, True, "High", True, False),   # Cancellation Override
        ],
    )
    def test_timetable_shift_boundary_matrix_table_4_2(
        self, shift_minutes, is_cancellation, expected_impact, expected_approval, expected_executable
    ):
        """
        Verifies the authoritative boundary condition matrix from Section 4.2 of Implementation Plan.
        """
        from guardrails.output_validator import validate_impact_classification

        classification = validate_impact_classification(
            llm_classification="Low",
            affected_passenger_count=20,
            delay_minutes=shift_minutes,
            is_cancellation=is_cancellation,
        )

        assert classification == expected_impact
        requires_approval = classification == "High"
        assert requires_approval is expected_approval

        auto_executable = not requires_approval
        assert auto_executable is expected_executable


# ============================================================================
# 4. Operational Safety & Approval Boundary Constraints (BR-APPLY-001, BR-AITOOL-001)
# ============================================================================

class TestGoldenSafetyOperationalBoundaries:
    """Proves prohibition of direct automated execution by the AI agent."""

    def test_safety_agent_tools_have_no_direct_execution_capabilities(self):
        """Asserts Safety Agent is bound only to advisory and proposal tools (no database mutations)."""
        from tools.registry import SAFETY_AGENT_TOOLS

        tool_names = [t.name for t in SAFETY_AGENT_TOOLS]
        assert len(tool_names) == 4

        # Allow-listed advisory and proposal tools (BR-AITOOL-001)
        assert "create_rebooking_proposal" in tool_names
        assert "calculate_passenger_impact" in tool_names
        assert "request_manager_approval" in tool_names
        assert "apply_approved_operational_change" in tool_names

        # Strict prohibition: no direct unapproved execution tools exist (BR-APPLY-001)
        disallowed_actions = [
            "direct_database_update",
            "cancel_service_unapproved",
            "refund_payment_direct",
            "reissue_tickets_unapproved",
            "execute_without_approval",
        ]
        for disallowed in disallowed_actions:
            assert disallowed not in tool_names, f"Disallowed execution tool '{disallowed}' bound to agent!"

    def test_high_impact_proposal_cannot_auto_execute(self, golden_high_impact_state):
        """Proves high-impact remedy is blocked from automated execution."""
        from guardrails.output_validator import validate_impact_classification

        impact = validate_impact_classification(
            llm_classification="High",
            affected_passenger_count=28,
            delay_minutes=45,
            is_cancellation=True,
        )
        assert impact == "High"
        requires_approval = impact == "High"
        workflow_status = "PendingManagerApproval" if requires_approval else "Completed"

        # Automated execution is prohibited
        assert workflow_status != "Completed"
        assert workflow_status == "PendingManagerApproval"


# ============================================================================
# 5. Safe Failure Resilience (FR-AI-004)
# ============================================================================

class TestGoldenSafeFailureResilience:
    """Verifies safe failure error wrapping in golden disruption scenarios."""

    @pytest.mark.asyncio
    async def test_golden_scenario_handles_llm_failure_safely(self, golden_high_impact_state):
        """If LLM crashes at max retries during the golden scenario, state transitions to SafeFailure gracefully."""
        from agents.safety_agent import safety_agent_node

        # Set retry_count to 2 so next failure hits MAX_RETRIES (3) -> terminal SafeFailure
        golden_high_impact_state["retry_count"] = 2

        mock_llm = AsyncMock()
        mock_llm.ainvoke = AsyncMock(side_effect=RuntimeError("Google Gemini API quota exceeded"))
        mock_llm.bind_tools = MagicMock(return_value=mock_llm)

        with patch("agents.safety_agent.ChatGoogleGenerativeAI", return_value=mock_llm):
            result = await safety_agent_node(golden_high_impact_state)

            assert result["workflow_status"] == "SafeFailure"
            assert "error" in result
            assert "quota exceeded" in result["error"]

            # SafeFailure step recorded
            assert len(result["steps"]) > 0
            last_step = result["steps"][-1]
            assert last_step["agent_name"] == "ValidationSafetyAgent"
            rule_names = [v["rule_name"] for v in last_step["validation_results"]]
            assert "SafeFailureGuardrail" in rule_names
