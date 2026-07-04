namespace SchoolERP.Shared.Events;

/// <summary>
/// Published whenever a PDF document (transfer certificate, bonafide, report card, receipt,
/// ID card) is generated and uploaded to Blob Storage. Consumed by Notification.API to
/// alert the recipient, carrying only the verification code -- never the raw SAS URL --
/// so the notification itself cannot leak time-limited access.
/// </summary>
public record CertificateGeneratedEvent
{
    public required string DocumentType { get; init; } // "TransferCertificate" | "Bonafide" | "ReportCard" | "Receipt" | "IdCard"
    public Guid SubjectId { get; init; }               // e.g. StudentId
    public required string VerificationCode { get; init; }
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}
