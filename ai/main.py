"""
WayPoint AI Microservice — FastAPI Entry Point.

Provides:
- GET  /api/ai/health                    → Health check
- POST /api/ai/disruption-rebooking      → Disruption recovery workflow
- POST /api/ai/journey-recommendation    → Journey recommendation workflow

Per ADR-003, this Python FastAPI microservice communicates exclusively
with the ASP.NET Core backend via HTTP tool callbacks. Zero direct
database access.
"""

import logging
import uuid

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

from config import AI_EXECUTION_TIMEOUT_SECONDS
from agents.state import create_initial_state
from agents.graph import workflow_graph
from schemas.workflow import WorkflowResult

# ---------------------------------------------------------------------------
# Logging
# ---------------------------------------------------------------------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s | %(name)-30s | %(levelname)-7s | %(message)s",
)
logger = logging.getLogger("waypoint.ai")

# ---------------------------------------------------------------------------
# FastAPI Application
# ---------------------------------------------------------------------------
app = FastAPI(
    title="WayPoint AI Microservice",
    description=(
        "Level 4 Multi-Agent Agentic AI Subsystem for the WayPoint "
        "Intercity Journey Planner & Bus Operations Platform (SE3090)."
    ),
    version="0.1.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ---------------------------------------------------------------------------
# Request / Response Models
# ---------------------------------------------------------------------------
class HealthResponse(BaseModel):
    status: str = "healthy"
    version: str = "0.1.0"
    timeout_seconds: int = AI_EXECUTION_TIMEOUT_SECONDS


class WorkflowRequest(BaseModel):
    """Inbound request to trigger an AI workflow."""

    disruption_case_id: str | None = Field(
        None,
        description="UUID of the disruption case (for disruption-rebooking)",
    )
    objective: str = Field(
        default="",
        description="Natural language objective describing the task",
    )


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------
@app.get("/api/ai/health", response_model=HealthResponse)
async def health_check() -> HealthResponse:
    """Health check endpoint for deployment verification."""
    return HealthResponse()


@app.post("/api/ai/disruption-rebooking")
async def disruption_rebooking(request: WorkflowRequest) -> dict:
    """
    Trigger the disruption recovery multi-agent workflow.

    Maps to FR-AI-001, UC-03. Invokes the 5-node LangGraph workflow:
    planner → journey → resource → booking → safety.

    Disruption workflows are deterministically classified as High-Impact
    (BR-APPROVAL-001) and transition to PendingManagerApproval.
    """
    workflow_id = str(uuid.uuid4())
    objective = request.objective or (
        f"Disruption recovery for case {request.disruption_case_id}"
    )

    logger.info(
        "Disruption rebooking started | workflow=%s | case=%s",
        workflow_id,
        request.disruption_case_id,
    )

    try:
        initial_state = create_initial_state(
            objective=objective,
            workflow_type="disruption_rebooking",
            disruption_case_id=request.disruption_case_id or "",
            workflow_id=workflow_id,
        )

        final_state = await workflow_graph.ainvoke(initial_state)

        result = WorkflowResult(
            workflow_id=workflow_id,
            workflow_type="disruption_rebooking",
            status=final_state.get("workflow_status", "Completed"),
            steps_completed=final_state.get("step_order", 0),
            candidate_routes=final_state.get("candidate_routes", []),
            feasibility_result=final_state.get("feasibility_result"),
            fare_analysis=final_state.get("fare_analysis"),
            impact_assessment=final_state.get("impact_assessment"),
            approval_status=final_state.get("approval_status"),
        )

        logger.info(
            "Disruption rebooking completed | workflow=%s | status=%s",
            workflow_id,
            result.status,
        )
        return result.model_dump()

    except Exception as exc:
        logger.exception(
            "Disruption rebooking failed | workflow=%s", workflow_id
        )
        raise HTTPException(
            status_code=500,
            detail=f"Workflow execution failed: {exc}",
        ) from exc


@app.post("/api/ai/journey-recommendation")
async def journey_recommendation(request: WorkflowRequest) -> dict:
    """
    Trigger the journey recommendation multi-agent workflow.

    Maps to FR-AI-001. Invokes the first 3 nodes of the workflow:
    planner → journey → resource → booking.
    (Safety agent skipped for non-disruption recommendations.)
    """
    workflow_id = str(uuid.uuid4())
    objective = request.objective or "Find the best journey option"

    logger.info(
        "Journey recommendation started | workflow=%s | objective=%s",
        workflow_id,
        objective[:100],
    )

    try:
        initial_state = create_initial_state(
            objective=objective,
            workflow_type="journey_recommendation",
            workflow_id=workflow_id,
        )

        final_state = await workflow_graph.ainvoke(initial_state)

        result = WorkflowResult(
            workflow_id=workflow_id,
            workflow_type="journey_recommendation",
            status=final_state.get("workflow_status", "Completed"),
            steps_completed=final_state.get("step_order", 0),
            candidate_routes=final_state.get("candidate_routes", []),
            feasibility_result=final_state.get("feasibility_result"),
            fare_analysis=final_state.get("fare_analysis"),
        )

        logger.info(
            "Journey recommendation completed | workflow=%s | status=%s",
            workflow_id,
            result.status,
        )
        return result.model_dump()

    except Exception as exc:
        logger.exception(
            "Journey recommendation failed | workflow=%s", workflow_id
        )
        raise HTTPException(
            status_code=500,
            detail=f"Workflow execution failed: {exc}",
        ) from exc
