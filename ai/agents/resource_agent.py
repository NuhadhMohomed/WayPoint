"""
WayPoint AI — Resource Feasibility Agent Node (Student 2 / Nuhadh).

Evaluates replacement bus/driver/seat availability.
Tools: CheckSeatAvailability, CheckTransferFeasibility.
"""

import json
import logging

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.messages import SystemMessage, HumanMessage

from agents.state import WorkflowState
from config import GEMINI_API_KEY, LLM_MODEL, LLM_TEMPERATURE
from prompts.agent_prompts import RESOURCE_AGENT_PROMPT
from tools.registry import RESOURCE_AGENT_TOOLS

logger = logging.getLogger("waypoint.ai.resource_agent")


async def resource_agent_node(state: WorkflowState) -> dict:
    """Resource Feasibility Agent node — checks seat/resource availability."""
    logger.info("Resource Feasibility Agent started")

    llm = ChatGoogleGenerativeAI(
        model=LLM_MODEL,
        google_api_key=GEMINI_API_KEY,
        temperature=LLM_TEMPERATURE,
    )

    llm_with_tools = llm.bind_tools(RESOURCE_AGENT_TOOLS)

    # Build context from Journey Analysis Agent output
    candidate_routes = state.get("candidate_routes", [])
    candidate_ctx = (
        f"Candidate routes from Journey Analysis Agent:\n"
        f"{json.dumps(candidate_routes, indent=2)}\n\n"
        if candidate_routes
        else "No candidate routes provided yet.\n\n"
    )

    messages = [
        SystemMessage(content=RESOURCE_AGENT_PROMPT),
        HumanMessage(
            content=(
                f"Objective: {state.get('objective', '')}\n\n"
                f"Workflow type: {state.get('workflow_type', '')}\n\n"
                f"{candidate_ctx}"
                f"Please check seat availability for the candidate "
                f"services and evaluate resource feasibility."
            )
        ),
    ]

    response = await llm_with_tools.ainvoke(messages)

    # Parse feasibility result
    feasibility_result: dict = {}
    try:
        content = (
            response.content
            if isinstance(response.content, str)
            else str(response.content)
        )
        if content.strip().startswith("{"):
            feasibility_result = json.loads(content)
    except (json.JSONDecodeError, AttributeError):
        feasibility_result = {"raw_response": str(response.content)}

    logger.info(
        "Resource Feasibility Agent completed | feasible=%s",
        feasibility_result.get("isFeasible", "unknown"),
    )

    return {
        "current_agent": "ResourceFeasibilityAgent",
        "step_order": state.get("step_order", 0) + 1,
        "messages": [response],
        "feasibility_result": feasibility_result,
        "steps": state.get("steps", [])
        + [
            {
                "agent_name": "ResourceFeasibilityAgent",
                "step_order": state.get("step_order", 0) + 1,
                "step_description": (
                    "Evaluated seat and resource feasibility"
                ),
                "tool_calls": [],
                "validation_results": [],
            }
        ],
    }
