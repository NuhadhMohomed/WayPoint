"""
WayPoint AI Microservice — FastAPI Entry Point.

Provides:
- GET  /api/ai/health                    → Health check
- POST /api/ai/disruption-rebooking      → Disruption recovery workflow
- POST /api/ai/journey-recommendation    → Journey recommendation workflow

Per ADR-003, this Python FastAPI microservice communicates exclusively
with the ASP.NET Core backend via HTTP tool callbacks. Zero direct
database access.
Security Fix 05: Credentials Security, Service Authentication & Secret Redaction.
"""

import logging
import os
import uuid

from fastapi import Depends, FastAPI, HTTPException, Security, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import APIKeyHeader
from pydantic import BaseModel, Field

from config import (
    AI_EXECUTION_TIMEOUT_SECONDS,
    AI_SERVICE_API_KEY,
    BACKEND_ORIGIN,
    ENVIRONMENT,
    IS_PRODUCTION_OR_STAGING,
    ConfigurationError,
    validate_config,
)
from guardrails.security_sanitizer import SensitiveDataLogFilter, sanitize_text
from agents.state import create_initial_state
from agents.graph import workflow_graph
from schemas.workflow import WorkflowResult
from persistence.workflow_logger import WorkflowLogger

# ---------------------------------------------------------------------------
# Logging with Secret Redaction Filter (Security Fix 05)
# ---------------------------------------------------------------------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s | %(name)-30s | %(levelname)-7s | %(message)s",
)
logger = logging.getLogger("waypoint.ai")
logger.addFilter(SensitiveDataLogFilter())

from contextlib import asynccontextmanager

# ---------------------------------------------------------------------------
# Application Lifespan (Security Fix 05)
# ---------------------------------------------------------------------------
@asynccontextmanager
async def lifespan(app: FastAPI):
    """Validate security configurations on application startup."""
    try:
        validate_config(raise_on_error=True)
        logger.info(
            "Security configuration validated successfully for environment '%s'",
            ENVIRONMENT,
        )
    except ConfigurationError as err:
        logger.critical("FATAL: Security configuration validation failed: %s", err)
        raise
    yield


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
    lifespan=lifespan,
)

# Parse origins if comma-separated, default to backend origin
_allowed_origins = [o.strip() for o in BACKEND_ORIGIN.split(",") if o.strip()]

app.add_middleware(
    CORSMiddleware,
    allow_origins=_allowed_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ---------------------------------------------------------------------------
# Service-to-Service Authentication Dependency (Security Fix 05)
# ---------------------------------------------------------------------------
api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)


async def verify_service_auth(api_key: str | None = Security(api_key_header)) -> bool:
    """
    Verify service-to-service authentication header for protected endpoints.

    In production/staging environments, or whenever AI_SERVICE_API_KEY is configured,
    requests must provide a valid matching X-API-Key header.
    In development mode without an explicit AI_SERVICE_API_KEY, unauthenticated local
    calls are permitted to maintain frictionless developer workflow.
    """
    from config import is_production_or_staging, AI_SERVICE_API_KEY as CURRENT_API_KEY

    expected_key = os.getenv("AI_SERVICE_API_KEY") or os.getenv("AI_API_KEY") or CURRENT_API_KEY
    is_prod = is_production_or_staging()

    if is_prod or expected_key:
        if not expected_key and is_prod:
            logger.error("Service authentication misconfigured: missing AI_SERVICE_API_KEY in production.")
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail="Service authentication misconfigured.",
            )
        if not api_key:
            logger.warning("Service request rejected: missing X-API-Key header")
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Unauthorized: missing service authentication credentials.",
            )
        if api_key != expected_key:
            logger.warning("Service request rejected: invalid X-API-Key header")
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Unauthorized: invalid service authentication credentials.",
            )
    return True



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
    """Health check endpoint for deployment verification (Public / Unauthenticated)."""
    return HealthResponse()


@app.post("/api/ai/disruption-rebooking", dependencies=[Depends(verify_service_auth)])
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

        # Fire-and-forget persistence (ADR-004)
        try:
            wf_logger = WorkflowLogger()
            await wf_logger.log_full_workflow(
                objective=objective,
                steps=final_state.get("steps", []),
                final_status=final_state.get("workflow_status", "Completed"),
            )
        except Exception:
            logger.warning("Workflow persistence failed (non-fatal)", exc_info=True)

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
            agent_reasoning=f"Disruption rebooking workflow completed with {final_state.get('step_order', 0)} steps.",
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
        # Redact secrets from error message detail
        sanitized_exc = sanitize_text(str(exc))
        raise HTTPException(
            status_code=500,
            detail=f"Workflow execution failed: {sanitized_exc}",
        ) from None


@app.post("/api/ai/journey-recommendation", dependencies=[Depends(verify_service_auth)])
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

        # Fire-and-forget persistence (ADR-004)
        try:
            wf_logger = WorkflowLogger()
            await wf_logger.log_full_workflow(
                objective=objective,
                steps=final_state.get("steps", []),
                final_status=final_state.get("workflow_status", "Completed"),
            )
        except Exception:
            logger.warning("Workflow persistence failed (non-fatal)", exc_info=True)

        result = WorkflowResult(
            workflow_id=workflow_id,
            workflow_type="journey_recommendation",
            status=final_state.get("workflow_status", "Completed"),
            steps_completed=final_state.get("step_order", 0),
            candidate_routes=final_state.get("candidate_routes", []),
            feasibility_result=final_state.get("feasibility_result"),
            fare_analysis=final_state.get("fare_analysis"),
            agent_reasoning=f"Journey recommendation completed with {final_state.get('step_order', 0)} steps.",
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
        # Redact secrets from error message detail
        sanitized_exc = sanitize_text(str(exc))
        raise HTTPException(
            status_code=500,
            detail=f"Workflow execution failed: {sanitized_exc}",
        ) from None
