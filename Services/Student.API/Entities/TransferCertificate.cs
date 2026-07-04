using SchoolERP.Shared.Entities;

namespace SchoolERP.Student.Entities;

/// <summary>
/// A generated Transfer Certificate. IsApproved follows the two-step submit/approve
/// workflow: a request IsSubmitted by an Admin/Teacher, then IsApproved by the
/// Principal before the PDF is generated and made available. VerificationCode is
/// exposed on the public, unauthenticated verification endpoint so a receiving
/// school can confirm authenticity without logging in.
/// </summary>
public class TransferCertificate : BaseEntity
{
    public Guid StudentId { get; set; }
    public StudentProfile? Student { get; set; }

    public required string Reason { get; set; }
    public DateTime RequestedLeavingDateUtc { get; set; }

    public bool IsSubmitted { get; set; } = false;
    public DateTime? SubmittedAtUtc { get; set; }
    public Guid? SubmittedByUserId { get; set; }

    public bool IsApproved { get; set; } = false;
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>Unique, unguessable code printed on the certificate and looked up by GET /api/transfer-certificates/verify/{code}.</summary>
    public required string VerificationCode { get; set; }

    public string? BlobPath { get; set; }
    public bool IsPdfGenerated { get; set; } = false;
}
