using SchoolERP.Common;
using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Student.DTOs;

// Admission. Students have no login: the parent's login (created first, role Parent) is how
// the family follows attendance, marks and fees. Both parents' name and mobile are required.
public record CreateStudentRequest(
    [Required, Display(Name = "Admission number")] string AdmissionNumber,
    [Required, StringLength(200), Display(Name = "Student's name")] string FullName,
    [Required, Display(Name = "Date of birth")] DateTime DateOfBirth,
    [Required, Display(Name = "Gender")] string Gender,
    [Required, Display(Name = "Class")] string ClassId,
    [Required, Display(Name = "Section")] string SectionId,
    [Required, Display(Name = "Parent login")] Guid ParentUserId,
    [Required, StringLength(200, MinimumLength = 2), Display(Name = "Father's name")] string FatherName,
    [Required, IndianMobile, Display(Name = "Father's mobile number")] string FatherPhone,
    [EmailAddress, StringLength(256), Display(Name = "Father's email")] string? FatherEmail,
    [Required, StringLength(200, MinimumLength = 2), Display(Name = "Mother's name")] string MotherName,
    [Required, IndianMobile, Display(Name = "Mother's mobile number")] string MotherPhone,
    [EmailAddress, StringLength(256), Display(Name = "Mother's email")] string? MotherEmail,
    [StringLength(500), Display(Name = "Address")] string? Address,
    // Fees still owed from before (an earlier school or year), on top of this class's fees.
    [Range(0, 10_000_000), Display(Name = "Previous dues")] decimal? PreviousDues = null,
    // Transport: a route means the student uses it, and its monthly charge is billed with tuition.
    Guid? RouteId = null,
    [StringLength(200), Display(Name = "Pickup point")] string? PickupPoint = null);

public record ReassignClassRequest([Required] string ClassId, [Required] string SectionId);

// Full edit of everything collected at admission (except the parent's login, which lives in
// the Identity module and is handled by the admin password-reset flow).
public record UpdateStudentRequest(
    [Required, Display(Name = "Admission number")] string AdmissionNumber,
    [Required, StringLength(200), Display(Name = "Student's name")] string FullName,
    [Required, Display(Name = "Date of birth")] DateTime DateOfBirth,
    [Required, Display(Name = "Gender")] string Gender,
    [Required, Display(Name = "Class")] string ClassId,
    [Required, Display(Name = "Section")] string SectionId,
    [Required, StringLength(200, MinimumLength = 2), Display(Name = "Father's name")] string FatherName,
    [Required, IndianMobile, Display(Name = "Father's mobile number")] string FatherPhone,
    [EmailAddress, StringLength(256), Display(Name = "Father's email")] string? FatherEmail,
    [Required, StringLength(200, MinimumLength = 2), Display(Name = "Mother's name")] string MotherName,
    [Required, IndianMobile, Display(Name = "Mother's mobile number")] string MotherPhone,
    [EmailAddress, StringLength(256), Display(Name = "Mother's email")] string? MotherEmail,
    [StringLength(500), Display(Name = "Address")] string? Address);

public record StudentSummary(
    Guid Id, Guid? LinkedUserId, string AdmissionNumber, string FullName, DateTime? DateOfBirth, string? Gender,
    string ClassId, string SectionId, string Status, DateTime AdmissionDateUtc,
    string? FatherName, string? FatherPhone, string? FatherEmail,
    string? MotherName, string? MotherPhone, string? MotherEmail,
    string? ParentName, string? ParentEmail, string? ParentPhone, string? Address, Guid? ParentUserId = null,
    DateTime? RemovedAtUtc = null, string? RemovalReason = null)
{
    /// <summary>
    /// This record trimmed to what a caller may see (see SchoolERP.Common.StudentAccess):
    /// the directory view keeps only who and where the child is; the contact view adds the
    /// parents' contact details; the full profile is returned as-is.
    /// </summary>
    public StudentSummary ViewAs(StudentRecord view) => view switch
    {
        StudentRecord.Profile => this,
        StudentRecord.Contact => this with { DateOfBirth = null, Gender = null, Address = null, ParentUserId = null, RemovalReason = null },
        _ => this with
        {
            DateOfBirth = null, Gender = null, Address = null, ParentUserId = null, RemovalReason = null,
            FatherName = null, FatherPhone = null, FatherEmail = null, MotherName = null, MotherPhone = null, MotherEmail = null,
            ParentName = null, ParentEmail = null, ParentPhone = null
        }
    };
}

/// <summary>The new student and what admission put on their fee account.</summary>
public record AdmittedStudent(StudentSummary Student, SchoolERP.Business.Fee.DTOs.FeeBreakdown Fees);

public record RemoveStudentRequest([StringLength(500)] string? Reason);

// Links (or with null, unlinks) the Parent-role login that may view this student's records.
public record LinkParentAccountRequest(Guid? ParentUserId);

public record UploadDocumentRequest([Required] string DocumentType, [Required] string ContentType);

public record RequestTransferCertificateRequest([Required] string Reason, [Required] DateTime RequestedLeavingDateUtc);

public record TransferCertificateSummary(
    Guid Id, Guid StudentId, string Reason, DateTime RequestedLeavingDateUtc,
    bool IsSubmitted, bool IsApproved, bool IsPdfGenerated, string VerificationCode);

public record TransferCertificateVerification(
    bool IsValid, string? StudentName, string? AdmissionNumber, DateTime? ApprovedAtUtc);
