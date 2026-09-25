using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Domain.Entities.Ai;
using WayPoint.Domain.Entities.Audit;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Disruption;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;

namespace WayPoint.Infrastructure.Data;

public class WayPointDbContext : DbContext, IWayPointDbContext
{
    public WayPointDbContext(DbContextOptions<WayPointDbContext> options) : base(options)
    {
    }

    // Identity & Access
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<OperatorProfile> OperatorProfiles => Set<OperatorProfile>();
    public DbSet<PassengerProfile> PassengerProfiles => Set<PassengerProfile>();

    // Component 1: Journey Planning & Route Catalogue (Sethum)
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<BoardingPoint> BoardingPoints => Set<BoardingPoint>();
    public DbSet<TouristDestination> TouristDestinations => Set<TouristDestination>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<FareRule> FareRules => Set<FareRule>();
    public DbSet<JourneySearch> JourneySearches => Set<JourneySearch>();
    public DbSet<JourneyCandidate> JourneyCandidates => Set<JourneyCandidate>();

    // Component 2: Fleet, Seat & Resource Feasibility (Nuhadh)
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<SeatLayout> SeatLayouts => Set<SeatLayout>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverAssignment> DriverAssignments => Set<DriverAssignment>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<ServiceAmenity> ServiceAmenities => Set<ServiceAmenity>();
    public DbSet<BusReview> BusReviews => Set<BusReview>();
    public DbSet<DriverReview> DriverReviews => Set<DriverReview>();

    // Component 3: Booking, Ticketing & Payments (Mithila)
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<Refund> Refunds => Set<Refund>();

    // Component 4: Disruption, Rebooking & Approval (Dineth)
    public DbSet<ServiceAlert> ServiceAlerts => Set<ServiceAlert>();
    public DbSet<DisruptionCase> DisruptionCases => Set<DisruptionCase>();
    public DbSet<RebookingProposal> RebookingProposals => Set<RebookingProposal>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();

    // Agentic AI Persistence & Auditing
    public DbSet<AiWorkflow> AiWorkflows => Set<AiWorkflow>();
    public DbSet<AiWorkflowStep> AiWorkflowSteps => Set<AiWorkflowStep>();
    public DbSet<AiToolCall> AiToolCalls => Set<AiToolCall>();
    public DbSet<AiValidationResult> AiValidationResults => Set<AiValidationResult>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Identity & RBAC
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.RoleName).IsUnique();
            entity.Property(r => r.RoleName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<OperatorProfile>(entity =>
        {
            entity.HasIndex(o => o.OperatorCode).IsUnique();
            entity.HasIndex(o => o.UserId).IsUnique();
            entity.HasOne(o => o.User)
                  .WithOne(u => u.OperatorProfile)
                  .HasForeignKey<OperatorProfile>(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PassengerProfile>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.HasOne(p => p.User)
                  .WithOne(u => u.PassengerProfile)
                  .HasForeignKey<PassengerProfile>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Component 1: Routes & Services (Sethum)
        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasIndex(r => r.RouteCode).IsUnique();
            entity.HasIndex(r => new { r.OriginCity, r.DestinationCity });
            entity.Property(r => r.RouteCode).HasMaxLength(20).IsRequired();
            entity.Property(r => r.Name).HasMaxLength(100).IsRequired();
            entity.Property(r => r.OriginCity).HasMaxLength(50).IsRequired();
            entity.Property(r => r.DestinationCity).HasMaxLength(50).IsRequired();
            entity.Property(r => r.TotalDistanceKm).HasPrecision(8, 2);
        });

        modelBuilder.Entity<RouteStop>(entity =>
        {
            entity.HasIndex(rs => new { rs.RouteId, rs.SequenceOrder }).IsUnique();
            entity.Property(rs => rs.StopName).HasMaxLength(100).IsRequired();
            entity.Property(rs => rs.DistanceFromOriginKm).HasPrecision(8, 2);
            entity.HasOne(rs => rs.Route)
                  .WithMany(r => r.Stops)
                  .HasForeignKey(rs => rs.RouteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BoardingPoint>(entity =>
        {
            entity.Property(bp => bp.PointName).HasMaxLength(100).IsRequired();
            entity.Property(bp => bp.Latitude).HasPrecision(10, 8);
            entity.Property(bp => bp.Longitude).HasPrecision(11, 8);
            entity.HasOne(bp => bp.Route)
                  .WithMany(r => r.BoardingPoints)
                  .HasForeignKey(bp => bp.RouteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TouristDestination>(entity =>
        {
            entity.Property(td => td.AttractionName).HasMaxLength(100).IsRequired();
            entity.HasOne(td => td.Route)
                  .WithMany(r => r.TouristDestinations)
                  .HasForeignKey(td => td.RouteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasIndex(s => s.ServiceCode).IsUnique();
            entity.HasIndex(s => new { s.RouteId, s.DepartureTime, s.Status });
            entity.Property(s => s.ServiceCode).HasMaxLength(30).IsRequired();
            entity.Property(s => s.BaseFare).HasPrecision(10, 2);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(s => s.Route)
                  .WithMany(r => r.Services)
                  .HasForeignKey(s => s.RouteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Bus)
                  .WithMany(b => b.Services)
                  .HasForeignKey(s => s.BusId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Driver)
                  .WithMany(d => d.Services)
                  .HasForeignKey(s => s.DriverId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FareRule>(entity =>
        {
            entity.Property(f => f.BusClass).HasConversion<string>().HasMaxLength(30);
            entity.Property(f => f.RatePerKm).HasPrecision(8, 2);
            entity.Property(f => f.ClassMultiplier).HasPrecision(4, 2);
            entity.HasOne(f => f.Service)
                  .WithMany(s => s.FareRules)
                  .HasForeignKey(f => f.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JourneyCandidate>(entity =>
        {
            entity.Property(jc => jc.TotalFare).HasPrecision(10, 2);
            entity.Property(jc => jc.MatchScore).HasPrecision(4, 3);
            entity.HasOne(jc => jc.JourneySearch)
                  .WithMany(js => js.Candidates)
                  .HasForeignKey(jc => jc.JourneySearchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Component 2: Fleet, Seats & Resources (Nuhadh)
        modelBuilder.Entity<Bus>(entity =>
        {
            entity.HasIndex(b => b.RegistrationNumber).IsUnique();
            entity.Property(b => b.RegistrationNumber).HasMaxLength(30).IsRequired();
            entity.Property(b => b.BusClass).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(b => b.SeatLayout)
                  .WithMany(sl => sl.Buses)
                  .HasForeignKey(b => b.SeatLayoutId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeatLayout>(entity =>
        {
            entity.Property(sl => sl.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasIndex(s => new { s.SeatLayoutId, s.SeatNumber }).IsUnique();
            entity.Property(s => s.SeatNumber).HasMaxLength(10).IsRequired();
            entity.Property(s => s.SeatClass).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.RowVersion).IsRowVersion();
            entity.HasOne(s => s.SeatLayout)
                  .WithMany(sl => sl.Seats)
                  .HasForeignKey(s => s.SeatLayoutId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasIndex(d => d.LicenseNumber).IsUnique();
            entity.Property(d => d.FullName).HasMaxLength(100).IsRequired();
            entity.Property(d => d.LicenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(d => d.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<DriverAssignment>(entity =>
        {
            entity.HasIndex(da => new { da.DriverId, da.ServiceId }).IsUnique();
            entity.HasOne(da => da.Driver)
                  .WithMany(d => d.Assignments)
                  .HasForeignKey(da => da.DriverId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(da => da.Service)
                  .WithMany()
                  .HasForeignKey(da => da.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.Property(m => m.Cost).HasPrecision(10, 2);
            entity.HasOne(m => m.Bus)
                  .WithMany(b => b.MaintenanceRecords)
                  .HasForeignKey(m => m.BusId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceAmenity>(entity =>
        {
            entity.HasKey(sa => new { sa.ServiceId, sa.AmenityId });
            entity.HasOne(sa => sa.Service)
                  .WithMany(s => s.ServiceAmenities)
                  .HasForeignKey(sa => sa.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(sa => sa.Amenity)
                  .WithMany(a => a.ServiceAmenities)
                  .HasForeignKey(sa => sa.AmenityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BusReview>(entity =>
        {
            entity.HasIndex(br => new { br.BusId, br.BookingId }).IsUnique(); // One review per booking per bus
            entity.Property(br => br.Rating).IsRequired();
            entity.Property(br => br.Comment).HasMaxLength(1000);
            entity.HasOne(br => br.Bus)
                  .WithMany(b => b.Reviews)
                  .HasForeignKey(br => br.BusId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(br => br.Passenger)
                  .WithMany()
                  .HasForeignKey(br => br.PassengerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(br => br.Booking)
                  .WithMany()
                  .HasForeignKey(br => br.BookingId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DriverReview>(entity =>
        {
            entity.HasIndex(dr => new { dr.DriverId, dr.BookingId }).IsUnique();
            entity.Property(dr => dr.Rating).IsRequired();
            entity.Property(dr => dr.Comment).HasMaxLength(1000);
            entity.HasOne(dr => dr.Driver)
                  .WithMany(d => d.Reviews)
                  .HasForeignKey(dr => dr.DriverId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(dr => dr.Passenger)
                  .WithMany()
                  .HasForeignKey(dr => dr.PassengerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(dr => dr.Booking)
                  .WithMany()
                  .HasForeignKey(dr => dr.BookingId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Component 3: Booking, Ticketing & Payments (Mithila)
        modelBuilder.Entity<SeatHold>(entity =>
        {
            entity.HasIndex(sh => new { sh.ServiceId, sh.SeatId, sh.HeldUntil, sh.Status });
            entity.Property(sh => sh.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(sh => sh.Service)
                  .WithMany()
                  .HasForeignKey(sh => sh.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(sh => sh.Seat)
                  .WithMany()
                  .HasForeignKey(sh => sh.SeatId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(sh => sh.Passenger)
                  .WithMany()
                  .HasForeignKey(sh => sh.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(b => b.BookingReference).IsUnique();
            entity.Property(b => b.BookingReference).HasMaxLength(20).IsRequired();
            entity.Property(b => b.TotalFareAmount).HasPrecision(10, 2);
            entity.Property(b => b.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(b => b.Passenger)
                  .WithMany()
                  .HasForeignKey(b => b.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Service)
                  .WithMany()
                  .HasForeignKey(b => b.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasIndex(t => t.BookingId).IsUnique();
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(t => t.Booking)
                  .WithOne(b => b.Ticket)
                  .HasForeignKey<Ticket>(t => t.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(10, 2);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(p => p.Booking)
                  .WithMany(b => b.PaymentAttempts)
                  .HasForeignKey(p => p.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.Property(r => r.RefundAmount).HasPrecision(10, 2);
            entity.Property(r => r.Percentage).HasPrecision(5, 2);
            entity.HasOne(r => r.Booking)
                  .WithMany(b => b.Refunds)
                  .HasForeignKey(r => r.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Component 4: Disruption, Rebooking & Approval (Dineth)
        modelBuilder.Entity<ServiceAlert>(entity =>
        {
            entity.Property(sa => sa.Title).HasMaxLength(100).IsRequired();
            entity.HasOne(sa => sa.Service)
                  .WithMany()
                  .HasForeignKey(sa => sa.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DisruptionCase>(entity =>
        {
            entity.Property(dc => dc.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(dc => dc.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(dc => dc.DisruptedService)
                  .WithMany()
                  .HasForeignKey(dc => dc.DisruptedServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RebookingProposal>(entity =>
        {
            entity.Property(rp => rp.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(rp => rp.DisruptionCase)
                  .WithMany(dc => dc.RebookingProposals)
                  .HasForeignKey(rp => rp.DisruptionCaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.ReplacementService)
                  .WithMany()
                  .HasForeignKey(rp => rp.ReplacementServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalDecision>(entity =>
        {
            entity.HasIndex(ad => ad.RebookingProposalId).IsUnique();
            entity.Property(ad => ad.Decision).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(ad => ad.RebookingProposal)
                  .WithOne(rp => rp.ApprovalDecision)
                  .HasForeignKey<ApprovalDecision>(ad => ad.RebookingProposalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ad => ad.Manager)
                  .WithMany()
                  .HasForeignKey(ad => ad.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Agentic AI Persistence (PostgreSQL JSONB columns)
        modelBuilder.Entity<AiWorkflow>(entity =>
        {
            entity.Property(w => w.Status).HasConversion<string>().HasMaxLength(30);
        });

        modelBuilder.Entity<AiWorkflowStep>(entity =>
        {
            entity.HasOne(s => s.AiWorkflow)
                  .WithMany(w => w.Steps)
                  .HasForeignKey(s => s.AiWorkflowId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiToolCall>(entity =>
        {
            entity.Property(tc => tc.ArgumentsJson).HasColumnType("jsonb");
            entity.Property(tc => tc.ResultJson).HasColumnType("jsonb");
            entity.HasOne(tc => tc.AiWorkflowStep)
                  .WithMany(s => s.ToolCalls)
                  .HasForeignKey(tc => tc.AiWorkflowStepId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiValidationResult>(entity =>
        {
            entity.HasOne(vr => vr.AiWorkflowStep)
                  .WithMany(s => s.ValidationResults)
                  .HasForeignKey(vr => vr.AiWorkflowStepId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Immutable Audit Log
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.BeforeStateJson).HasColumnType("jsonb");
            entity.Property(a => a.AfterStateJson).HasColumnType("jsonb");
            entity.Property(a => a.ActorId).HasMaxLength(100);
            entity.Property(a => a.ActionType).HasMaxLength(50);
            entity.Property(a => a.EntityName).HasMaxLength(50);
            entity.Property(a => a.EntityId).HasMaxLength(100);
        });
    }
}
