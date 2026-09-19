using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Academic.Entities;

public class Subject : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string ClassId { get; set; }
    public Guid? TeacherStaffId { get; set; } // reference into the Staff module's database (no EF join)

    /// <summary>Free-text syllabus for the subject (topics/chapters/marking scheme), shown on the student portal.</summary>
    public string? SyllabusOutline { get; set; }
}
