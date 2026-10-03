"""
WayPoint AI — Disruption & Approval Tools (Student 4 / Dineth).

Implements the 4 allow-listed disruption tools as async LangChain @tool functions
with HTTP bridge communication to the ASP.NET Core backend API (Phase 2).

Tools:
  1. CreateRebookingProposal     → POST /api/v1/rebooking/generate-proposal
  2. CalculatePassengerImpact    → GET  /api/v1/disruptions/{id}
  3. RequestManagerApproval      → PUT  /api/v1/approvals/{id}/request
  4. ApplyApprovedOperationalChange → POST /api/v1/rebooking/{id}/execute

Requirements:
  - BR-AITOOL-001: Exactly 4 allow-listed tools bound to Safety Agent
  - BR-AITOOL-002: Strict Pydantic input/output schema validation
  - BR-APPLY-001: Prohibition of unauthorized automated change execution
  - FR-AI-002: Allow-listed tool registry and controlled execution loop
"""

import json
import logging

from pydantic import ValidationError
from langchain_core.tools import tool

from schemas.tools import (
    CreateRebookingProposalInput,
    CreateRebookingProposalOutput,
    CalculatePassengerImpactInput,
    CalculatePassengerImpactOutput,
    RequestManagerApprovalInput,
    RequestManagerApprovalOutput,
    ApplyApprovedChangeInput,
    ApplyApprovedChangeOutput,
)
from tools.http_client import make_tool_request

logger = logging.getLogger("waypoint.ai.tools.disruption")


def _validation_error_response(tool_name: str, exc: ValidationError) -> str:
    """Build a structured error JSON string from a Pydantic ValidationError.

    This ensures the LLM receives a clear, parseable error instead of
    an uncaught exception trace (BR-AITOOL-002 resilience).
    """
    logger.warning(
        "Tool %s input validation failed: %s", tool_name, exc.error_count()
    )
    return json.dumps({
        "error": True,
        "tool": tool_name,
        "detail": f"Input validation failed: {exc.error_count()} error(s)",
        "validation_errors": [
            {"field": e["loc"][-1] if e["loc"] else "unknown", "message": e["msg"]}
            for e in exc.errors()
        ],
    })


@tool
async def create_rebooking_proposal(
    disruption_case_id: str,
    replacement_service_id: str,
    proposed_by_agent: str = "ValidationSafetyAgent",
) -> str:
    """Create a rebooking proposal for a disrupted service.

    Generates a formal rebooking proposal pairing a disrupted service with
    a replacement coach. The proposal enters PendingManagerApproval status.

    Backend: POST /api/v1/rebooking/generate-proposal
    Request:  CreateRebookingProposalDto { disruptionCaseId, replacementServiceId, proposedByAgent }
    Response: RebookingProposalDto

    Args:
        disruption_case_id: UUID of the disruption case.
        replacement_service_id: UUID of the replacement service.
        proposed_by_agent: Name of the agent creating the proposal.
    """
    try:
        validated = CreateRebookingProposalInput(
            disruption_case_id=disruption_case_id,
            replacement_service_id=replacement_service_id,
            proposed_by_agent=proposed_by_agent,
        )
    except ValidationError as exc:
        return _validation_error_response("CreateRebookingProposal", exc)

    logger.info(
        "CreateRebookingProposal: disruption=%s replacement=%s",
        validated.disruption_case_id,
        validated.replacement_service_id,
    )

    result = await make_tool_request(
        "POST",
        "/rebooking/generate-proposal",
        json_body=validated.model_dump(by_alias=True),
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    output = CreateRebookingProposalOutput(
        proposal_id=str(result.get("proposalId", result.get("id", ""))),
        disruption_case_id=str(
            result.get("disruptionCaseId", validated.disruption_case_id)
        ),
        replacement_service_id=str(
            result.get("replacementServiceId", "")
        ),
        status=result.get("status", "PendingManagerApproval"),
    )
    return output.model_dump_json(by_alias=True)


@tool
async def calculate_passenger_impact(disrupted_service_id: str) -> str:
    """Calculate the passenger impact of a service disruption.

    Computes affected passenger count, total delay, revenue at risk,
    and affected booking IDs from confirmed bookings. Use this to
    classify impact as Low or High (BR-APPROVAL-001).

    Backend: GET /api/v1/disruptions/{id}
    Response: DisruptionImpactDto

    Args:
        disrupted_service_id: UUID of the disrupted service.
    """
    try:
        validated = CalculatePassengerImpactInput(
            disrupted_service_id=disrupted_service_id
        )
    except ValidationError as exc:
        return _validation_error_response("CalculatePassengerImpact", exc)

    logger.info(
        "CalculatePassengerImpact: service=%s",
        validated.disrupted_service_id,
    )

    result = await make_tool_request(
        "GET", f"/disruptions/{validated.disrupted_service_id}"
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    output = CalculatePassengerImpactOutput(
        affected_passenger_count=result.get("affectedPassengerCount", 0),
        total_delay_minutes=result.get("totalDelayMinutes", 0),
        net_fare_delta=result.get("netFareDelta", 0.0),
        affected_booking_ids=[
            str(bid) for bid in result.get("affectedBookingIds", [])
        ],
    )
    return output.model_dump_json(by_alias=True)


@tool
async def request_manager_approval(
    rebooking_proposal_id: str,
    impact_classification: str = "High",
    justification: str = "",
) -> str:
    """Request Transport Manager approval for a high-impact operational change.

    Transitions the workflow to PendingManagerApproval state.
    The workflow HALTS until the Transport Manager reviews via the
    Manager Approval Workbench (WEB-08).

    High-impact triggers (BR-APPROVAL-001):
    - Cancelling a ticketed service
    - Timetable shift > 15 minutes
    - Bus/driver reassignment causing schedule conflict

    Backend: PUT /api/v1/approvals/{id}/request

    Args:
        rebooking_proposal_id: UUID of the rebooking proposal.
        impact_classification: 'Low' or 'High'.
        justification: Reason for requesting approval.
    """
    try:
        validated = RequestManagerApprovalInput(
            rebooking_proposal_id=rebooking_proposal_id,
            impact_classification=impact_classification,
            justification=justification,
        )
    except ValidationError as exc:
        return _validation_error_response("RequestManagerApproval", exc)

    logger.info(
        "RequestManagerApproval: proposal=%s impact=%s",
        validated.rebooking_proposal_id,
        validated.impact_classification,
    )

    result = await make_tool_request(
        "PUT",
        f"/approvals/{validated.rebooking_proposal_id}/request",
        json_body=validated.model_dump(by_alias=True),
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    output = RequestManagerApprovalOutput(
        proposal_id=validated.rebooking_proposal_id,
        new_status="PendingManagerApproval",
        message=(
            f"High-impact change requires Transport Manager approval. "
            f"Justification: {validated.justification}"
        ),
    )
    return output.model_dump_json(by_alias=True)


@tool
async def apply_approved_operational_change(
    rebooking_proposal_id: str,
) -> str:
    """Execute an approved rebooking proposal.

    Applies the transactional rebooking after Transport Manager approval.
    Rebooks affected passengers onto the replacement service with atomic
    seat transfer and ticket re-issuance.

    IMPORTANT (BR-APPLY-001): Can ONLY be called AFTER the Transport Manager
    approves via POST /api/v1/approvals/{id}/decision. The backend will
    reject execution if the proposal has not achieved 'Approved' status,
    returning a 400 error with detail explaining the status constraint.

    Backend: POST /api/v1/rebooking/{id}/execute
    Response: RebookingExecutionResultDto

    Args:
        rebooking_proposal_id: UUID of the approved rebooking proposal.
    """
    try:
        validated = ApplyApprovedChangeInput(
            rebooking_proposal_id=rebooking_proposal_id
        )
    except ValidationError as exc:
        return _validation_error_response("ApplyApprovedOperationalChange", exc)

    logger.info(
        "ApplyApprovedOperationalChange: proposal=%s (requires prior manager approval)",
        validated.rebooking_proposal_id,
    )

    result = await make_tool_request(
        "POST",
        f"/rebooking/{validated.rebooking_proposal_id}/execute",
        json_body={},
    )

    if isinstance(result, dict) and result.get("error"):
        # BR-APPLY-001: Backend returns error if proposal status != Approved
        logger.warning(
            "ApplyApprovedOperationalChange rejected: %s",
            result.get("detail", "Unknown error"),
        )
        return json.dumps(result)

    output = ApplyApprovedChangeOutput(
        success=True,
        passengers_rebooked=result.get(
            "passengersRebooked", result.get("affectedPassengersRebooked", 0)
        ),
        message=result.get("summary", result.get("message", "Rebooking applied successfully")),
    )
    return output.model_dump_json(by_alias=True)
