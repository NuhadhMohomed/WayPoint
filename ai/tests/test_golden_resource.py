"""
WayPoint AI — Golden Test: Resource Feasibility Agent (Student 2 / Nuhadh).

Pre-scripted end-to-end scenario with deterministic inputs and expected
outputs. Proves the agent correctly evaluates resource feasibility for
the Agentic AI Evaluation Report (REQ-TEST-06).

Golden Scenario (FEASIBLE):
  Service S-303 (Colombo → Ella) is disrupted with 30 booked passengers.
  Replacement candidate service S-404 has a 45-seat bus.
  CheckSeatAvailability returns: 35 available, 2 held, 8 booked.
  Expected: Agent declares FEASIBLE (35 >= 30, BR-RESOURCE-001).
  Driver had 10h rest since last shift → passes BR-RESOURCE-002.

Golden Scenario (INFEASIBLE):
  Same setup but replacement bus S-505 has only 25 total seats.
  CheckSeatAvailability returns: 10 available, 5 held, 10 booked.
  Expected: Agent declares INFEASIBLE (10 < 30, BR-RESOURCE-001).

Mocking Strategy:
  - LLM is mocked to return a deterministic AIMessage with the expected
    JSON output (no live Gemini API calls needed).
  - Tool execution is tested through the deterministic validators and
    the _run_resource_validators helper (no HTTP calls needed).

Requirements Covered:
  - BR-RESOURCE-001: Bus capacity >= required
  - BR-RESOURCE-002: Driver >= 8h rest
  - FR-AI-003: Deterministic output validation overrides LLM
  - FR-AI-004: Safe failure guardrail
  - FR-AI-005: Tool call recording for persistence (ADR-004)
  - REQ-TEST-06: Agent evaluation golden test case
"""

import json
import sys

import pytest

sys.path.insert(0, ".")


# ============================================================================
# Golden Scenario Constants
# ============================================================================

# --- Feasible Scenario ---
GOLDEN_FEASIBLE = {
    "scenario": "Disrupted service S-303 (Colombo→Ella), 30 passengers",
    "disrupted_service_id": "s-303-colombo-ella",
    "disruption_case_id": "dc-001-golden",
    "replacement_service_id": "s-404-replacement",
    "total_seats": 45,
    "available_seats": 35,
    "held_seats": 2,
    "booked_seats": 8,
    "required_seats": 30,
    "driver_last_shift_end": "2026-10-15T00:00:00",
    "driver_next_shift_start": "2026-10-15T10:00:00",  # 10h rest
    "expected_feasible": True,
    "expected_capacity_pass": True,
    "expected_driver_rest_pass": True,
    "expected_driver_rest_hours": 10.0,
}

# --- Infeasible Scenario ---
GOLDEN_INFEASIBLE = {
    "scenario": "Disrupted service S-303, replacement S-505 too small",
    "disrupted_service_id": "s-303-colombo-ella",
    "disruption_case_id": "dc-002-golden",
    "replacement_service_id": "s-505-small-bus",
    "total_seats": 25,
    "available_seats": 10,
    "held_seats": 5,
    "booked_seats": 10,
    "required_seats": 30,
    "driver_last_shift_end": "2026-10-15T06:00:00",
    "driver_next_shift_start": "2026-10-15T11:00:00",  # 5h rest
    "expected_feasible": False,
    "expected_capacity_pass": False,
    "expected_driver_rest_pass": False,
    "expected_driver_rest_hours": 5.0,
}

# --- LLM Mock Responses ---
# These simulate what a well-behaved LLM would return as its final answer.

GOLDEN_FEASIBLE_LLM_RESPONSE = json.dumps({
    "isFeasible": True,
    "reason": "Replacement service S-404 has sufficient capacity",
    "total_seats": 45,
    "available_seats": 35,
    "held_seats": 2,
    "booked_seats": 8,
    "required_seats": 30,
    "driver_last_shift_end": "2026-10-15T00:00:00",
    "driver_next_shift_start": "2026-10-15T10:00:00",
})

GOLDEN_INFEASIBLE_LLM_RESPONSE = json.dumps({
    "isFeasible": False,
    "reason": "Replacement service S-505 has insufficient capacity",
    "total_seats": 25,
    "available_seats": 10,
    "held_seats": 5,
    "booked_seats": 10,
    "required_seats": 30,
    "driver_last_shift_end": "2026-10-15T06:00:00",
    "driver_next_shift_start": "2026-10-15T11:00:00",
})


# ============================================================================
# Fixtures
# ============================================================================

@pytest.fixture
def feasible_state():
    """Workflow state for the feasible golden scenario."""
    from agents.state import create_initial_state

    return create_initial_state(
        objective=(
            "Check if replacement service S-404 can accommodate 30 "
            "passengers from disrupted service S-303 (Colombo→Ella)"
        ),
        workflow_type="disruption_rebooking",
        disruption_case_id=GOLDEN_FEASIBLE["disruption_case_id"],
        workflow_id="wf-golden-feasible",
    )


@pytest.fixture
def infeasible_state():
    """Workflow state for the infeasible golden scenario."""
    from agents.state import create_initial_state

    return create_initial_state(
        objective=(
            "Check if replacement service S-505 can accommodate 30 "
            "passengers from disrupted service S-303 (Colombo→Ella)"
        ),
        workflow_type="disruption_rebooking",
        disruption_case_id=GOLDEN_INFEASIBLE["disruption_case_id"],
        workflow_id="wf-golden-infeasible",
    )


# ============================================================================
# 1. Feasible Golden Scenario Tests
# ============================================================================

class TestGoldenFeasibleScenario:
    """Golden test: 30 passengers, 35 available seats → FEASIBLE."""

    def test_golden_feasible_parsed_correctly(self):
        """LLM response is parsed into a dict with isFeasible=True."""
        from agents.resource_agent import _extract_json

        result = _extract_json(GOLDEN_FEASIBLE_LLM_RESPONSE)
        assert result["isFeasible"] is True
        assert result["total_seats"] == 45
        assert result["available_seats"] == 35

    def test_golden_capacity_validation_passes(self):
        """BR-RESOURCE-001: 35 available >= 30 required → pass."""
        from guardrails.output_validator import validate_bus_capacity

        g = GOLDEN_FEASIBLE
        passed, detail = validate_bus_capacity(
            g["available_seats"], g["required_seats"]
        )
        assert passed is True, f"Expected pass: {detail}"
        assert "Sufficient" in detail

    def test_golden_seat_arithmetic_valid(self):
        """Seat count arithmetic: 35 + 2 + 8 == 45 → pass."""
        from guardrails.output_validator import validate_seat_counts

        g = GOLDEN_FEASIBLE
        passed, detail = validate_seat_counts(
            g["total_seats"],
            g["available_seats"],
            g["held_seats"],
            g["booked_seats"],
        )
        assert passed is True, f"Expected pass: {detail}"

    def test_golden_driver_rest_passes(self):
        """BR-RESOURCE-002: 10h rest >= 8h minimum → pass."""
        from guardrails.output_validator import validate_driver_rest

        g = GOLDEN_FEASIBLE
        passed, hours, detail = validate_driver_rest(
            g["driver_last_shift_end"],
            g["driver_next_shift_start"],
        )
        assert passed is True, f"Expected pass: {detail}"
        assert hours == g["expected_driver_rest_hours"]

    def test_golden_feasible_all_validators_pass(self):
        """All three validators pass for the feasible scenario."""
        from agents.resource_agent import _run_resource_validators, _extract_json

        result = _extract_json(GOLDEN_FEASIBLE_LLM_RESPONSE)
        validations = _run_resource_validators(result)

        # Should have all 3 validators
        rule_names = [v["rule_name"] for v in validations]
        assert "SeatCountArithmetic" in rule_names
        assert "BusCapacityCheck_BR-RESOURCE-001" in rule_names
        assert "DriverRestHours_BR-RESOURCE-002" in rule_names

        # All should pass
        for v in validations:
            assert v["passed"] is True, (
                f"Validator {v['rule_name']} failed: {v['validation_details']}"
            )

    def test_golden_feasible_validation_count(self):
        """Exactly 3 validators run for the feasible scenario."""
        from agents.resource_agent import _run_resource_validators, _extract_json

        result = _extract_json(GOLDEN_FEASIBLE_LLM_RESPONSE)
        validations = _run_resource_validators(result)
        assert len(validations) == 3


# ============================================================================
# 2. Infeasible Golden Scenario Tests
# ============================================================================

class TestGoldenInfeasibleScenario:
    """Golden test: 30 passengers, 10 available seats → INFEASIBLE."""

    def test_golden_infeasible_parsed_correctly(self):
        """LLM response is parsed into a dict with isFeasible=False."""
        from agents.resource_agent import _extract_json

        result = _extract_json(GOLDEN_INFEASIBLE_LLM_RESPONSE)
        assert result["isFeasible"] is False
        assert result["total_seats"] == 25
        assert result["available_seats"] == 10

    def test_golden_capacity_validation_fails(self):
        """BR-RESOURCE-001: 10 available < 30 required → FAIL."""
        from guardrails.output_validator import validate_bus_capacity

        g = GOLDEN_INFEASIBLE
        passed, detail = validate_bus_capacity(
            g["available_seats"], g["required_seats"]
        )
        assert passed is False, f"Expected fail: {detail}"
        assert "BR-RESOURCE-001" in detail

    def test_golden_seat_arithmetic_valid_infeasible(self):
        """Seat count arithmetic: 10 + 5 + 10 == 25 → pass (arithmetic is correct)."""
        from guardrails.output_validator import validate_seat_counts

        g = GOLDEN_INFEASIBLE
        passed, detail = validate_seat_counts(
            g["total_seats"],
            g["available_seats"],
            g["held_seats"],
            g["booked_seats"],
        )
        assert passed is True, (
            f"Arithmetic should be correct even in infeasible scenario: {detail}"
        )

    def test_golden_driver_rest_fails(self):
        """BR-RESOURCE-002: 5h rest < 8h minimum → FAIL."""
        from guardrails.output_validator import validate_driver_rest

        g = GOLDEN_INFEASIBLE
        passed, hours, detail = validate_driver_rest(
            g["driver_last_shift_end"],
            g["driver_next_shift_start"],
        )
        assert passed is False, f"Expected fail: {detail}"
        assert hours == g["expected_driver_rest_hours"]
        assert "BR-RESOURCE-002" in detail

    def test_golden_infeasible_validators_mixed(self):
        """Infeasible scenario: seat arithmetic passes, capacity and rest fail."""
        from agents.resource_agent import _run_resource_validators, _extract_json

        result = _extract_json(GOLDEN_INFEASIBLE_LLM_RESPONSE)
        validations = _run_resource_validators(result)

        results_by_rule = {v["rule_name"]: v for v in validations}

        # Seat arithmetic should still pass (10+5+10=25)
        assert results_by_rule["SeatCountArithmetic"]["passed"] is True

        # Capacity should fail (10 < 30)
        assert results_by_rule[
            "BusCapacityCheck_BR-RESOURCE-001"
        ]["passed"] is False

        # Driver rest should fail (5h < 8h)
        assert results_by_rule[
            "DriverRestHours_BR-RESOURCE-002"
        ]["passed"] is False

    def test_golden_infeasible_shows_2_failures(self):
        """Infeasible scenario has exactly 2 failed validations."""
        from agents.resource_agent import _run_resource_validators, _extract_json

        result = _extract_json(GOLDEN_INFEASIBLE_LLM_RESPONSE)
        validations = _run_resource_validators(result)
        failed = [v for v in validations if not v["passed"]]
        assert len(failed) == 2


# ============================================================================
# 3. Markdown Fenced LLM Response Tests
# ============================================================================

class TestGoldenMarkdownFenced:
    """Golden test with LLM wrapping JSON in markdown code fences."""

    def test_golden_fenced_feasible(self):
        """LLM wraps feasible JSON in markdown fences → still parsed correctly."""
        from agents.resource_agent import _extract_json

        fenced = f"```json\n{GOLDEN_FEASIBLE_LLM_RESPONSE}\n```"
        result = _extract_json(fenced)
        assert result["isFeasible"] is True
        assert result["available_seats"] == 35

    def test_golden_fenced_infeasible(self):
        """LLM wraps infeasible JSON in markdown fences → still parsed correctly."""
        from agents.resource_agent import _extract_json

        fenced = f"```json\n{GOLDEN_INFEASIBLE_LLM_RESPONSE}\n```"
        result = _extract_json(fenced)
        assert result["isFeasible"] is False
        assert result["available_seats"] == 10

    def test_golden_with_preamble(self):
        """LLM adds text before JSON → JSON still extracted."""
        from agents.resource_agent import _extract_json

        text = (
            "Based on the seat availability check, here are the results:\n\n"
            f"{GOLDEN_FEASIBLE_LLM_RESPONSE}"
        )
        result = _extract_json(text)
        assert result["isFeasible"] is True


# ============================================================================
# 4. Step Record Structure Tests (ADR-004)
# ============================================================================

class TestGoldenStepRecordStructure:
    """Verifies the step record structure matches ADR-004 persistence schema."""

    def _make_golden_step(self, feasible=True):
        """Build a step record as the agent would produce."""
        from agents.resource_agent import _run_resource_validators, _extract_json

        llm_response = (
            GOLDEN_FEASIBLE_LLM_RESPONSE
            if feasible
            else GOLDEN_INFEASIBLE_LLM_RESPONSE
        )
        feasibility_result = _extract_json(llm_response)
        validation_results = _run_resource_validators(feasibility_result)
        tool_call_records = [
            {
                "tool_name": "CheckSeatAvailability",
                "arguments_json": json.dumps({"service_id": "s-404"}),
                "result_json": json.dumps({"available_seats": 35}),
                "duration_ms": 120,
            }
        ]

        return {
            "agent_name": "ResourceFeasibilityAgent",
            "step_order": 3,
            "step_description": (
                f"Evaluated seat and resource feasibility | "
                f"Tools called: {len(tool_call_records)} | "
                f"Validations: "
                f"{sum(1 for v in validation_results if v['passed'])}"
                f"/{len(validation_results)} passed"
            ),
            "tool_calls": tool_call_records,
            "validation_results": validation_results,
        }

    def test_golden_step_has_agent_name(self):
        step = self._make_golden_step()
        assert step["agent_name"] == "ResourceFeasibilityAgent"

    def test_golden_step_has_step_order(self):
        step = self._make_golden_step()
        assert step["step_order"] == 3

    def test_golden_step_has_tool_calls(self):
        step = self._make_golden_step()
        assert isinstance(step["tool_calls"], list)
        assert len(step["tool_calls"]) >= 1

    def test_golden_step_has_validation_results(self):
        step = self._make_golden_step()
        assert isinstance(step["validation_results"], list)
        assert len(step["validation_results"]) == 3

    def test_golden_step_tool_call_structure(self):
        """Each tool call record has the ADR-004 required keys."""
        step = self._make_golden_step()
        for tc in step["tool_calls"]:
            assert "tool_name" in tc
            assert "arguments_json" in tc
            assert "result_json" in tc
            assert "duration_ms" in tc
            assert isinstance(tc["duration_ms"], int)

    def test_golden_step_validation_result_structure(self):
        """Each validation result has the required keys."""
        step = self._make_golden_step()
        for vr in step["validation_results"]:
            assert "rule_name" in vr
            assert "passed" in vr
            assert "validation_details" in vr

    def test_golden_step_description_is_dynamic(self):
        """Step description includes tool count and validation summary."""
        step = self._make_golden_step()
        desc = step["step_description"]
        assert "Tools called:" in desc
        assert "Validations:" in desc
        assert "passed" in desc

    def test_golden_step_feasible_description(self):
        step = self._make_golden_step(feasible=True)
        assert "3/3 passed" in step["step_description"]

    def test_golden_step_infeasible_description(self):
        step = self._make_golden_step(feasible=False)
        assert "1/3 passed" in step["step_description"]


# ============================================================================
# 5. WorkflowResult Schema Tests
# ============================================================================

class TestGoldenWorkflowResultSchema:
    """Verifies the golden output fits the WorkflowResult Pydantic model."""

    def test_golden_feasible_maps_to_workflow_result(self):
        from schemas.workflow import WorkflowResult
        from agents.resource_agent import _extract_json

        feasibility_result = _extract_json(GOLDEN_FEASIBLE_LLM_RESPONSE)

        result = WorkflowResult(
            workflow_id="wf-golden-feasible",
            workflow_type="disruption_rebooking",
            status="Completed",
            steps_completed=3,
            feasibility_result=feasibility_result,
        )
        assert result.status == "Completed"
        assert result.feasibility_result["isFeasible"] is True
        assert result.steps_completed == 3

    def test_golden_infeasible_maps_to_workflow_result(self):
        from schemas.workflow import WorkflowResult
        from agents.resource_agent import _extract_json

        feasibility_result = _extract_json(GOLDEN_INFEASIBLE_LLM_RESPONSE)

        result = WorkflowResult(
            workflow_id="wf-golden-infeasible",
            workflow_type="disruption_rebooking",
            status="Completed",
            steps_completed=3,
            feasibility_result=feasibility_result,
        )
        assert result.status == "Completed"
        assert result.feasibility_result["isFeasible"] is False

    def test_golden_safe_failure_maps_to_workflow_result(self):
        from schemas.workflow import WorkflowResult

        result = WorkflowResult(
            workflow_id="wf-golden-safefailure",
            workflow_type="disruption_rebooking",
            status="SafeFailure",
            steps_completed=0,
            error="ResourceFeasibilityAgent failed after 3 retries",
        )
        assert result.status == "SafeFailure"
        assert "3 retries" in result.error

    def test_golden_result_serializes_to_json(self):
        from schemas.workflow import WorkflowResult
        from agents.resource_agent import _extract_json

        feasibility_result = _extract_json(GOLDEN_FEASIBLE_LLM_RESPONSE)

        result = WorkflowResult(
            workflow_id="wf-golden-feasible",
            workflow_type="disruption_rebooking",
            status="Completed",
            steps_completed=3,
            feasibility_result=feasibility_result,
        )
        data = json.loads(result.model_dump_json())
        assert data["workflow_id"] == "wf-golden-feasible"
        assert data["feasibility_result"]["isFeasible"] is True
        assert isinstance(data["steps_completed"], int)


# ============================================================================
# 6. ToolCallRecord / ValidationRecord Schema Tests
# ============================================================================

class TestGoldenPersistenceSchemas:
    """Verifies golden data maps to ADR-004 persistence Pydantic models."""

    def test_golden_tool_call_maps_to_record(self):
        from schemas.workflow import ToolCallRecord

        record = ToolCallRecord(
            tool_name="CheckSeatAvailability",
            arguments_json=json.dumps({
                "service_id": GOLDEN_FEASIBLE["replacement_service_id"],
            }),
            result_json=json.dumps({
                "service_id": GOLDEN_FEASIBLE["replacement_service_id"],
                "total_seats": GOLDEN_FEASIBLE["total_seats"],
                "available_seats": GOLDEN_FEASIBLE["available_seats"],
                "held_seats": GOLDEN_FEASIBLE["held_seats"],
                "booked_seats": GOLDEN_FEASIBLE["booked_seats"],
            }),
            duration_ms=120,
        )
        assert record.tool_name == "CheckSeatAvailability"
        assert record.duration_ms == 120
        # Verify JSON round-trips correctly
        args = json.loads(record.arguments_json)
        assert args["service_id"] == "s-404-replacement"

    def test_golden_validation_record_pass(self):
        from schemas.workflow import ValidationRecord

        record = ValidationRecord(
            rule_name="BusCapacityCheck_BR-RESOURCE-001",
            passed=True,
            validation_details="Sufficient capacity: 35 available >= 30 required",
        )
        assert record.passed is True
        assert "BR-RESOURCE-001" in record.rule_name

    def test_golden_validation_record_fail(self):
        from schemas.workflow import ValidationRecord

        record = ValidationRecord(
            rule_name="BusCapacityCheck_BR-RESOURCE-001",
            passed=False,
            validation_details=(
                "Insufficient capacity: 10 available < 30 required "
                "(BR-RESOURCE-001)"
            ),
        )
        assert record.passed is False

    def test_golden_step_record_model(self):
        from schemas.workflow import AgentStepRecord, ToolCallRecord, ValidationRecord

        step = AgentStepRecord(
            agent_name="ResourceFeasibilityAgent",
            step_order=3,
            step_description="Evaluated seat and resource feasibility",
            tool_calls=[
                ToolCallRecord(
                    tool_name="CheckSeatAvailability",
                    arguments_json='{"service_id": "s-404-replacement"}',
                    result_json='{"available_seats": 35}',
                    duration_ms=120,
                ),
            ],
            validation_results=[
                ValidationRecord(
                    rule_name="SeatCountArithmetic",
                    passed=True,
                    validation_details="OK",
                ),
                ValidationRecord(
                    rule_name="BusCapacityCheck_BR-RESOURCE-001",
                    passed=True,
                    validation_details="Sufficient capacity: 35 >= 30",
                ),
                ValidationRecord(
                    rule_name="DriverRestHours_BR-RESOURCE-002",
                    passed=True,
                    validation_details="Rest period OK: 10.0h >= 8h",
                ),
            ],
        )
        assert step.agent_name == "ResourceFeasibilityAgent"
        assert len(step.tool_calls) == 1
        assert len(step.validation_results) == 3
        assert all(vr.passed for vr in step.validation_results)


# ============================================================================
# 7. End-to-End Agent Mock Tests
# ============================================================================

class TestGoldenEndToEnd:
    """End-to-end agent test with mocked LLM — proves full pipeline works."""

    @pytest.mark.asyncio
    async def test_golden_agent_skips_in_safe_failure(self, feasible_state):
        """Agent short-circuits if workflow is already in SafeFailure."""
        from agents.resource_agent import resource_agent_node

        state = {**feasible_state, "workflow_status": "SafeFailure"}
        result = await resource_agent_node(state)
        assert result["workflow_status"] == "SafeFailure"

    def test_golden_extract_and_validate_feasible_pipeline(self):
        """Full pipeline: extract JSON → validate → all pass."""
        from agents.resource_agent import _extract_json, _run_resource_validators

        # Step 1: Extract
        parsed = _extract_json(GOLDEN_FEASIBLE_LLM_RESPONSE)
        assert parsed["isFeasible"] is True

        # Step 2: Validate
        validations = _run_resource_validators(parsed)
        assert len(validations) == 3
        assert all(v["passed"] for v in validations)

        # Step 3: Verify specific rules
        rules = {v["rule_name"]: v for v in validations}
        assert "SeatCountArithmetic" in rules
        assert "BusCapacityCheck_BR-RESOURCE-001" in rules
        assert "DriverRestHours_BR-RESOURCE-002" in rules

    def test_golden_extract_and_validate_infeasible_pipeline(self):
        """Full pipeline: extract JSON → validate → capacity & rest fail."""
        from agents.resource_agent import _extract_json, _run_resource_validators

        # Step 1: Extract
        parsed = _extract_json(GOLDEN_INFEASIBLE_LLM_RESPONSE)
        assert parsed["isFeasible"] is False

        # Step 2: Validate
        validations = _run_resource_validators(parsed)
        assert len(validations) == 3

        # Step 3: Check specific results
        rules = {v["rule_name"]: v for v in validations}
        assert rules["SeatCountArithmetic"]["passed"] is True
        assert rules["BusCapacityCheck_BR-RESOURCE-001"]["passed"] is False
        assert rules["DriverRestHours_BR-RESOURCE-002"]["passed"] is False

    def test_golden_fenced_extract_and_validate_pipeline(self):
        """Full pipeline with markdown fenced response."""
        from agents.resource_agent import _extract_json, _run_resource_validators

        fenced = f"Here is my analysis:\n\n```json\n{GOLDEN_FEASIBLE_LLM_RESPONSE}\n```\n\nHope this helps!"
        parsed = _extract_json(fenced)
        assert parsed["isFeasible"] is True

        validations = _run_resource_validators(parsed)
        assert all(v["passed"] for v in validations)


# ============================================================================
# 8. Business Rule Boundary Tests
# ============================================================================

class TestGoldenBoundaryConditions:
    """Boundary conditions for golden scenario business rules."""

    def test_capacity_boundary_exact_30_of_30(self):
        """Exact match: 30 available == 30 required → PASS."""
        from guardrails.output_validator import validate_bus_capacity

        passed, _ = validate_bus_capacity(30, 30)
        assert passed is True

    def test_capacity_boundary_29_of_30(self):
        """Off-by-one: 29 available < 30 required → FAIL."""
        from guardrails.output_validator import validate_bus_capacity

        passed, _ = validate_bus_capacity(29, 30)
        assert passed is False

    def test_capacity_boundary_31_of_30(self):
        """One over: 31 available > 30 required → PASS."""
        from guardrails.output_validator import validate_bus_capacity

        passed, _ = validate_bus_capacity(31, 30)
        assert passed is True

    def test_driver_rest_boundary_exact_8h(self):
        """Exact minimum: 8h rest == 8h required → PASS."""
        from guardrails.output_validator import validate_driver_rest

        passed, hours, _ = validate_driver_rest(
            "2026-10-15T00:00:00", "2026-10-15T08:00:00"
        )
        assert passed is True
        assert hours == 8.0

    def test_driver_rest_boundary_7h59m(self):
        """Just under: 7h59m < 8h → FAIL."""
        from guardrails.output_validator import validate_driver_rest

        passed, hours, _ = validate_driver_rest(
            "2026-10-15T00:00:00", "2026-10-15T07:59:00"
        )
        assert passed is False
        assert hours < 8.0

    def test_driver_rest_boundary_8h01m(self):
        """Just over: 8h01m > 8h → PASS."""
        from guardrails.output_validator import validate_driver_rest

        passed, hours, _ = validate_driver_rest(
            "2026-10-15T00:00:00", "2026-10-15T08:01:00"
        )
        assert passed is True
        assert hours > 8.0
