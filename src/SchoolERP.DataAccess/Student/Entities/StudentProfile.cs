using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Student.Entities;

/// <summary>
/// Core admission record. LinkedUserId points back to the student's account in the Identity
/// module's database (a cross-database reference, never an EF join).
/// AdmissionNumber carries a unique index since it is the human-facing identifier
/// used on certificates, ID cards, and fee receipts.
/// </summary>
public class StudentProfile : BaseEntity
{
    public Guid LinkedUserId { get; set; }

    /// <summary>
    /// Optional Parent-role account (an Identity user) for this student's guardian. Identity
    /// resolves it at login so the parent's token carries this student's id/class claims.
    /// </summary>
    public Guid? ParentUserId { get; set; }

    public required string AdmissionNumber { get; set; }
    public required string FullName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required string Gender { get; set; }

    public required string ClassId { get; set; }
    public required string SectionId { get; set; }

    public DateTime AdmissionDateUtc { get; set; } = DateTime.UtcNow;

    public string? ParentName { get; set; }
    public string? ParentEmail { get; set; }
    public string? ParentPhone { get; set; }
    public string? Address { get; set; }

    /// <summary>Active | TransferredOut | Graduated | Suspended.</summary>
    public string Status { get; set; } = "Active";

    public DateTime? TransferredOutAtUtc { get; set; }
}
