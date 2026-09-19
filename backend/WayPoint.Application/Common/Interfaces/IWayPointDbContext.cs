using Microsoft.EntityFrameworkCore;
using WayPoint.Domain.Entities.Ai;
using WayPoint.Domain.Entities.Audit;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Disruption;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;

namespace WayPoint.Application.Common.Interfaces;

public interface IWayPointDbContext
{
    // Identity & Access
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<OperatorProfile> OperatorProfiles { get; }
    DbSet<PassengerProfile> PassengerProfiles { get; }

    // Component 1: Journey Planning & Route Catalogue (Sethum)
    DbSet<Route> Routes { get; }
    DbSet<RouteStop> RouteStops { get; }
    DbSet<BoardingPoint> BoardingPoints { get; }
    DbSet<TouristDestination> TouristDestinations { get; }
    DbSet<Service> Services { get; }
    DbSet<FareRule> FareRules { get; }
    DbSet<JourneySearch> JourneySearches { get; }
    DbSet<JourneyCandidate> JourneyCandidates { get; }

    // Component 2: Fleet, Seat & Resource Feasibility (Nuhadh)
    DbSet<Bus> Buses { get; }
    DbSet<SeatLayout> SeatLayouts { get; }
    DbSet<Seat> Seats { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<DriverAssignment> DriverAssignments { get; }
    DbSet<MaintenanceRecord> MaintenanceRecords { get; }
    DbSet<Amenity> Amenities { get; }
    DbSet<ServiceAmenity> ServiceAmenities { get; }
    DbSet<BusReview> BusReviews { get; }
    DbSet<DriverReview> DriverReviews { get; }

    // Component 3: Booking, Ticketing & Passenger Options (Mithila)
    DbSet<SeatHold> SeatHolds { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<PaymentAttempt> PaymentAttempts { get; }
    DbSet<Refund> Refunds { get; }

    // Component 4: Disruption, Rebooking & Approval (Dineth)
    DbSet<ServiceAlert> ServiceAlerts { get; }
    DbSet<DisruptionCase> DisruptionCases { get; }
    DbSet<RebookingProposal> RebookingProposals { get; }
    DbSet<ApprovalDecision> ApprovalDecisions { get; }

    // Agentic AI Persistence & Auditing
    DbSet<AiWorkflow> AiWorkflows { get; }
    DbSet<AiWorkflowStep> AiWorkflowSteps { get; }
    DbSet<AiToolCall> AiToolCalls { get; }
    DbSet<AiValidationResult> AiValidationResults { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
