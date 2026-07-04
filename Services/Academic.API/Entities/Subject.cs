using SchoolERP.Shared.Entities;

namespace SchoolERP.Academic.Entities;

public class Subject : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string ClassId { get; set; }
    public Guid? TeacherStaffId { get; set; } // cross-service ref into Staff.API, resolved over HTTP

    /// <summary>Free-text syllabus for the subject (topics/chapters/marking scheme), shown on the student portal.</summary>
    public string? SyllabusOutline { get; set; }
}
