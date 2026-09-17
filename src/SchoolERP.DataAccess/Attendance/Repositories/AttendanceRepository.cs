using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Attendance;
using SchoolERP.DataAccess.Attendance.Entities;
using SchoolERP.DataAccess.Attendance.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Attendance.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly AttendanceDbContext _db;

    public AttendanceRepository(AttendanceDbContext db) => _db = db;

    public Task<AttendanceRecord?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> HasMarkedAttendanceTodayAsync(Guid studentId, DateOnly date, CancellationToken ct = default) =>
        _db.AttendanceRecords.AnyAsync(a => a.StudentId == studentId && a.Date == date, ct);

    public Task<IReadOnlyList<AttendanceRecord>> FindByClassAndDateAsync(string classId, string sectionId, DateOnly date, CancellationToken ct = default) =>
        _db.AttendanceRecords
            .Where(a => a.ClassId == classId && a.SectionId == sectionId && a.Date == date)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<AttendanceRecord>)t.Result, ct);

    public Task<IReadOnlyList<AttendanceRecord>> FindByStudentAndRangeAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _db.AttendanceRecords
            .Where(a => a.StudentId == studentId && a.Date >= from && a.Date <= to)
            .OrderBy(a => a.Date)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<AttendanceRecord>)t.Result, ct);

    public async Task AddAsync(AttendanceRecord record, CancellationToken ct = default) =>
        await _db.AttendanceRecords.AddAsync(record, ct);

    public Task<int> UpdateStatusAsync(Guid recordId, string status, CancellationToken ct = default) =>
        _db.AttendanceRecords.Where(a => a.Id == recordId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, status)
                .SetProperty(a => a.UpdatedAtUtc, DateTime.UtcNow), ct);

    public async Task<double> GetAttendancePercentageAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Aggregate query executed in PostgreSQL via AverageAsync -- no row materialization in app memory.
        var records = _db.AttendanceRecords.Where(a => a.StudentId == studentId && a.Date >= from && a.Date <= to);

        var total = await records.CountAsync(ct);
        if (total == 0) return 0d;

        var presentCount = await records.CountAsync(a => a.Status == "Present" || a.Status == "Late", ct);
        return Math.Round(presentCount * 100.0 / total, 2);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
