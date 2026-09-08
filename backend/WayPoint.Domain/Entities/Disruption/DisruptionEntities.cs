using WayPoint.Domain.Common;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;

namespace WayPoint.Domain.Entities.Disruption;

public class ServiceAlert : BaseEntity
{
    public Guid ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Service Service { get; set; } = null!;
}

public class DisruptionCase : BaseEntity
{
    public Guid DisruptedServiceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DisruptionSeverity Severity { get; set; } = DisruptionSeverity.Major;
    public int AffectedPassengerCount { get; set; }
    public DisruptionStatus Status { get; set; } = DisruptionStatus.Logged;

    // Navigation properties
    public Service DisruptedService { get; set; } = null!;
    public ICollection<RebookingProposal> RebookingProposals { get; set; } = new List<RebookingProposal>();
}

public class RebookingProposal : BaseEntity
{
    public Guid DisruptionCaseId { get; set; }
    public Guid ReplacementServiceId { get; set; }
    public string ProposedByAgent { get; set; } = "ResourceBookingAgent";
    public RebookingStatus Status { get; set; } = RebookingStatus.PendingManagerApproval;

    // Navigation properties
    public DisruptionCase DisruptionCase { get; set; } = null!;
    public Service ReplacementService { get; set; } = null!;
    public ApprovalDecision? ApprovalDecision { get; set; }
}

public class ApprovalDecision : BaseEntity
{
    public Guid RebookingProposalId { get; set; }
    public Guid ManagerId { get; set; }
    public ApprovalDecisionType Decision { get; set; } = ApprovalDecisionType.Approve;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public RebookingProposal RebookingProposal { get; set; } = null!;
    public User Manager { get; set; } = null!;
}
