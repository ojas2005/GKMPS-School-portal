using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Academic.DTOs;

public record CreateSubjectRequest([Required] string Code, [Required] string Name, [Required] string ClassId, Guid? TeacherStaffId, string? SyllabusOutline = null);
public record SubjectSummary(Guid Id, string Code, string Name, string ClassId, Guid? TeacherStaffId, string? SyllabusOutline = null);
public record UpdateSyllabusRequest(string? SyllabusOutline);

// SubjectId is nullable: the auto-generated timetable is teacher+subject-name based and
// doesn't require a pre-existing Subject row, while the manual editor still passes a SubjectId.
public record TimetableSlot(string Day, int Period, Guid? SubjectId, string? SubjectName, Guid? TeacherStaffId, string StartTime, string EndTime);
public record SetTimetableRequest([Required] string ClassId, [Required] string SectionId, [Required] List<TimetableSlot> Slots);
public record TimetableSummary(Guid Id, string ClassId, string SectionId, List<TimetableSlot> Slots, DateTime EffectiveFromUtc);

// ---- Auto-generated timetable: owner config + generation ----

// One teacher's role: the subject they teach, their daily period cap, and the classes they cover.
public record TeacherAssignmentDto(Guid StaffId, string SubjectName, int MaxPeriodsPerDay, List<string> ClassIds);

// A pre-primary class (PG/Nursery/LKG/UKG) is covered entirely by its class teacher.
public record PrePrimaryAssignmentDto(string ClassId, Guid ClassTeacherStaffId, string? SubjectName);

public record SaveScheduleConfigRequest(
    List<TeacherAssignmentDto> Junior,
    List<TeacherAssignmentDto> Senior,
    List<PrePrimaryAssignmentDto> PrePrimary);

public record ScheduleConfigResponse(
    List<TeacherAssignmentDto> Junior,
    List<TeacherAssignmentDto> Senior,
    List<PrePrimaryAssignmentDto> PrePrimary);

public record GenerateTimetableResult(List<string> GeneratedClassIds, List<string> Warnings);

public record TeacherSlot(string Day, int Period, string ClassId, string SectionId, string? SubjectName, string StartTime, string EndTime);
public record TeacherTimetableResponse(Guid StaffId, List<TeacherSlot> Slots);

public record CreateHomeworkRequest(
    [Required] string ClassId, [Required] string SectionId, [Required] Guid SubjectId,
    [Required] string Title, string? Description, [Required] DateTime DueDateUtc);
public record HomeworkSummary(Guid Id, string ClassId, string SectionId, Guid SubjectId, string Title, DateTime DueDateUtc);
