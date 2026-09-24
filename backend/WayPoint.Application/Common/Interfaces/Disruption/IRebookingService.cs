using WayPoint.Application.Features.DisruptionManagement.DTOs;

namespace WayPoint.Application.Common.Interfaces.Disruption;

public interface IRebookingService
{
    /// <summary>
    /// Create a rebooking proposal record linked to a disruption case and replacement service.
    /// Sets initial status based on impact severity classification (BR-APPROVAL-001).
    /// </summary>
    Task<RebookingProposalDto> CreateProposalAsync(CreateRebookingProposalDto dto);

    /// <summary>
    /// List all rebooking proposals for a given disruption case.
    /// </summary>
    Task<List<RebookingProposalDto>> GetProposalsByDisruptionAsync(Guid disruptionCaseId);

    /// <summary>
    /// Complex business operation: Transactional Rebooking Execution.
    /// Runs inside IDbContextTransaction (BR-REBOOK-001, BR-APPLY-002):
    ///   1. Validate proposal status is Approved (BR-APPLY-001)
    ///   2. Transfer all affected bookings to replacement service
    ///   3. Release old seat allocations
    ///   4. Lock replacement seats
    ///   5. Re-issue updated tickets
    ///   6. Create ServiceAlert notification
    ///   7. Enforce fare protection guarantee (BR-REBOOK-002)
    /// </summary>
    Task<RebookingExecutionResultDto> ExecuteApprovedRebookingAsync(Guid proposalId);
}
