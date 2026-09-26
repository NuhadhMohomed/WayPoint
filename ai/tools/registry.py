"""
WayPoint AI — Allow-Listed Tool Registry (BR-AITOOL-001).

Central registry enforcing the 10 mandatory allow-listed tools.
Any tool invocation not in this registry is rejected with
ToolNotAllowedError.

Also exports agent-specific tool subsets for LangGraph binding.
"""

from typing import Callable

from tools.journey_tools import (
    search_routes,
    get_boarding_points,
    check_transfer_feasibility,
)
from tools.resource_tools import check_seat_availability
from tools.booking_tools import (
    calculate_fare_difference,
    send_passenger_notification,
)
from tools.disruption_tools import (
    create_rebooking_proposal,
    calculate_passenger_impact,
    request_manager_approval,
    apply_approved_operational_change,
)


class ToolNotAllowedError(Exception):
    """Raised when an agent attempts to use a tool not in the allow-list."""

    pass


# ---------------------------------------------------------------------------
# The 10 Mandatory Allow-Listed Tools (BR-AITOOL-001)
# ---------------------------------------------------------------------------
ALLOWED_TOOLS: dict[str, Callable] = {
    "SearchRoutes": search_routes,
    "GetBoardingPoints": get_boarding_points,
    "CheckTransferFeasibility": check_transfer_feasibility,
    "CheckSeatAvailability": check_seat_availability,
    "CalculateFareDifference": calculate_fare_difference,
    "CreateRebookingProposal": create_rebooking_proposal,
    "CalculatePassengerImpact": calculate_passenger_impact,
    "RequestManagerApproval": request_manager_approval,
    "ApplyApprovedOperationalChange": apply_approved_operational_change,
    "SendPassengerNotification": send_passenger_notification,
}

# All LangChain tool objects for binding
ALL_TOOLS = list(ALLOWED_TOOLS.values())

# ---------------------------------------------------------------------------
# Agent-Specific Tool Subsets
# Each agent may ONLY bind these tools (enforced via prompt + registry)
# ---------------------------------------------------------------------------
JOURNEY_AGENT_TOOLS = [
    search_routes,
    get_boarding_points,
    check_transfer_feasibility,
]

RESOURCE_AGENT_TOOLS = [
    check_seat_availability,
    check_transfer_feasibility,
]

BOOKING_AGENT_TOOLS = [
    calculate_fare_difference,
    send_passenger_notification,
]

SAFETY_AGENT_TOOLS = [
    create_rebooking_proposal,
    calculate_passenger_impact,
    request_manager_approval,
    apply_approved_operational_change,
]


def get_tool(name: str) -> Callable:
    """
    Retrieve an allow-listed tool by name.

    Args:
        name: The tool name (must match one of the 10 allowed tools).

    Returns:
        The tool callable.

    Raises:
        ToolNotAllowedError: If the tool name is not in the allow-list.
    """
    if name not in ALLOWED_TOOLS:
        raise ToolNotAllowedError(
            f"Tool '{name}' is not in the allow-list. "
            f"Only these tools are permitted: "
            f"{sorted(ALLOWED_TOOLS.keys())}"
        )
    return ALLOWED_TOOLS[name]


def is_allowed(name: str) -> bool:
    """Check whether a tool name is in the allow-list."""
    return name in ALLOWED_TOOLS
