"""
WayPoint AI — Journey Planning Tools (Student 1 / Sethum).

Tools: SearchRoutes, GetBoardingPoints, CheckTransferFeasibility.
All decorated with @tool for LangGraph binding.
"""

import json
from datetime import datetime

from langchain_core.tools import tool

from schemas.tools import (
    SearchRoutesInput,
    SearchRoutesOutput,
    GetBoardingPointsInput,
    GetBoardingPointsOutput,
    CheckTransferFeasibilityInput,
    CheckTransferFeasibilityOutput,
)
from tools.http_client import make_tool_request


@tool
async def search_routes(
    origin_city: str,
    destination_city: str,
    travel_date: str | None = None,
) -> str:
    """Search for available routes between two cities.

    Use this tool to find route candidates for journey planning.
    Returns active routes matching origin and destination.

    Args:
        origin_city: Origin city name (e.g. 'Colombo').
        destination_city: Destination city name (e.g. 'Ella').
        travel_date: Optional travel date in YYYY-MM-DD format.
    """
    validated = SearchRoutesInput(
        origin_city=origin_city,
        destination_city=destination_city,
        travel_date=travel_date,
    )

    result = await make_tool_request(
        "GET",
        "/routes",
        params={
            "originCity": validated.origin_city,
            "destinationCity": validated.destination_city,
        },
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    # Normalise paginated response (PaginatedResponseDto<RouteSummaryDto>)
    items = result.get("items", [result] if isinstance(result, dict) else result)
    output = SearchRoutesOutput(
        routes=[
            {
                "id": str(r.get("id", "")),
                "route_code": r.get("routeCode", ""),
                "name": r.get("name", ""),
                "origin_city": r.get("originCity", ""),
                "destination_city": r.get("destinationCity", ""),
                "total_distance_km": r.get("totalDistanceKm", 0),
                "is_active": r.get("isActive", True),
            }
            for r in items
        ],
        total_count=result.get("totalCount", len(items)),
    )
    return output.model_dump_json()


@tool
async def get_boarding_points(route_id: str) -> str:
    """Get boarding and drop-off points for a specific route.

    Returns pickup locations with GPS coordinates.
    Backend: GET /api/v1/boarding-points?routeId=...

    Args:
        route_id: UUID of the route.
    """
    validated = GetBoardingPointsInput(route_id=route_id)

    result = await make_tool_request(
        "GET",
        "/boarding-points",
        params={"routeId": validated.route_id},
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    items = result if isinstance(result, list) else result.get("items", [])
    output = GetBoardingPointsOutput(
        boarding_points=[
            {
                "id": str(bp.get("id", "")),
                "point_name": bp.get("pointName", ""),
                "landmark": bp.get("landmark"),
                "latitude": bp.get("latitude"),
                "longitude": bp.get("longitude"),
            }
            for bp in items
        ]
    )
    return output.model_dump_json()


@tool
async def check_transfer_feasibility(
    leg1_arrival_time: str,
    leg2_departure_time: str,
    transfer_stop_id: str | None = None,
) -> str:
    """Check whether a connecting transfer between two journey legs is feasible.

    Validates that the transfer window is at least 20 minutes (BR-TRANSFER-001).
    This is a local validation — no backend call required.

    Args:
        leg1_arrival_time: Arrival time of leg 1 in ISO 8601 format.
        leg2_departure_time: Departure time of leg 2 in ISO 8601 format.
        transfer_stop_id: Optional UUID of the transfer hub stop.
    """
    validated = CheckTransferFeasibilityInput(
        leg1_arrival_time=leg1_arrival_time,
        leg2_departure_time=leg2_departure_time,
        transfer_stop_id=transfer_stop_id,
    )

    try:
        arrival = datetime.fromisoformat(validated.leg1_arrival_time)
        departure = datetime.fromisoformat(validated.leg2_departure_time)
        transfer_minutes = (departure - arrival).total_seconds() / 60.0
    except (ValueError, TypeError) as exc:
        output = CheckTransferFeasibilityOutput(
            is_feasible=False,
            transfer_minutes=0,
            reason=f"Invalid datetime format: {exc}",
        )
        return output.model_dump_json()

    # BR-TRANSFER-001: Minimum 20-minute transfer window
    is_feasible = transfer_minutes >= 20.0
    reason = (
        f"Transfer window is {transfer_minutes:.0f} minutes "
        f"(>= 20 min required)"
        if is_feasible
        else f"Insufficient transfer window: {transfer_minutes:.0f} minutes "
        f"(minimum 20 required)"
    )

    output = CheckTransferFeasibilityOutput(
        is_feasible=is_feasible,
        transfer_minutes=transfer_minutes,
        reason=reason,
    )
    return output.model_dump_json()
