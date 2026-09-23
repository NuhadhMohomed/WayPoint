"""
WayPoint AI — Planner / Coordinator Agent Node.

Entry node of the LangGraph workflow. Receives the objective,
generates a multi-step execution plan, and delegates to
specialised agents via graph edges.
"""

import logging

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.messages import SystemMessage, HumanMessage

from agents.state import WorkflowState
from config import GEMINI_API_KEY, LLM_MODEL, LLM_TEMPERATURE
from prompts.planner_prompt import PLANNER_SYSTEM_PROMPT

logger = logging.getLogger("waypoint.ai.planner")


async def planner_node(state: WorkflowState) -> dict:
    """
    Planner Agent node — the first step in every workflow.

    Analyses the objective and produces a structured execution plan.
    Does NOT invoke tools directly — only plans the delegation.
    """
    logger.info(
        "Planner Agent started | workflow=%s | type=%s",
        state.get("workflow_id", ""),
        state.get("workflow_type", ""),
    )

    llm = ChatGoogleGenerativeAI(
        model=LLM_MODEL,
        google_api_key=GEMINI_API_KEY,
        temperature=LLM_TEMPERATURE,
    )

    objective = state.get("objective", "")

    messages = [
        SystemMessage(content=PLANNER_SYSTEM_PROMPT),
        HumanMessage(
            content=(
                f"Workflow type: {state.get('workflow_type', 'disruption_rebooking')}\n\n"
                f"Objective: {objective}\n\n"
                f"Disruption case ID: {state.get('disruption_case_id', 'N/A')}"
            )
        ),
    ]

    response = await llm.ainvoke(messages)

    logger.info("Planner Agent completed plan generation")

    return {
        "current_agent": "PlannerAgent",
        "step_order": state.get("step_order", 0) + 1,
        "messages": [response],
        "steps": state.get("steps", [])
        + [
            {
                "agent_name": "PlannerAgent",
                "step_order": state.get("step_order", 0) + 1,
                "step_description": "Generated multi-agent execution plan",
                "tool_calls": [],
                "validation_results": [],
            }
        ],
    }
