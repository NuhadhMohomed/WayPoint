"""
WayPoint AI — Booking & Policy Tools (Student 3 / Mithila).

Tools: CalculateFareDifference, SendPassengerNotification.
"""

import json

from langchain_core.tools import tool

from schemas.tools import (
    CalculateFareDifferenceInput,
    CalculateFareDifferenceOutput,
    SendPassengerNotificationInput,
    SendPassengerNotificationOutput,
)
from tools.http_client import make_tool_request


@tool
async def calculate_fare_difference(
    original_service_id: str,
    replacement_service_id: str,
) -> str:
    """Calculate the fare difference between an original and replacement service.

    Fetches baseFare from both services and computes the delta.
    Use this during disruption recovery to assess passenger fare impact.

    Args:
        original_service_id: UUID of the original disrupted service.
        replacement_service_id: UUID of the proposed replacement service.
    """
    validated = CalculateFareDifferenceInput(
        original_service_id=original_service_id,
        replacement_service_id=replacement_service_id,
    )

    # Fetch fare data for both services
    original = await make_tool_request(
        "GET", f"/services/{validated.original_service_id}"
    )
    replacement = await make_tool_request(
        "GET", f"/services/{validated.replacement_service_id}"
    )

    # Propagate errors
    for result in (original, replacement):
        if isinstance(result, dict) and result.get("error"):
            return json.dumps(result)

    original_fare = float(original.get("baseFare", 0))
    replacement_fare = float(replacement.get("baseFare", 0))
    fare_diff = replacement_fare - original_fare

    output = CalculateFareDifferenceOutput(
        original_fare=original_fare,
        replacement_fare=replacement_fare,
        fare_difference=fare_diff,
        passenger_pays_extra=fare_diff > 0,
        note=(
            f"Passenger pays LKR {fare_diff:.2f} extra"
            if fare_diff > 0
            else (
                f"Passenger saves LKR {abs(fare_diff):.2f}"
                if fare_diff < 0
                else "No fare difference"
            )
        ),
    )
    return output.model_dump_json()


@tool
async def send_passenger_notification(
    passenger_ids: list[str],
    title: str,
    message: str,
) -> str:
    """Send push/in-app notifications to affected passengers.

    Use after a rebooking decision to inform passengers about journey changes.
    Backend: POST /api/v1/notifications

    Args:
        passenger_ids: List of passenger UUIDs to notify.
        title: Notification title.
        message: Notification body text.
    """
    validated = SendPassengerNotificationInput(
        passenger_ids=passenger_ids,
        title=title,
        message=message,
    )

    result = await make_tool_request(
        "POST",
        "/notifications",
        json_body={
            "passengerIds": validated.passenger_ids,
            "title": validated.title,
            "message": validated.message,
        },
    )

    if isinstance(result, dict) and result.get("error"):
        return json.dumps(result)

    output = SendPassengerNotificationOutput(
        notifications_sent=len(validated.passenger_ids),
        success=True,
        message=(
            f"Sent notifications to "
            f"{len(validated.passenger_ids)} passenger(s)"
        ),
    )
    return output.model_dump_json()
