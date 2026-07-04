using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Attendance.DTOs;

public record MarkAttendanceRequest(
    [Required] Guid StudentId, [Required] string ClassId, [Required] string SectionId,
    [Required] DateOnly Date, [Required] string Status, TimeOnly? ArrivalTime);

public record AttendanceRecordSummary(
    Guid Id, Guid StudentId, string ClassId, string SectionId, DateOnly Date, string Status, bool IsLate);

public record AttendancePercentageResponse(Guid StudentId, DateOnly From, DateOnly To, double Percentage);
