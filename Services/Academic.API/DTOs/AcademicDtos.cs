using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Academic.DTOs;

public record CreateSubjectRequest([Required] string Code, [Required] string Name, [Required] string ClassId, Guid? TeacherStaffId, string? SyllabusOutline = null);
public record SubjectSummary(Guid Id, string Code, string Name, string ClassId, Guid? TeacherStaffId, string? SyllabusOutline = null);
public record UpdateSyllabusRequest(string? SyllabusOutline);

public record TimetableSlot(string Day, int Period, Guid SubjectId, Guid? TeacherStaffId, string StartTime, string EndTime);
public record SetTimetableRequest([Required] string ClassId, [Required] string SectionId, [Required] List<TimetableSlot> Slots);
public record TimetableSummary(Guid Id, string ClassId, string SectionId, List<TimetableSlot> Slots, DateTime EffectiveFromUtc);

public record CreateHomeworkRequest(
    [Required] string ClassId, [Required] string SectionId, [Required] Guid SubjectId,
    [Required] string Title, string? Description, [Required] DateTime DueDateUtc);
public record HomeworkSummary(Guid Id, string ClassId, string SectionId, Guid SubjectId, string Title, DateTime DueDateUtc);
