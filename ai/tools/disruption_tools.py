"""
WayPoint AI — Disruption & Approval Tools (Student 4 / Dineth).

Tools: CreateRebookingProposal, CalculatePassengerImpact,
       RequestManagerApproval, ApplyApprovedOperationalChange.
"""

import json

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


@tool
async def create_rebooking_proposal(
    disruption_case_id: str,
    replacement_service_id: str,
    proposed_by_agent: str = "ValidationSafetyAgent",
) -> str:
    """Create a rebooking proposal for a disrupted service.

    Backend: POST /api/v1/rebooking/generate-proposal
    Request:  GenerateRebookingProposalDto { disruptionCaseId }
    Response: RebookingProposalDto

    Args:
        disruption_case_id: UUID of the disruption case.
        replacement_service_id: UUID of the replacement service.
        proposed_by_agent: Name of the agent creating the proposal.
    """
    validated = CreateRebookingProposalInput(
        disruption_case_id=disruption_case_id,
        replacement_service_id=replacement_service_id,
        proposed_by_agent=proposed_by_agent,
    )

    result = await make_tool_request(
        "POST",
        "/rebooking/generate-proposal",
        json_body={
            "disruptionCaseId": validated.disruption_case_id,
            "replacementServiceId": validated.replacement_service_id,
            "proposedByAgent": validated.proposed_by_agent,
        },
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    output = CreateRebookingProposalOutput(
        proposal_id=str(result.get("proposalId", "")),
        disruption_case_id=str(
            result.get("disruptionCaseId", validated.disruption_case_id)
        ),
        replacement_service_id=str(
            result.get("replacementServiceId", "")
        ),
        status=result.get("status", "PendingManagerApproval"),
    )
    return output.model_dump_json()


@tool
async def calculate_passenger_impact(disrupted_service_id: str) -> str:
    """Calculate the passenger impact of a service disruption.

    Returns affected passenger count, delay, fare impact, and booking IDs.
    Use this to classify impact as Low or High (BR-APPROVAL-001).

    Backend: GET /api/v1/disruptions/{id}

    Args:
        disrupted_service_id: UUID of the disrupted service.
    """
    validated = CalculatePassengerImpactInput(
        disrupted_service_id=disrupted_service_id
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
    return output.model_dump_json()


@tool
async def request_manager_approval(
    rebooking_proposal_id: str,
    impact_classification: str = "High",
    justification: str = "",
) -> str:
    """Request Transport Manager approval for a high-impact operational change.

    Transitions the workflow to PendingManagerApproval state.
    The workflow HALTS until the Transport Manager reviews via the
    Manager Approval Workbench.

    High-impact triggers (BR-APPROVAL-001):
    - Cancelling a ticketed service
    - Timetable shift > 15 minutes
    - Bus/driver reassignment causing schedule conflict

    Args:
        rebooking_proposal_id: UUID of the rebooking proposal.
        impact_classification: 'Low' or 'High'.
        justification: Reason for requesting approval.
    """
    validated = RequestManagerApprovalInput(
        rebooking_proposal_id=rebooking_proposal_id,
        impact_classification=impact_classification,
        justification=justification,
    )

    result = await make_tool_request(
        "PUT",
        f"/approvals/{validated.rebooking_proposal_id}/request",
        json_body={
            "impactClassification": validated.impact_classification,
            "justification": validated.justification,
        },
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
    return output.model_dump_json()


@tool
async def apply_approved_operational_change(
    rebooking_proposal_id: str,
) -> str:
    """Execute an approved rebooking proposal.

    Applies the transactional rebooking after Transport Manager approval.
    Rebooks affected passengers onto the replacement service.

    IMPORTANT: Can ONLY be called AFTER the Transport Manager approves
    via POST /api/v1/approvals/{id}/decision.

    Backend: POST /api/v1/approvals/{id}/decision (execute path)

    Args:
        rebooking_proposal_id: UUID of the approved rebooking proposal.
    """
    validated = ApplyApprovedChangeInput(
        rebooking_proposal_id=rebooking_proposal_id
    )

    result = await make_tool_request(
        "POST",
        f"/rebooking/{validated.rebooking_proposal_id}/execute",
        json_body={},
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    output = ApplyApprovedChangeOutput(
        success=True,
        passengers_rebooked=result.get("affectedPassengersRebooked", 0),
        message=result.get("message", "Rebooking applied successfully"),
    )
    return output.model_dump_json()
