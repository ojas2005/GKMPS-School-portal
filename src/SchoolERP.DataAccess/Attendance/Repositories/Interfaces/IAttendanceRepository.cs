using SchoolERP.DataAccess.Attendance.Entities;

namespace SchoolERP.DataAccess.Attendance.Repositories.Interfaces;

public interface IAttendanceRepository
{
    Task<AttendanceRecord?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Duplicate-prevention guard mirroring HasStudentReviewed(): true if this student already has a record for the given date.</summary>
    Task<bool> HasMarkedAttendanceTodayAsync(Guid studentId, DateOnly date, CancellationToken ct = default);

    Task<IReadOnlyList<AttendanceRecord>> FindByClassAndDateAsync(string classId, string sectionId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecord>> FindByStudentAndRangeAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task AddAsync(AttendanceRecord record, CancellationToken ct = default);

    /// <summary>Atomic status correction (e.g. Absent -> Excused) without loading the full row first.</summary>
    Task<int> UpdateStatusAsync(Guid recordId, string status, CancellationToken ct = default);

    /// <summary>Aggregate: average attendance % for a student over a date range, computed by PostgreSQL, not pulled into memory.</summary>
    Task<double> GetAttendancePercentageAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
