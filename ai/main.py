"""
WayPoint AI Microservice — FastAPI Entry Point.

Provides:
- GET  /api/ai/health                    → Health check
- POST /api/ai/disruption-rebooking      → Disruption recovery workflow (Phase 3)
- POST /api/ai/journey-recommendation    → Journey recommendation workflow (Phase 3)

Per ADR-003, this Python FastAPI microservice communicates exclusively
with the ASP.NET Core backend via HTTP tool callbacks. Zero direct
database access.
"""

import logging

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

from config import AI_EXECUTION_TIMEOUT_SECONDS

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
        None, description="UUID of the disruption case (for disruption-rebooking)"
    )
    objective: str = Field(
        default="",
        description="Natural language objective describing the task",
    )


class WorkflowResponse(BaseModel):
    """Placeholder response until graph is wired in Phase 3."""

    workflow_id: str = ""
    status: str = ""
    message: str = ""


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------
@app.get("/api/ai/health", response_model=HealthResponse)
async def health_check() -> HealthResponse:
    """Health check endpoint for deployment verification."""
    return HealthResponse()


@app.post(
    "/api/ai/disruption-rebooking",
    response_model=WorkflowResponse,
    status_code=501,
)
async def disruption_rebooking(request: WorkflowRequest) -> WorkflowResponse:
    """
    Trigger the disruption recovery multi-agent workflow.

    Maps to FR-AI-001, UC-03. Will be wired to the LangGraph
    coordinator in Phase 3.
    """
    logger.info(
        "Disruption rebooking requested | case=%s",
        request.disruption_case_id,
    )
    raise HTTPException(
        status_code=501,
        detail="Disruption rebooking workflow not yet implemented (Phase 3)",
    )


@app.post(
    "/api/ai/journey-recommendation",
    response_model=WorkflowResponse,
    status_code=501,
)
async def journey_recommendation(request: WorkflowRequest) -> WorkflowResponse:
    """
    Trigger the journey recommendation multi-agent workflow.

    Maps to FR-AI-001. Will be wired to the LangGraph
    coordinator in Phase 3.
    """
    logger.info("Journey recommendation requested | objective=%s", request.objective)
    raise HTTPException(
        status_code=501,
        detail="Journey recommendation workflow not yet implemented (Phase 3)",
    )
