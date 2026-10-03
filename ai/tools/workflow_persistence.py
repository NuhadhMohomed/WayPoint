"""
WayPoint AI — Workflow State Persistence Client
Student 1 (Sethum): Journey Analysis Agent

Persists AI workflow execution state to PostgreSQL via the ASP.NET Core backend API
(ADR-004: Relational PostgreSQL Strategy for AI Workflow Execution State).

Tables managed:
  - AiWorkflows       → Top-level workflow record
  - AiWorkflowSteps   → Per-agent step logs
  - AiToolCalls       → Tool execution traces (JSONB)
  - AiValidationResults → Deterministic rule assertions

AI NEVER accesses the database directly (AGENTS.md Rule 1).
"""

from __future__ import annotations

import json
import logging
import os

import httpx

logger = logging.getLogger("waypoint.ai.persistence")

API_BASE_URL = os.getenv("API_BASE_URL", "http://localhost:5010/api/v1")
_client: httpx.AsyncClient | None = None


async def _get_client() -> httpx.AsyncClient:
    """Lazy-initialised singleton async HTTP client for persistence calls."""
    global _client
    if _client is None or _client.is_closed:
        _client = httpx.AsyncClient(
            base_url=API_BASE_URL,
            timeout=httpx.Timeout(10.0),
            headers={"Content-Type": "application/json"},
        )
    return _client


async def close_client() -> None:
    """Shutdown the persistence HTTP client."""
    global _client
    if _client is not None and not _client.is_closed:
        await _client.aclose()
        _client = None


# ── Workflow Lifecycle ───────────────────────────────────────────────────────

async def create_workflow(objective: str) -> str | None:
    """
    POST /api/v1/ai/workflows
    Creates a new AiWorkflow record with Status=Running.
    Returns the workflow ID or None on failure.
    """
    client = await _get_client()
    try:
        response = await client.post("/ai/workflows", json={"objective": objective})
        response.raise_for_status()
        data = response.json()
        workflow_id = data.get("id", "")
        logger.info("Created AI workflow %s", workflow_id)
        return str(workflow_id)
    except Exception as exc:
        logger.warning("Failed to create AI workflow: %s", exc)
        return None


async def complete_workflow(workflow_id: str, status: str) -> bool:
    """
    PATCH /api/v1/ai/workflows/{id}/complete
    Marks workflow as Completed or SafeFailure with CompletedAt timestamp.
    """
    client = await _get_client()
    try:
        response = await client.patch(
            f"/ai/workflows/{workflow_id}/complete",
            json={"status": status},
        )
        response.raise_for_status()
        logger.info("Workflow %s marked as %s", workflow_id, status)
        return True
    except Exception as exc:
        logger.warning("Failed to complete workflow %s: %s", workflow_id, exc)
        return False


# ── Step Logging ─────────────────────────────────────────────────────────────

async def log_step(
    workflow_id: str,
    agent_name: str,
    step_order: int,
    description: str,
) -> str | None:
    """
    POST /api/v1/ai/workflows/{id}/steps
    Logs an agent execution step. Returns step ID or None.
    """
    client = await _get_client()
    try:
        response = await client.post(
            f"/ai/workflows/{workflow_id}/steps",
            json={
                "agentName": agent_name,
                "stepOrder": step_order,
                "stepDescription": description,
            },
        )
        response.raise_for_status()
        data = response.json()
        step_id = data.get("id", "")
        return str(step_id)
    except Exception as exc:
        logger.warning("Failed to log step for workflow %s: %s", workflow_id, exc)
        return None


# ── Tool Call Logging ────────────────────────────────────────────────────────

async def log_tool_call(
    step_id: str,
    tool_name: str,
    arguments: dict,
    result: dict,
    duration_ms: int,
) -> bool:
    """
    POST /api/v1/ai/workflow-steps/{id}/tool-calls
    Logs a tool execution trace with JSONB arguments and result.
    """
    client = await _get_client()
    try:
        response = await client.post(
            f"/ai/workflow-steps/{step_id}/tool-calls",
            json={
                "toolName": tool_name,
                "argumentsJson": json.dumps(arguments),
                "resultJson": json.dumps(result),
                "durationMs": duration_ms,
            },
        )
        response.raise_for_status()
        return True
    except Exception as exc:
        logger.warning("Failed to log tool call %s: %s", tool_name, exc)
        return False


# ── Validation Logging ───────────────────────────────────────────────────────

async def log_validation(
    step_id: str,
    rule_name: str,
    passed: bool,
    details: str = "",
) -> bool:
    """
    POST /api/v1/ai/workflow-steps/{id}/validations
    Logs a deterministic rule validation check result.
    """
    client = await _get_client()
    try:
        response = await client.post(
            f"/ai/workflow-steps/{step_id}/validations",
            json={
                "ruleName": rule_name,
                "passed": passed,
                "validationDetails": details,
            },
        )
        response.raise_for_status()
        return True
    except Exception as exc:
        logger.warning("Failed to log validation %s: %s", rule_name, exc)
        return False

