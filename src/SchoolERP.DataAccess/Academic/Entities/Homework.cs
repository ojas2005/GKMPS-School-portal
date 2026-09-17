using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Academic.Entities;

public class Homework : BaseEntity
{
    public required string ClassId { get; set; }
    public required string SectionId { get; set; }
    public Guid SubjectId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime AssignedDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; set; }
    public Guid AssignedByStaffId { get; set; }
}
