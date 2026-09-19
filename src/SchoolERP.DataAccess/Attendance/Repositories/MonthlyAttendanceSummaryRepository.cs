using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Attendance;
using SchoolERP.DataAccess.Attendance.Entities;
using SchoolERP.DataAccess.Attendance.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Attendance.Repositories;

public class MonthlyAttendanceSummaryRepository : IMonthlyAttendanceSummaryRepository
{
    private readonly AttendanceDbContext _db;

    public MonthlyAttendanceSummaryRepository(AttendanceDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid studentId, int year, int month, CancellationToken ct = default) =>
        _db.MonthlySummaries.AnyAsync(m => m.StudentId == studentId && m.Year == year && m.Month == month, ct);

    public async Task CreateEmptyAsync(Guid studentId, int year, int month, CancellationToken ct = default)
    {
        await _db.MonthlySummaries.AddAsync(new MonthlyAttendanceSummary
        {
            StudentId = studentId,
            Year = year,
            Month = month
        }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> IncrementCounterAsync(Guid studentId, int year, int month, string status, CancellationToken ct = default)
    {
        var query = _db.MonthlySummaries.Where(m => m.StudentId == studentId && m.Year == year && m.Month == month);

        // Atomic increment -- ExecuteUpdateAsync bumps the counter in the database in one round trip.
        return status switch
        {
            "Present" => query.ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.PresentCount, m => m.PresentCount + 1)
                .SetProperty(m => m.TotalMarkedDays, m => m.TotalMarkedDays + 1)
                .SetProperty(m => m.UpdatedAtUtc, DateTime.UtcNow), ct),
            "Absent" => query.ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.AbsentCount, m => m.AbsentCount + 1)
                .SetProperty(m => m.TotalMarkedDays, m => m.TotalMarkedDays + 1)
                .SetProperty(m => m.UpdatedAtUtc, DateTime.UtcNow), ct),
            "Late" => query.ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.LateCount, m => m.LateCount + 1)
                .SetProperty(m => m.PresentCount, m => m.PresentCount + 1)
                .SetProperty(m => m.TotalMarkedDays, m => m.TotalMarkedDays + 1)
                .SetProperty(m => m.UpdatedAtUtc, DateTime.UtcNow), ct),
            _ => query.ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.TotalMarkedDays, m => m.TotalMarkedDays + 1)
                .SetProperty(m => m.UpdatedAtUtc, DateTime.UtcNow), ct)
        };
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
