using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Staff;
using SchoolERP.DataAccess.Staff.Entities;
using SchoolERP.DataAccess.Staff.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Staff.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly StaffDbContext _db;

    public LeaveRequestRepository(StaffDbContext db) => _db = db;

    public Task<LeaveRequest?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<IReadOnlyList<LeaveRequest>> FindByStaffIdAsync(Guid staffId, CancellationToken ct = default) =>
        _db.LeaveRequests.Where(l => l.StaffId == staffId)
            .OrderByDescending(l => l.FromDateUtc)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<LeaveRequest>)t.Result, ct);

    public Task<bool> HasOverlappingRequestAsync(Guid staffId, DateTime fromDateUtc, DateTime toDateUtc, CancellationToken ct = default) =>
        _db.LeaveRequests.AnyAsync(l =>
            l.StaffId == staffId &&
            !l.IsRejected &&
            l.FromDateUtc <= toDateUtc &&
            l.ToDateUtc >= fromDateUtc, ct);

    public async Task AddAsync(LeaveRequest request, CancellationToken ct = default) =>
        await _db.LeaveRequests.AddAsync(request, ct);

    public Task<int> MarkSubmittedAsync(Guid id, CancellationToken ct = default) =>
        _db.LeaveRequests.Where(l => l.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(l => l.IsSubmitted, true)
                .SetProperty(l => l.SubmittedAtUtc, DateTime.UtcNow), ct);

    public Task<int> DecideAsync(Guid id, bool approved, Guid decidedByUserId, string? note, CancellationToken ct = default) =>
        _db.LeaveRequests.Where(l => l.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(l => l.IsApproved, approved)
                .SetProperty(l => l.IsRejected, !approved)
                .SetProperty(l => l.DecidedAtUtc, DateTime.UtcNow)
                .SetProperty(l => l.DecidedByUserId, decidedByUserId)
                .SetProperty(l => l.DecisionNote, note), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
