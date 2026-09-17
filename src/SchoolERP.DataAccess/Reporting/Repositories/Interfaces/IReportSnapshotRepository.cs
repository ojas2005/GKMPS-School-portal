using SchoolERP.DataAccess.Reporting.Entities;

namespace SchoolERP.DataAccess.Reporting.Repositories.Interfaces;

public interface IReportSnapshotRepository
{
    Task AddAsync(ReportSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<ReportSnapshot>> FindByTypeAsync(string reportType, int page, int pageSize, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
