using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Disruption;

public interface IApprovalService
{
    /// <summary>
    /// Retrieve all rebooking proposals currently in PendingManagerApproval status
    /// with associated disruption context for the Manager Approval Workbench.
    /// </summary>
    Task<List<PendingApprovalDto>> GetPendingApprovalsAsync();

    /// <summary>
    /// Submit a Transport Manager decision (Approve/Reject/RequestRevision) on a
    /// pending rebooking proposal (BR-APPROVAL-001, BR-APPROVAL-002).
    /// Creates an immutable ApprovalDecision record and transitions proposal status.
    /// </summary>
    Task<ApprovalDecisionDto> SubmitDecisionAsync(Guid proposalId, Guid managerId, ApprovalDecisionRequestDto dto);

    /// <summary>
    /// Classify disruption impact severity (BR-APPROVAL-001):
    ///   - Timetable shift ≤15 min → Low Impact (auto-execute)
    ///   - Timetable shift >15 min OR service cancellation → High Impact (PendingManagerApproval)
    /// </summary>
    Task<string> ClassifyImpactSeverityAsync(Guid disruptionCaseId, Guid replacementServiceId);
}
