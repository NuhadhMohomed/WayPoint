"""
WayPoint AI — Resource Feasibility Tools (Student 2 / Nuhadh).

Tools: CheckSeatAvailability.
Also provides check_replacement_resources() helper for the Resource
Feasibility Agent during disruption recovery workflows.
"""

import json

from langchain_core.tools import tool

from schemas.tools import (
    CheckSeatAvailabilityInput,
    CheckSeatAvailabilityOutput,
)
from tools.http_client import make_tool_request


@tool
async def check_seat_availability(service_id: str) -> str:
    """Check real-time seat availability for a specific bus service.

    Returns a seat matrix with each seat's status (Available, Held, Booked),
    computed server-side by cross-referencing SeatHolds and Bookings
    (BR-SEAT-001).

    Backend: GET /api/v1/services/{serviceId}/seats
    Response mirrors ServiceSeatMatrixDto in FleetDtos.cs.

    Args:
        service_id: UUID of the scheduled service.
    """
    validated = CheckSeatAvailabilityInput(service_id=service_id)

    result = await make_tool_request(
        "GET", f"/services/{validated.service_id}/seats"
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    # Map camelCase backend response → snake_case Pydantic model
    output = CheckSeatAvailabilityOutput(
        service_id=str(result.get("serviceId", validated.service_id)),
        service_code=result.get("serviceCode", ""),
        total_seats=result.get("totalSeats", 0),
        available_seats=result.get("availableSeats", 0),
        held_seats=result.get("heldSeats", 0),
        booked_seats=result.get("bookedSeats", 0),
        seats=[
            {
                "seat_id": str(s.get("id", "")),
                "seat_number": s.get("seatNumber", ""),
                "row_index": s.get("rowIndex", 0),
                "column_index": s.get("columnIndex", 0),
                "seat_class": s.get("seatClass", "Standard"),
                "status": s.get("status", "Available"),
            }
            for s in result.get("seats", [])
        ],
    )
    return output.model_dump_json()


async def check_replacement_resources(
    disrupted_service_id: str,
    required_seat_capacity: int,
    required_departure_time: str,
) -> dict:
    """Evaluate replacement bus and driver availability for a disrupted service.

    Backend: POST /api/v1/resources/replacement-feasibility
    Request:  ResourceFeasibilityRequestDto
    Response: ResourceFeasibilityResponseDto

    This is an internal helper (NOT a LangChain tool) used by the
    Resource Feasibility Agent during disruption recovery workflows.

    Evaluates:
    - Buses: not under maintenance, capacity >= required, no conflict
      (BR-RESOURCE-001)
    - Drivers: active, 8h rest rule, no overlap (BR-RESOURCE-002)
    """
    return await make_tool_request(
        "POST",
        "/resources/replacement-feasibility",
        json_body={
            "disruptedServiceId": disrupted_service_id,
            "requiredSeatCapacity": required_seat_capacity,
            "requiredDepartureTime": required_departure_time,
        },
    )
