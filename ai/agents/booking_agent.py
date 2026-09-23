"""
WayPoint AI — Booking & Policy Agent Node (Student 3 / Mithila).

Analyses fare implications and handles passenger notifications.
Tools: CalculateFareDifference, SendPassengerNotification.
"""

import json
import logging

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.messages import SystemMessage, HumanMessage

from agents.state import WorkflowState
from config import GEMINI_API_KEY, LLM_MODEL, LLM_TEMPERATURE
from prompts.agent_prompts import BOOKING_AGENT_PROMPT
from tools.registry import BOOKING_AGENT_TOOLS

logger = logging.getLogger("waypoint.ai.booking_agent")


async def booking_agent_node(state: WorkflowState) -> dict:
    """Booking & Policy Agent node — analyses fares and notifications."""
    logger.info("Booking & Policy Agent started")

    llm = ChatGoogleGenerativeAI(
        model=LLM_MODEL,
        google_api_key=GEMINI_API_KEY,
        temperature=LLM_TEMPERATURE,
    )

    llm_with_tools = llm.bind_tools(BOOKING_AGENT_TOOLS)

    # Build context from previous agents
    feasibility = state.get("feasibility_result", {})
    candidates = state.get("candidate_routes", [])

    messages = [
        SystemMessage(content=BOOKING_AGENT_PROMPT),
        HumanMessage(
            content=(
                f"Objective: {state.get('objective', '')}\n\n"
                f"Workflow type: {state.get('workflow_type', '')}\n\n"
                f"Candidate routes: "
                f"{json.dumps(candidates, indent=2)}\n\n"
                f"Resource feasibility: "
                f"{json.dumps(feasibility, indent=2)}\n\n"
                f"Please calculate fare differences for the proposed "
                f"replacement services."
            )
        ),
    ]

    response = await llm_with_tools.ainvoke(messages)

    # Parse fare analysis
    fare_analysis: dict = {}
    try:
        content = (
            response.content
            if isinstance(response.content, str)
            else str(response.content)
        )
        if content.strip().startswith("{"):
            fare_analysis = json.loads(content)
    except (json.JSONDecodeError, AttributeError):
        fare_analysis = {"raw_response": str(response.content)}

    logger.info("Booking & Policy Agent completed fare analysis")

    return {
        "current_agent": "BookingPolicyAgent",
        "step_order": state.get("step_order", 0) + 1,
        "messages": [response],
        "fare_analysis": fare_analysis,
        "steps": state.get("steps", [])
        + [
            {
                "agent_name": "BookingPolicyAgent",
                "step_order": state.get("step_order", 0) + 1,
                "step_description": (
                    "Analysed fare differences and booking policies"
                ),
                "tool_calls": [],
                "validation_results": [],
            }
        ],
    }
