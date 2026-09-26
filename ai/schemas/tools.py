"""
WayPoint AI — Tool Input/Output Pydantic Schemas.

Defines strongly-typed schemas for the 10 allow-listed tools (BR-AITOOL-001).
Each input model maps to a backend API endpoint DTO from api-design.md.
All tool invocations must pass schema validation before execution (BR-AITOOL-002).

Backend DTO naming convention: camelCase JSON ↔ snake_case Python.
"""

from pydantic import BaseModel, Field


# ===========================================================================
# Tool 1: SearchRoutes
# Backend: GET /api/v1/routes?originCity=...&destinationCity=...
# Response: PaginatedResponseDto<RouteSummaryDto>
# Owner: Student 1 (Sethum)
# ===========================================================================

class SearchRoutesInput(BaseModel):
    """Input schema for the SearchRoutes tool."""

    origin_city: str = Field(..., description="Origin city name (e.g. 'Colombo')")
    destination_city: str = Field(
        ..., description="Destination city name (e.g. 'Ella')"
    )
    travel_date: str | None = Field(
        None, description="Travel date in YYYY-MM-DD format"
    )


class RouteSummary(BaseModel):
    """A single route returned by SearchRoutes."""

    id: str
    route_code: str
    name: str
    origin_city: str
    destination_city: str
    total_distance_km: float
    is_active: bool = True


class SearchRoutesOutput(BaseModel):
    """Output schema for the SearchRoutes tool."""

    routes: list[RouteSummary] = Field(default_factory=list)
    total_count: int = 0


# ===========================================================================
# Tool 2: GetBoardingPoints
# Backend: GET /api/v1/boarding-points?routeId=...
# Response: List<BoardingPointDto>
# Owner: Student 1 (Sethum)
# ===========================================================================

class GetBoardingPointsInput(BaseModel):
    """Input schema for the GetBoardingPoints tool."""

    route_id: str = Field(..., description="UUID of the route")


class BoardingPointInfo(BaseModel):
    """A single boarding/drop-off point."""

    id: str
    point_name: str
    landmark: str | None = None
    latitude: float | None = None
    longitude: float | None = None


class GetBoardingPointsOutput(BaseModel):
    """Output schema for the GetBoardingPoints tool."""

    boarding_points: list[BoardingPointInfo] = Field(default_factory=list)


# ===========================================================================
# Tool 3: CheckTransferFeasibility
# Validates BR-TRANSFER-001: minimum 20-minute transfer window
# Owner: Student 1 (Sethum) / Student 2 (Nuhadh)
# ===========================================================================

class CheckTransferFeasibilityInput(BaseModel):
    """Input schema for the CheckTransferFeasibility tool."""

    leg1_arrival_time: str = Field(
        ..., description="Arrival time of leg 1 in ISO 8601 format"
    )
    leg2_departure_time: str = Field(
        ..., description="Departure time of leg 2 in ISO 8601 format"
    )
    transfer_stop_id: str | None = Field(
        None,
        description="UUID of the transfer hub stop (leg1 dest == leg2 origin)",
    )


class CheckTransferFeasibilityOutput(BaseModel):
    """Output schema for the CheckTransferFeasibility tool."""

    is_feasible: bool
    transfer_minutes: float
    minimum_required_minutes: float = 20.0
    reason: str = ""


# ===========================================================================
# Tool 4: CheckSeatAvailability
# Backend: GET /api/v1/services/{serviceId}/seats
# Response: SeatMapResponseDto (matches ServiceSeatMatrixDto in FleetDtos.cs)
# Owner: Student 2 (Nuhadh)
# ===========================================================================

class CheckSeatAvailabilityInput(BaseModel):
    """Input schema for the CheckSeatAvailability tool."""

    service_id: str = Field(..., description="UUID of the scheduled service")


class SeatInfo(BaseModel):
    """Status of a single seat — mirrors SeatDto in FleetDtos.cs."""

    seat_id: str
    seat_number: str
    row_index: int
    column_index: int
    seat_class: str  # Standard | Window | Aisle | FrontRow | VIP
    status: str  # Available | Held | Booked


class CheckSeatAvailabilityOutput(BaseModel):
    """Output schema — mirrors ServiceSeatMatrixDto."""

    service_id: str
    service_code: str = ""
    total_seats: int = 0
    available_seats: int = 0
    held_seats: int = 0
    booked_seats: int = 0
    seats: list[SeatInfo] = Field(default_factory=list)


# ===========================================================================
# Tool 5: CalculateFareDifference
# Compares baseFare between two services via GET /api/v1/services/{id}
# Owner: Student 3 (Mithila)
# ===========================================================================

class CalculateFareDifferenceInput(BaseModel):
    """Input schema for the CalculateFareDifference tool."""

    original_service_id: str = Field(
        ..., description="UUID of the original disrupted service"
    )
    replacement_service_id: str = Field(
        ..., description="UUID of the proposed replacement service"
    )


class CalculateFareDifferenceOutput(BaseModel):
    """Output schema for the CalculateFareDifference tool."""

    original_fare: float = 0.0
    replacement_fare: float = 0.0
    fare_difference: float = 0.0
    passenger_pays_extra: bool = False
    note: str = ""


# ===========================================================================
# Tool 6: CreateRebookingProposal
# Backend: POST /api/v1/rebooking/generate-proposal
# Request:  GenerateRebookingProposalDto { disruptionCaseId }
# Response: RebookingProposalDto { proposalId, ... status }
# Owner: Student 4 (Dineth)
# ===========================================================================

class CreateRebookingProposalInput(BaseModel):
    """Input schema for the CreateRebookingProposal tool."""

    disruption_case_id: str = Field(
        ..., description="UUID of the disruption case"
    )
    replacement_service_id: str = Field(
        ..., description="UUID of the replacement service"
    )
    proposed_by_agent: str = Field(
        default="ValidationSafetyAgent",
        description="Name of the agent proposing the rebooking",
    )


class CreateRebookingProposalOutput(BaseModel):
    """Output schema — mirrors RebookingProposalDto."""

    proposal_id: str
    disruption_case_id: str
    replacement_service_id: str
    status: str = "PendingManagerApproval"


# ===========================================================================
# Tool 7: CalculatePassengerImpact
# Backend: GET /api/v1/disruptions/{id}
# Response: DisruptionDetailDto (affectedPassengerCount, etc.)
# Owner: Student 4 (Dineth)
# ===========================================================================

class CalculatePassengerImpactInput(BaseModel):
    """Input schema for the CalculatePassengerImpact tool."""

    disrupted_service_id: str = Field(
        ..., description="UUID of the disrupted service"
    )


class CalculatePassengerImpactOutput(BaseModel):
    """Output schema for the CalculatePassengerImpact tool."""

    affected_passenger_count: int = 0
    total_delay_minutes: float = 0.0
    net_fare_delta: float = 0.0
    affected_booking_ids: list[str] = Field(default_factory=list)


# ===========================================================================
# Tool 8: RequestManagerApproval
# Backend: POST /api/v1/approvals/{id}/decision (internal trigger)
# Transitions workflow → PendingManagerApproval (BR-APPROVAL-001)
# Owner: Student 4 (Dineth)
# ===========================================================================

class RequestManagerApprovalInput(BaseModel):
    """Input schema for the RequestManagerApproval tool."""

    rebooking_proposal_id: str = Field(
        ..., description="UUID of the rebooking proposal requiring approval"
    )
    impact_classification: str = Field(
        default="High", description="Impact level: 'Low' or 'High'"
    )
    justification: str = Field(
        default="", description="Reason for requesting manager approval"
    )


class RequestManagerApprovalOutput(BaseModel):
    """Output schema for the RequestManagerApproval tool."""

    proposal_id: str
    new_status: str = "PendingManagerApproval"
    message: str = ""


# ===========================================================================
# Tool 9: ApplyApprovedOperationalChange
# Backend: POST /api/v1/approvals/{id}/decision (after Approve decision)
# Response: ApprovalDecisionResultDto { affectedPassengersRebooked }
# Owner: Student 4 (Dineth)
# ===========================================================================

class ApplyApprovedChangeInput(BaseModel):
    """Input schema for the ApplyApprovedOperationalChange tool."""

    rebooking_proposal_id: str = Field(
        ..., description="UUID of the approved rebooking proposal"
    )


class ApplyApprovedChangeOutput(BaseModel):
    """Output schema for the ApplyApprovedOperationalChange tool."""

    success: bool
    passengers_rebooked: int = 0
    message: str = ""


# ===========================================================================
# Tool 10: SendPassengerNotification
# Backend: POST /api/v1/notifications (internal dispatch)
# Owner: Student 3 (Mithila)
# ===========================================================================

class SendPassengerNotificationInput(BaseModel):
    """Input schema for the SendPassengerNotification tool."""

    passenger_ids: list[str] = Field(
        ..., description="List of passenger UUIDs to notify"
    )
    title: str = Field(..., description="Notification title")
    message: str = Field(..., description="Notification body text")


class SendPassengerNotificationOutput(BaseModel):
    """Output schema for the SendPassengerNotification tool."""

    notifications_sent: int = 0
    success: bool = True
    message: str = ""
