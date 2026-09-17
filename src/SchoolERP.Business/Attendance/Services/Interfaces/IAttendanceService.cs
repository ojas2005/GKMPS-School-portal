using SchoolERP.Business.Attendance.DTOs;

namespace SchoolERP.Business.Attendance.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceRecordSummary> MarkAsync(MarkAttendanceRequest request, Guid markedByUserId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecordSummary>> GetForClassAsync(string classId, string sectionId, DateOnly date, CancellationToken ct = default);
    Task<AttendancePercentageResponse> GetAttendancePercentageAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecordSummary>> GetRecordsForStudentAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
