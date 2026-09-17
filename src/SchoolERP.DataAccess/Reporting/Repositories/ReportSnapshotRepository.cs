using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Reporting;
using SchoolERP.DataAccess.Reporting.Entities;
using SchoolERP.DataAccess.Reporting.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Reporting.Repositories;

public class ReportSnapshotRepository : IReportSnapshotRepository
{
    private readonly ReportingDbContext _db;

    public ReportSnapshotRepository(ReportingDbContext db) => _db = db;

    public async Task AddAsync(ReportSnapshot snapshot, CancellationToken ct = default) =>
        await _db.ReportSnapshots.AddAsync(snapshot, ct);

    public Task<IReadOnlyList<ReportSnapshot>> FindByTypeAsync(string reportType, int page, int pageSize, CancellationToken ct = default) =>
        _db.ReportSnapshots.Where(r => r.ReportType == reportType)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<ReportSnapshot>)t.Result, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
