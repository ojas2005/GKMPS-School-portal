using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Examination.Entities;

/// <summary>
/// AnswerKeyJson is a JSON-serialized, variable-shape structure (question -> correct
/// option/rubric) -- stored as a string column and deserialized in the service layer,
/// same pattern as the Academic module's timetable slots.
/// </summary>
public class Exam : BaseEntity
{
    public required string Name { get; set; }         // "Mid-Term 2026", "Final Exam 2026"
    public required string ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTime ExamDateUtc { get; set; }
    public int MaxMarks { get; set; }
    public int PassingMarks { get; set; }
    public string? AnswerKeyJson { get; set; }

    public bool IsResultPublished { get; set; } = false;
    public DateTime? ResultPublishedAtUtc { get; set; }
}
