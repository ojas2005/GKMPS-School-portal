using SchoolERP.Staff.Entities;

namespace SchoolERP.Staff.Repositories.Interfaces;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveRequest>> FindByStaffIdAsync(Guid staffId, CancellationToken ct = default);

    /// <summary>Duplicate-prevention guard: overlapping date range already requested for this staff member.</summary>
    Task<bool> HasOverlappingRequestAsync(Guid staffId, DateTime fromDateUtc, DateTime toDateUtc, CancellationToken ct = default);

    Task AddAsync(LeaveRequest request, CancellationToken ct = default);

    Task<int> MarkSubmittedAsync(Guid id, CancellationToken ct = default);
    Task<int> DecideAsync(Guid id, bool approved, Guid decidedByUserId, string? note, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
