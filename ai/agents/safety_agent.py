"""
WayPoint AI — Validation & Safety Agent Node (Student 4 / Dineth).

Validates proposals against business rules, classifies impact severity,
and gates high-impact changes for Transport Manager approval.

Tools: CreateRebookingProposal, CalculatePassengerImpact,
       RequestManagerApproval, ApplyApprovedOperationalChange.
"""

import json
import logging

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.messages import SystemMessage, HumanMessage

from agents.state import WorkflowState
from config import GEMINI_API_KEY, LLM_MODEL, LLM_TEMPERATURE
from guardrails.safe_failure import execute_with_safe_failure
from guardrails.input_sanitizer import sanitize_user_input, wrap_user_input
from guardrails.output_validator import validate_impact_classification
from prompts.agent_prompts import SAFETY_AGENT_PROMPT
from tools.registry import SAFETY_AGENT_TOOLS

logger = logging.getLogger("waypoint.ai.safety_agent")


async def _safety_core(state: WorkflowState) -> dict:
    """Core safety validation logic — isolated for safe failure wrapping."""
    logger.info("Validation & Safety Agent started")

    llm = ChatGoogleGenerativeAI(
        model=LLM_MODEL,
        google_api_key=GEMINI_API_KEY,
        temperature=LLM_TEMPERATURE,
    )

    llm_with_tools = llm.bind_tools(SAFETY_AGENT_TOOLS)

    # Sanitize user-provided objective (FR-AI-008)
    objective = sanitize_user_input(state.get("objective", ""))

    # Comprehensive context from all previous agents
    messages = [
        SystemMessage(content=SAFETY_AGENT_PROMPT),
        HumanMessage(
            content=(
                f"Objective: {wrap_user_input(objective)}\n\n"
                f"Workflow type: {state.get('workflow_type', '')}\n\n"
                f"Disruption case ID: "
                f"{state.get('disruption_case_id', 'N/A')}\n\n"
                f"Candidate routes: "
                f"{json.dumps(state.get('candidate_routes', []), indent=2)}"
                f"\n\n"
                f"Resource feasibility: "
                f"{json.dumps(state.get('feasibility_result', {}), indent=2)}"
                f"\n\n"
                f"Fare analysis: "
                f"{json.dumps(state.get('fare_analysis', {}), indent=2)}"
                f"\n\n"
                f"Please validate the proposals, classify impact, and "
                f"determine if manager approval is needed."
            )
        ),
    ]

    response = await llm_with_tools.ainvoke(messages)

    # Parse impact assessment from response
    impact_assessment: dict = {}
    llm_classification = "Low"

    try:
        content = (
            response.content
            if isinstance(response.content, str)
            else str(response.content)
        )
        if content.strip().startswith("{"):
            impact_assessment = json.loads(content)
            llm_classification = impact_assessment.get(
                "impact_classification",
                impact_assessment.get("impactClassification", "Low"),
            )
    except (json.JSONDecodeError, AttributeError):
        impact_assessment = {"raw_response": str(response.content)}

    # Deterministic override via shared guardrail (FR-AI-003, BR-APPROVAL-001)
    is_cancellation = (
        state.get("workflow_type", "") == "disruption_rebooking"
    )
    affected_count = impact_assessment.get("affected_passenger_count", 0)
    delay_minutes = impact_assessment.get("total_delay_minutes", 0)

    impact_classification = validate_impact_classification(
        llm_classification,
        affected_count,
        delay_minutes,
        is_cancellation=is_cancellation,
    )

    requires_approval = impact_classification == "High"
    workflow_status = (
        "PendingManagerApproval" if requires_approval else "Completed"
    )

    logger.info(
        "Safety Agent completed | impact=%s | requires_approval=%s",
        impact_classification,
        requires_approval,
    )

    return {
        "current_agent": "ValidationSafetyAgent",
        "step_order": state.get("step_order", 0) + 1,
        "messages": [response],
        "impact_assessment": impact_assessment,
        "impact_classification": impact_classification,
        "requires_approval": requires_approval,
        "approval_status": (
            "PendingManagerApproval" if requires_approval else ""
        ),
        "workflow_status": workflow_status,
        "steps": state.get("steps", [])
        + [
            {
                "agent_name": "ValidationSafetyAgent",
                "step_order": state.get("step_order", 0) + 1,
                "step_description": (
                    f"Impact: {impact_classification}. "
                    f"Approval required: {requires_approval}."
                ),
                "tool_calls": [],
                "validation_results": [
                    {
                        "rule_name": "ImpactClassification",
                        "passed": True,
                        "validation_details": (
                            f"Classification: {impact_classification}, "
                            f"Affected: {affected_count}, "
                            f"Delay: {delay_minutes} min"
                        ),
                    },
                    {
                        "rule_name": "ApprovalGateCheck",
                        "passed": True,
                        "validation_details": (
                            f"Requires approval: {requires_approval} "
                            f"(BR-APPROVAL-001)"
                        ),
                    },
                ],
            }
        ],
    }


async def safety_agent_node(state: WorkflowState) -> dict:
    """Validation & Safety Agent node — wrapped with safe failure (FR-AI-004)."""
    return await execute_with_safe_failure(
        agent_name="ValidationSafetyAgent",
        core_fn=_safety_core,
        state=state,
    )
