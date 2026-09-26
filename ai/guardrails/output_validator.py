"""
WayPoint AI — Output Validator Guardrail (FR-AI-003).

Deterministic business-rule validators that override LLM outputs
when they violate known constraints. These are pure functions with
no side effects — they return corrected values or validation results.

Validators:
- validate_uuid: Ensures UUID format
- validate_seat_counts: Arithmetic check on seat matrix totals
- validate_transfer_window: BR-TRANSFER-001 (>= 20 min)
- validate_bus_capacity: BR-RESOURCE-001 (capacity >= required)
- validate_driver_rest: BR-RESOURCE-002 (>= 8 hours rest)
- validate_impact_classification: BR-APPROVAL-001 deterministic override
"""

import logging
import re
from datetime import datetime

logger = logging.getLogger("waypoint.ai.guardrails.output_validator")

# UUID v4 regex pattern
_UUID_PATTERN = re.compile(
    r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
    re.IGNORECASE,
)


# ---------------------------------------------------------------------------
# Individual Validators
# ---------------------------------------------------------------------------


def validate_uuid(value: str, field_name: str = "id") -> tuple[bool, str]:
    """
    Validate that a string is a valid UUID v4 format.

    Args:
        value: The string to validate.
        field_name: Name of the field for error messages.

    Returns:
        Tuple of (is_valid, error_message).
    """
    if not value:
        return False, f"{field_name} is empty"

    if _UUID_PATTERN.match(value):
        return True, ""

    return False, f"{field_name} is not a valid UUID: '{value}'"


def validate_seat_counts(
    total_seats: int,
    available_seats: int,
    held_seats: int,
    booked_seats: int,
) -> tuple[bool, str]:
    """
    Validate that seat count arithmetic is consistent.

    Rule: available + held + booked == total

    Args:
        total_seats: Total seats on the bus.
        available_seats: Number of available seats.
        held_seats: Number of held seats.
        booked_seats: Number of booked seats.

    Returns:
        Tuple of (is_valid, error_message).
    """
    computed_total = available_seats + held_seats + booked_seats
    if computed_total != total_seats:
        return (
            False,
            f"Seat count mismatch: {available_seats} available + "
            f"{held_seats} held + {booked_seats} booked = "
            f"{computed_total}, but total_seats = {total_seats}",
        )
    return True, ""


def validate_transfer_window(
    leg1_arrival: str,
    leg2_departure: str,
    min_minutes: float = 20.0,
) -> tuple[bool, float, str]:
    """
    Validate transfer window meets minimum requirement (BR-TRANSFER-001).

    Args:
        leg1_arrival: Arrival time of leg 1 (ISO 8601).
        leg2_departure: Departure time of leg 2 (ISO 8601).
        min_minutes: Minimum required transfer window in minutes.

    Returns:
        Tuple of (is_feasible, actual_minutes, reason).
    """
    try:
        arrival = datetime.fromisoformat(leg1_arrival)
        departure = datetime.fromisoformat(leg2_departure)
        transfer_minutes = (departure - arrival).total_seconds() / 60.0
    except (ValueError, TypeError) as exc:
        return False, 0.0, f"Invalid datetime format: {exc}"

    if transfer_minutes < min_minutes:
        return (
            False,
            transfer_minutes,
            f"Insufficient transfer window: {transfer_minutes:.0f} min "
            f"(minimum {min_minutes:.0f} required, BR-TRANSFER-001)",
        )

    return (
        True,
        transfer_minutes,
        f"Transfer window OK: {transfer_minutes:.0f} min "
        f"(>= {min_minutes:.0f} required)",
    )


def validate_bus_capacity(
    available_seats: int,
    required_seats: int,
) -> tuple[bool, str]:
    """
    Validate bus has sufficient capacity (BR-RESOURCE-001).

    Args:
        available_seats: Number of seats currently available.
        required_seats: Number of seats needed for rebooking.

    Returns:
        Tuple of (is_valid, reason).
    """
    if available_seats >= required_seats:
        return (
            True,
            f"Sufficient capacity: {available_seats} available "
            f">= {required_seats} required",
        )

    return (
        False,
        f"Insufficient capacity: {available_seats} available "
        f"< {required_seats} required (BR-RESOURCE-001)",
    )


def validate_driver_rest(
    last_shift_end: str,
    next_shift_start: str,
    min_rest_hours: float = 8.0,
) -> tuple[bool, float, str]:
    """
    Validate driver has had sufficient rest (BR-RESOURCE-002).

    Args:
        last_shift_end: When the driver's last shift ended (ISO 8601).
        next_shift_start: When the proposed shift starts (ISO 8601).
        min_rest_hours: Minimum rest hours required.

    Returns:
        Tuple of (is_valid, actual_rest_hours, reason).
    """
    try:
        end = datetime.fromisoformat(last_shift_end)
        start = datetime.fromisoformat(next_shift_start)
        rest_hours = (start - end).total_seconds() / 3600.0
    except (ValueError, TypeError) as exc:
        return False, 0.0, f"Invalid datetime format: {exc}"

    if rest_hours < min_rest_hours:
        return (
            False,
            rest_hours,
            f"Insufficient rest: {rest_hours:.1f}h "
            f"(minimum {min_rest_hours:.0f}h required, BR-RESOURCE-002)",
        )

    return (
        True,
        rest_hours,
        f"Rest period OK: {rest_hours:.1f}h "
        f"(>= {min_rest_hours:.0f}h required)",
    )


def validate_impact_classification(
    llm_classification: str,
    affected_passenger_count: int = 0,
    delay_minutes: float = 0,
    *,
    is_cancellation: bool = False,
) -> str:
    """
    Deterministic override of LLM impact classification (FR-AI-003).

    The LLM may suggest 'Low' impact, but business rules require 'High'
    classification if ANY of these triggers are met (BR-APPROVAL-001):
    - Service cancellation (always High)
    - Timetable shift > 15 minutes
    - Affected passenger count > 0 in a cancellation scenario

    This function ALWAYS takes precedence over the LLM's classification.

    Args:
        llm_classification: The LLM's suggested classification.
        affected_passenger_count: Number of affected passengers.
        delay_minutes: Total delay in minutes.
        is_cancellation: Whether this is a service cancellation.

    Returns:
        'Low' or 'High' — the authoritative classification.
    """
    # BR-APPROVAL-001: cancellation is always High-Impact
    if is_cancellation:
        logger.info(
            "Impact override: cancellation → High "
            "(LLM suggested '%s')", llm_classification
        )
        return "High"

    # Delay > 15 minutes is High-Impact
    if delay_minutes > 15:
        logger.info(
            "Impact override: delay %.0f min > 15 → High "
            "(LLM suggested '%s')", delay_minutes, llm_classification
        )
        return "High"

    # Otherwise trust the LLM (bounded to 'Low' or 'High')
    if llm_classification in ("Low", "High"):
        return llm_classification

    # Unknown classification defaults to Low
    logger.warning(
        "Unknown LLM classification '%s', defaulting to 'Low'",
        llm_classification,
    )
    return "Low"


# ---------------------------------------------------------------------------
# Batch Validator
# ---------------------------------------------------------------------------


def run_all_validators(
    agent_output: dict,
    workflow_type: str = "",
) -> list[dict]:
    """
    Run all applicable validators on an agent's output dict.

    Returns a list of validation result dicts, each with:
    - rule_name: str
    - passed: bool
    - validation_details: str

    This is used by the safety agent to produce AiValidationResult records.
    """
    results: list[dict] = []

    # Seat count validation (if present in output)
    if all(
        k in agent_output
        for k in ("total_seats", "available_seats", "held_seats", "booked_seats")
    ):
        passed, detail = validate_seat_counts(
            agent_output["total_seats"],
            agent_output["available_seats"],
            agent_output["held_seats"],
            agent_output["booked_seats"],
        )
        results.append(
            {
                "rule_name": "SeatCountArithmetic",
                "passed": passed,
                "validation_details": detail if not passed else "OK",
            }
        )

    # Impact classification (if present)
    if "impact_classification" in agent_output or "impactClassification" in agent_output:
        llm_class = agent_output.get(
            "impact_classification",
            agent_output.get("impactClassification", "Low"),
        )
        is_cancel = workflow_type == "disruption_rebooking"
        corrected = validate_impact_classification(
            llm_class,
            agent_output.get("affected_passenger_count", 0),
            agent_output.get("total_delay_minutes", 0),
            is_cancellation=is_cancel,
        )
        results.append(
            {
                "rule_name": "ImpactClassificationOverride",
                "passed": corrected == llm_class,
                "validation_details": (
                    f"LLM: {llm_class}, Corrected: {corrected}"
                ),
            }
        )

    return results
