using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Examination.Entities;

/// <summary>
/// One row per student per exam. The (ExamId, StudentId) unique index backs the
/// HasMarksEnteredAsync() duplicate-prevention guard -- marks can only be entered once
/// per student per exam (corrections go through UpdateMarksAsync's atomic update).
/// </summary>
public class MarksEntry : BaseEntity
{
    public Guid ExamId { get; set; }
    public Exam? Exam { get; set; }

    public Guid StudentId { get; set; }
    public decimal MarksObtained { get; set; }
    public string? Grade { get; set; }
    public string? Remarks { get; set; }

    public Guid EnteredByStaffId { get; set; }
}
