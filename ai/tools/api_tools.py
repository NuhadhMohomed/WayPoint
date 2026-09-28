"""
WayPoint AI — Allow-Listed Backend Tool Wrappers
Student 1 (Sethum): Journey Analysis Agent

These are the ONLY tools the Journey Analysis Agent is permitted to execute (BR-AITOOL-001).
Each tool wraps an authenticated HTTP call to the ASP.NET Core Web API.
AI agents NEVER access PostgreSQL directly (AGENTS.md Rule 1).

Allow-Listed Tools for Student 1:
  1. SearchRoutes     — GET /api/v1/routes
  2. SearchServices   — GET /api/v1/services
  3. GetBoardingPoints— GET /api/v1/boarding-points
  4. GetTimetable     — GET /api/v1/services/{id}
  5. CheckTransferFeasibility — deterministic 20-min transfer validation (BR-TRANSFER-001)
"""

from __future__ import annotations

import os
import time
from datetime import datetime

import httpx

from schemas.journey_schemas import (
    GetBoardingPointsInput,
    SearchRoutesInput,
    SearchServicesInput,
    ToolCallRecord,
    TransferFeasibilityInput,
    TransferFeasibilityResult,
)

# Base URL loaded from environment; defaults to local dev backend
API_BASE_URL = os.getenv("API_BASE_URL", "http://localhost:5010/api/v1")

# Shared async HTTP client — timeout enforced per BR-AIVAL-002
_client: httpx.AsyncClient | None = None

TOOL_TIMEOUT_SECONDS = 10  # Max per-tool execution time


async def _get_client() -> httpx.AsyncClient:
    """Lazy-initialised singleton async HTTP client."""
    global _client
    if _client is None or _client.is_closed:
        _client = httpx.AsyncClient(
            base_url=API_BASE_URL,
            timeout=httpx.Timeout(TOOL_TIMEOUT_SECONDS),
            headers={"Content-Type": "application/json"},
        )
    return _client


async def close_client() -> None:
    """Gracefully close the HTTP client on shutdown."""
    global _client
    if _client is not None and not _client.is_closed:
        await _client.aclose()
        _client = None


# ── Tool 1: SearchRoutes ─────────────────────────────────────────────────────

async def search_routes(inp: SearchRoutesInput) -> tuple[list[dict], ToolCallRecord]:
    """
    GET /api/v1/routes?originCity=...&destinationCity=...
    Returns list of active route corridors matching origin/destination.
    """
    client = await _get_client()
    params: dict[str, str] = {}
    if inp.origin_city:
        params["originCity"] = inp.origin_city
    if inp.destination_city:
        params["destinationCity"] = inp.destination_city

    start = time.perf_counter_ns()
    try:
        response = await client.get("/routes", params=params)
        response.raise_for_status()
        data = response.json()
    except (httpx.HTTPError, Exception) as exc:
        duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
        record = ToolCallRecord(
            tool_name="SearchRoutes",
            arguments=inp.model_dump(),
            result={"error": str(exc)},
            duration_ms=duration_ms,
        )
        return [], record

    duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
    record = ToolCallRecord(
        tool_name="SearchRoutes",
        arguments=inp.model_dump(),
        result={"route_count": len(data), "routes": data},
        duration_ms=duration_ms,
    )
    return data, record


# ── Tool 2: SearchServices ───────────────────────────────────────────────────

async def search_services(inp: SearchServicesInput) -> tuple[list[dict], ToolCallRecord]:
    """
    GET /api/v1/services?routeId=...&date=...
    Returns scheduled departures for a route on a given date.
    Only returns services with Status == Scheduled (BR-SEARCH-001).
    """
    client = await _get_client()
    params: dict[str, str] = {"routeId": inp.route_id}
    if inp.date:
        params["date"] = inp.date

    start = time.perf_counter_ns()
    try:
        response = await client.get("/services", params=params)
        response.raise_for_status()
        data = response.json()
    except (httpx.HTTPError, Exception) as exc:
        duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
        record = ToolCallRecord(
            tool_name="SearchServices",
            arguments=inp.model_dump(),
            result={"error": str(exc)},
            duration_ms=duration_ms,
        )
        return [], record

    duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
    record = ToolCallRecord(
        tool_name="SearchServices",
        arguments=inp.model_dump(),
        result={"service_count": len(data), "services": data},
        duration_ms=duration_ms,
    )
    return data, record


# ── Tool 3: GetBoardingPoints ────────────────────────────────────────────────

async def get_boarding_points(inp: GetBoardingPointsInput) -> tuple[list[dict], ToolCallRecord]:
    """
    GET /api/v1/boarding-points?routeId=...
    Returns pickup/drop-off landmarks and GPS coordinates for a route.
    """
    client = await _get_client()
    params = {"routeId": inp.route_id}

    start = time.perf_counter_ns()
    try:
        response = await client.get("/boarding-points", params=params)
        response.raise_for_status()
        data = response.json()
    except (httpx.HTTPError, Exception) as exc:
        duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
        record = ToolCallRecord(
            tool_name="GetBoardingPoints",
            arguments=inp.model_dump(),
            result={"error": str(exc)},
            duration_ms=duration_ms,
        )
        return [], record

    duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
    record = ToolCallRecord(
        tool_name="GetBoardingPoints",
        arguments=inp.model_dump(),
        result={"point_count": len(data), "points": data},
        duration_ms=duration_ms,
    )
    return data, record


# ── Tool 4: GetTimetable ─────────────────────────────────────────────────────

async def get_timetable(service_id: str) -> tuple[dict | None, ToolCallRecord]:
    """
    GET /api/v1/services/{id}
    Returns detailed departure info for a specific service.
    """
    client = await _get_client()

    start = time.perf_counter_ns()
    try:
        response = await client.get(f"/services/{service_id}")
        response.raise_for_status()
        data = response.json()
    except (httpx.HTTPError, Exception) as exc:
        duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
        record = ToolCallRecord(
            tool_name="GetTimetable",
            arguments={"service_id": service_id},
            result={"error": str(exc)},
            duration_ms=duration_ms,
        )
        return None, record

    duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
    record = ToolCallRecord(
        tool_name="GetTimetable",
        arguments={"service_id": service_id},
        result=data,
        duration_ms=duration_ms,
    )
    return data, record


# ── Tool 5: CheckTransferFeasibility ─────────────────────────────────────────

MINIMUM_TRANSFER_MINUTES = 20  # BR-TRANSFER-001


async def check_transfer_feasibility(
    inp: TransferFeasibilityInput,
) -> tuple[TransferFeasibilityResult, ToolCallRecord]:
    """
    Deterministic transfer window validation (BR-TRANSFER-001).
    Leg2.DepartureTime - Leg1.ArrivalTime MUST be >= 20 minutes.
    This is NOT an LLM call — pure business rule enforcement.
    """
    start = time.perf_counter_ns()

    try:
        leg1_arrival = datetime.fromisoformat(inp.leg1_arrival_time)
        leg2_departure = datetime.fromisoformat(inp.leg2_departure_time)
        transfer_minutes = int((leg2_departure - leg1_arrival).total_seconds() / 60)

        is_feasible = transfer_minutes >= MINIMUM_TRANSFER_MINUTES
        reason = ""
        if not is_feasible:
            reason = (
                f"Transfer buffer is {transfer_minutes} min at {inp.transfer_hub}, "
                f"but minimum required is {MINIMUM_TRANSFER_MINUTES} min (BR-TRANSFER-001)."
            )

        result = TransferFeasibilityResult(
            is_feasible=is_feasible,
            transfer_minutes=transfer_minutes,
            transfer_hub=inp.transfer_hub,
            reason=reason,
        )
    except Exception as exc:
        result = TransferFeasibilityResult(
            is_feasible=False,
            transfer_minutes=0,
            transfer_hub=inp.transfer_hub,
            reason=f"Parsing error: {exc}",
        )

    duration_ms = int((time.perf_counter_ns() - start) / 1_000_000)
    record = ToolCallRecord(
        tool_name="CheckTransferFeasibility",
        arguments=inp.model_dump(),
        result=result.model_dump(),
        duration_ms=duration_ms,
    )
    return result, record

