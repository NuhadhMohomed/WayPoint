namespace WayPoint.Domain.Enums;

public enum UserRoleType
{
    Passenger,
    Operator,
    TransportManager,
    Admin
}

public enum ServiceStatus
{
    Scheduled,
    Boarding,
    InTransit,
    Completed,
    Disrupted,
    Cancelled
}

public enum BusClass
{
    Standard,
    SemiLuxury,
    Luxury,
    SuperLuxury
}

public enum SeatStatus
{
    Available,
    Held,
    Booked,
    Blocked
}

public enum SeatClass
{
    Standard,
    Window,
    Aisle,
    FrontRow,
    VIP
}

public enum SeatHoldStatus
{
    Held,
    Expired,
    ConvertedToBooking
}

public enum BookingStatus
{
    PendingPayment,
    Confirmed,
    Rebooked,
    Cancelled
}

public enum TicketStatus
{
    Issued,
    Boarded,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failed,
    Refunded
}

public enum DisruptionSeverity
{
    Minor,
    Major,
    Critical
}

public enum DisruptionStatus
{
    Logged,
    Analyzing,
    PendingApproval,
    Resolved,
    Cancelled
}

public enum RebookingStatus
{
    Proposed,
    PendingManagerApproval,
    Approved,
    Rejected,
    Executed
}

public enum ApprovalDecisionType
{
    Approve,
    Reject,
    RequestRevision
}

public enum AiWorkflowStatus
{
    Running,
    PendingManagerApproval,
    Completed,
    SafeFailure
}

/// <summary>
/// Operational status of a driver in the fleet (M-04, Database Design Table 7).
/// </summary>
public enum DriverStatus
{
    Active,
    OnLeave,
    Suspended,
    Retired
}

/// <summary>
/// Classification of a journey search candidate result (L-04).
/// </summary>
public enum JourneyCandidateType
{
    Direct,
    Connecting
}
