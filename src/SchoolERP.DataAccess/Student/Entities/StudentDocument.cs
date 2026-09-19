using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Student.Entities;

/// <summary>
/// Metadata for an uploaded or system-generated document (birth certificate, previous
/// school records, photo, generated PDFs). The binary itself lives in Azure Blob Storage;
/// this row stores only the blob path -- callers always get a time-limited SAS URL,
/// never a permanent public link.
/// </summary>
public class StudentDocument : BaseEntity
{
    public Guid StudentId { get; set; }
    public StudentProfile? Student { get; set; }

    /// <summary>Photo | BirthCertificate | PreviousSchoolRecord | TransferCertificate | Bonafide.</summary>
    public required string DocumentType { get; set; }

    public required string BlobPath { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
