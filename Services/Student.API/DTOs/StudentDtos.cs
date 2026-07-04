using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Student.DTOs;

public record CreateStudentRequest(
    [Required] Guid LinkedUserId,
    [Required] string AdmissionNumber,
    [Required] string FullName,
    [Required] DateTime DateOfBirth,
    [Required] string Gender,
    [Required] string ClassId,
    [Required] string SectionId,
    string? ParentName,
    [EmailAddress] string? ParentEmail,
    string? ParentPhone,
    string? Address);

public record ReassignClassRequest([Required] string ClassId, [Required] string SectionId);

public record StudentSummary(
    Guid Id, string AdmissionNumber, string FullName, DateTime DateOfBirth, string Gender,
    string ClassId, string SectionId, string Status, DateTime AdmissionDateUtc);

public record UploadDocumentRequest([Required] string DocumentType, [Required] string ContentType);

public record RequestTransferCertificateRequest([Required] string Reason, [Required] DateTime RequestedLeavingDateUtc);

public record TransferCertificateSummary(
    Guid Id, Guid StudentId, string Reason, DateTime RequestedLeavingDateUtc,
    bool IsSubmitted, bool IsApproved, bool IsPdfGenerated, string VerificationCode);

public record TransferCertificateVerification(
    bool IsValid, string? StudentName, string? AdmissionNumber, DateTime? ApprovedAtUtc);
