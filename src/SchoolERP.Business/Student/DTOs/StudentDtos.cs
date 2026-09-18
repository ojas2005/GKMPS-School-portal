using SchoolERP.Common;
using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Student.DTOs;

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
    string? Address,
    Guid? ParentUserId = null);

public record ReassignClassRequest([Required] string ClassId, [Required] string SectionId);

// Full edit of everything collected at admission (except login credentials, which live in
// the Identity module and are handled by the separate admin password-reset flow).
public record UpdateStudentRequest(
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

public record StudentSummary(
    Guid Id, Guid LinkedUserId, string AdmissionNumber, string FullName, DateTime? DateOfBirth, string? Gender,
    string ClassId, string SectionId, string Status, DateTime AdmissionDateUtc,
    string? ParentName, string? ParentEmail, string? ParentPhone, string? Address, Guid? ParentUserId = null)
{
    /// <summary>
    /// This record trimmed to what a caller may see (see SchoolERP.Common.StudentAccess):
    /// the directory view keeps only who and where the child is; the contact view adds the
    /// parent's contact details; the full profile is returned as-is.
    /// </summary>
    public StudentSummary ViewAs(StudentRecord view) => view switch
    {
        StudentRecord.Profile => this,
        StudentRecord.Contact => this with { DateOfBirth = null, Gender = null, Address = null, ParentUserId = null },
        _ => this with { DateOfBirth = null, Gender = null, Address = null, ParentUserId = null, ParentName = null, ParentEmail = null, ParentPhone = null }
    };
}

// Links (or with null, unlinks) the Parent-role login that may view this student's records.
public record LinkParentAccountRequest(Guid? ParentUserId);

public record UploadDocumentRequest([Required] string DocumentType, [Required] string ContentType);

public record RequestTransferCertificateRequest([Required] string Reason, [Required] DateTime RequestedLeavingDateUtc);

public record TransferCertificateSummary(
    Guid Id, Guid StudentId, string Reason, DateTime RequestedLeavingDateUtc,
    bool IsSubmitted, bool IsApproved, bool IsPdfGenerated, string VerificationCode);

public record TransferCertificateVerification(
    bool IsValid, string? StudentName, string? AdmissionNumber, DateTime? ApprovedAtUtc);
