using SchoolERP.Business.Attendance.DTOs;
using SchoolERP.DataAccess.Attendance.Entities;
using SchoolERP.DataAccess.Attendance.Repositories.Interfaces;
using SchoolERP.Business.Attendance.Services.Interfaces;

namespace SchoolERP.Business.Attendance.Services;

/// <summary>
/// Marking attendance does three things atomically at the workflow level: guards against
/// a duplicate same-day record, inserts the daily row, and bumps the student's monthly
/// rolled-up counters via ExecuteUpdateAsync -- never a load-all-rows-then-sum approach.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendance;
    private readonly IMonthlyAttendanceSummaryRepository _summaries;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(IAttendanceRepository attendance, IMonthlyAttendanceSummaryRepository summaries, ILogger<AttendanceService> logger)
    {
        _attendance = attendance;
        _summaries = summaries;
        _logger = logger;
    }

    public async Task<AttendanceRecordSummary> MarkAsync(MarkAttendanceRequest request, Guid markedByUserId, CancellationToken ct = default)
    {
        // Duplicate-prevention guard: HasMarkedAttendanceTodayAsync mirrors HasStudentReviewed().
        if (await _attendance.HasMarkedAttendanceTodayAsync(request.StudentId, request.Date, ct))
            throw new InvalidOperationException("Attendance has already been marked for this student today.");

        var record = new AttendanceRecord
        {
            StudentId = request.StudentId,
            ClassId = request.ClassId,
            SectionId = request.SectionId,
            Date = request.Date,
            Status = request.Status,
            ArrivalTime = request.ArrivalTime,
            IsLate = request.Status == "Late",
            MarkedByUserId = markedByUserId
        };

        await _attendance.AddAsync(record, ct);
        await _attendance.SaveChangesAsync(ct);

        if (!await _summaries.ExistsAsync(request.StudentId, request.Date.Year, request.Date.Month, ct))
            await _summaries.CreateEmptyAsync(request.StudentId, request.Date.Year, request.Date.Month, ct);

        await _summaries.IncrementCounterAsync(request.StudentId, request.Date.Year, request.Date.Month, request.Status, ct);

        _logger.LogInformation("Attendance marked: student={StudentId} date={Date} status={Status}", request.StudentId, request.Date, request.Status);

        return ToSummary(record);
    }

    public async Task<IReadOnlyList<AttendanceRecordSummary>> GetForClassAsync(string classId, string sectionId, DateOnly date, CancellationToken ct = default)
    {
        var records = await _attendance.FindByClassAndDateAsync(classId, sectionId, date, ct);
        return records.Select(ToSummary).ToList();
    }

    public async Task<AttendancePercentageResponse> GetAttendancePercentageAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var pct = await _attendance.GetAttendancePercentageAsync(studentId, from, to, ct);
        return new AttendancePercentageResponse(studentId, from, to, pct);
    }

    public async Task<IReadOnlyList<AttendanceRecordSummary>> GetRecordsForStudentAsync(Guid studentId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var records = await _attendance.FindByStudentAndRangeAsync(studentId, from, to, ct);
        return records.Select(ToSummary).ToList();
    }

    private static AttendanceRecordSummary ToSummary(AttendanceRecord a) =>
        new(a.Id, a.StudentId, a.ClassId, a.SectionId, a.Date, a.Status, a.IsLate);
}
