"""
WayPoint AI — Journey Analysis Agent Node (Student 1 / Sethum).

Searches for candidate routes and evaluates connecting transfers.
Tools: SearchRoutes, GetBoardingPoints, CheckTransferFeasibility.
"""

import json
import logging

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.messages import SystemMessage, HumanMessage

from agents.state import WorkflowState
from config import GEMINI_API_KEY, LLM_MODEL, LLM_TEMPERATURE
from prompts.agent_prompts import JOURNEY_AGENT_PROMPT
from tools.registry import JOURNEY_AGENT_TOOLS

logger = logging.getLogger("waypoint.ai.journey_agent")


async def journey_agent_node(state: WorkflowState) -> dict:
    """Journey Analysis Agent node — searches routes and evaluates transfers."""
    logger.info("Journey Analysis Agent started")

    llm = ChatGoogleGenerativeAI(
        model=LLM_MODEL,
        google_api_key=GEMINI_API_KEY,
        temperature=LLM_TEMPERATURE,
    )

    # Bind journey-specific tools
    llm_with_tools = llm.bind_tools(JOURNEY_AGENT_TOOLS)

    messages = [
        SystemMessage(content=JOURNEY_AGENT_PROMPT),
        HumanMessage(
            content=(
                f"Objective: {state.get('objective', '')}\n\n"
                f"Workflow type: {state.get('workflow_type', '')}\n\n"
                f"Disruption case ID: "
                f"{state.get('disruption_case_id', 'N/A')}\n\n"
                f"Please search for relevant routes and evaluate "
                f"transfer feasibility."
            )
        ),
    ]

    response = await llm_with_tools.ainvoke(messages)

    # Parse candidate routes from response
    candidate_routes: list[dict] = []
    try:
        content = (
            response.content
            if isinstance(response.content, str)
            else str(response.content)
        )
        if content.strip().startswith(("{", "[")):
            parsed = json.loads(content)
            if isinstance(parsed, list):
                candidate_routes = parsed
            elif isinstance(parsed, dict):
                candidate_routes = parsed.get(
                    "routes", parsed.get("candidates", [parsed])
                )
    except (json.JSONDecodeError, AttributeError):
        pass

    logger.info(
        "Journey Analysis Agent found %d candidate(s)", len(candidate_routes)
    )

    return {
        "current_agent": "JourneyAnalysisAgent",
        "step_order": state.get("step_order", 0) + 1,
        "messages": [response],
        "candidate_routes": candidate_routes,
        "steps": state.get("steps", [])
        + [
            {
                "agent_name": "JourneyAnalysisAgent",
                "step_order": state.get("step_order", 0) + 1,
                "step_description": (
                    f"Searched routes, found "
                    f"{len(candidate_routes)} candidate(s)"
                ),
                "tool_calls": [],
                "validation_results": [],
            }
        ],
    }
