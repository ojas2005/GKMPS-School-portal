using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Student.Entities;

/// <summary>
/// Core admission record. Students have no login of their own: the parent's login
/// (ParentUserId, an Identity user) is how the family sees attendance, marks and fees.
/// LinkedUserId is kept only for students admitted when they still had one.
/// AdmissionNumber carries a unique index since it is the human-facing identifier
/// used on certificates, ID cards, and fee receipts.
/// </summary>
public class StudentProfile : BaseEntity
{
    public Guid? LinkedUserId { get; set; }

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

    // Both parents are recorded at admission (name and mobile number required).
    public string? FatherName { get; set; }
    public string? FatherPhone { get; set; }
    public string? FatherEmail { get; set; }
    public string? MotherName { get; set; }
    public string? MotherPhone { get; set; }
    public string? MotherEmail { get; set; }

    /// <summary>The single guardian contact used before father and mother were recorded separately.</summary>
    public string? ParentName { get; set; }
    public string? ParentEmail { get; set; }
    public string? ParentPhone { get; set; }
    public string? Address { get; set; }

    /// <summary>One of <see cref="StudentStatuses"/>.</summary>
    public string Status { get; set; } = StudentStatuses.Active;

    public DateTime? TransferredOutAtUtc { get; set; }

    /// <summary>When and why the school removed the student (status Removed).</summary>
    public DateTime? RemovedAtUtc { get; set; }
    public string? RemovalReason { get; set; }
}

public static class StudentStatuses
{
    public const string Active = "Active";
    /// <summary>Left the school: their transfer certificate was approved.</summary>
    public const string TransferredOut = "TransferredOut";
    /// <summary>Taken off the rolls by the school without a transfer certificate.</summary>
    public const string Removed = "Removed";
    /// <summary>Left, and their personal data has since been erased.</summary>
    public const string Erased = "Erased";
}
