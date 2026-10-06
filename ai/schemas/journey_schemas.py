"""
WayPoint AI — Pydantic Structured Output Schemas
Student 1 (Sethum): Journey Planner / Journey Analysis Agent

All AI outputs MUST conform to these schemas before being accepted.
Enforces BR-AITOOL-002 (Tool Input DTO Validation) and BR-AIVAL-001 (Deterministic Server Assertion).
"""

from __future__ import annotations

from pydantic import BaseModel, Field


# ── Request Schemas ──────────────────────────────────────────────────────────

class JourneyAnalysisRequest(BaseModel):
    """Incoming request to the Journey Analysis Agent."""

    query: str = Field(..., min_length=3, description="Natural language travel query, e.g. 'fastest route to Ella with AC'")
    origin_city: str = Field(..., min_length=2, description="Departure city name")
    destination_city: str = Field(..., min_length=2, description="Arrival city name")
    travel_date: str = Field(..., description="Travel date in ISO format YYYY-MM-DD")
    passenger_count: int = Field(default=1, ge=1, le=20, description="Number of passengers")
    preferences: JourneyPreferences = Field(default_factory=lambda: JourneyPreferences())


class JourneyPreferences(BaseModel):
    """Passenger preference filters for journey ranking."""

    direct_only: bool = Field(default=False, description="Only return direct (non-connecting) services")
    require_ac: bool = Field(default=False, description="Require air-conditioned bus")
    max_fare: float | None = Field(default=None, ge=0, description="Maximum acceptable total fare in LKR")
    arrive_before: str | None = Field(default=None, description="Preferred arrival deadline in HH:MM format")


# ── Tool Input / Output Schemas ──────────────────────────────────────────────

class SearchRoutesInput(BaseModel):
    """Input for the SearchRoutes allow-listed tool."""

    origin_city: str
    destination_city: str


class SearchServicesInput(BaseModel):
    """Input for the SearchServices allow-listed tool."""

    route_id: str
    date: str  # ISO date YYYY-MM-DD


class GetBoardingPointsInput(BaseModel):
    """Input for the GetBoardingPoints allow-listed tool."""

    route_id: str


class TransferFeasibilityInput(BaseModel):
    """Input for the CheckTransferFeasibility tool (BR-TRANSFER-001)."""

    leg1_arrival_time: str  # ISO datetime
    leg2_departure_time: str  # ISO datetime
    transfer_hub: str


class TransferFeasibilityResult(BaseModel):
    """Output from transfer feasibility check."""

    is_feasible: bool
    transfer_minutes: int
    transfer_hub: str
    reason: str = ""


class ToolCallRecord(BaseModel):
    """Audit record for a single tool execution (maps to AiToolCall entity)."""

    tool_name: str
    arguments: dict
    result: dict
    duration_ms: int


class ValidationRecord(BaseModel):
    """Audit record for a validation check (maps to AiValidationResult entity)."""

    rule_name: str
    passed: bool
    details: str = ""


# ── Response Schemas ─────────────────────────────────────────────────────────

class JourneyRecommendation(BaseModel):
    """A single ranked journey candidate returned to the passenger."""

    service_id: str
    service_code: str = ""
    route_number: str = ""
    origin: str
    destination: str
    departure_time: str
    arrival_time: str
    total_fare: float = Field(ge=0)
    duration_minutes: int = Field(ge=0)
    is_connecting: bool = False
    match_score: float = Field(ge=0.0, le=1.0, description="Preference match score 0.00–1.00")
    available_seats: int = Field(ge=0)
    bus_class: str = ""


class JourneyAnalysisResponse(BaseModel):
    """Full response from the Journey Analysis Agent workflow."""

    workflow_id: str
    status: str = Field(description="Running | Completed | SafeFailure")
    recommendations: list[JourneyRecommendation] = []
    agent_reasoning: str = ""
    tool_calls_summary: list[ToolCallRecord] = []
    validation_summary: list[ValidationRecord] = []
    error: str | None = None

