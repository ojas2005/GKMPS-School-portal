using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Fee.Entities;

public class FeeStructure : BaseEntity
{
    public required string ClassId { get; set; }
    public required string Name { get; set; }        // "Tuition Fee Term 1", "Transport Fee 2026"
    public decimal Amount { get; set; }
    public required string AcademicYear { get; set; }
    public DateTime DueDateUtc { get; set; }
}
