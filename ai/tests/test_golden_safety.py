"""
WayPoint AI — Golden Scenario Tests: Validation & Safety Agent (Student 4 / Dineth).

Golden Test Cases fulfilling:
  - REQ-TEST-06: Golden scenario execution verification
  - BR-APPROVAL-001: Human Transport Manager approval boundary
  - BR-DISRUPT-001: Impact classification (shift > 15 min or cancellation -> High)
  - BR-APPLY-001: Prohibition of direct automated execution of high-impact changes
  - FR-AI-003: Deterministic output guardrails overriding LLM hallucination
  - ADR-004: Relational audit persistence structure matching Pydantic DTOs
"""

import json
import sys
import pytest

sys.path.insert(0, ".")

from schemas.workflow import (
    AgentStepRecord,
    ToolCallRecord,
    ValidationRecord,
)


# ============================================================================
# Golden Disruption State Fixture
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


# ============================================================================
# 1. Golden Scenario: High-Impact Disruption Case (BR-APPROVAL-001)
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


# ============================================================================
# 2. Golden Scenario: Low-Impact Disruption Case (Schedule Shift <= 15 min)
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


# ============================================================================
# 3. Boundary Condition Tests: Exactly 15 Minutes
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
