using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Business.Notification.Services.Interfaces;
using SchoolERP.Common.Audit;
using SchoolERP.DataAccess.Storage;
using SchoolERP.DataAccess.Student.Entities;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;

namespace SchoolERP.Business.Student.Services;

public record StudentErasureResult(string FormerName, bool StudentLoginErased, bool ParentLoginErased, int DocumentsDeleted, int NotificationsForgotten);

/// <summary>
/// Erases a former student's personal data -- the "right to erasure" in India's DPDP Act.
/// Only for students who have left (removed, or their transfer certificate was approved), so a current
/// pupil can't be wiped by mistake. What goes: name, date of birth, gender, address and
/// both parents' details on the profile; the student's login, and the parent's login unless it
/// still serves a sibling; transfer-certificate and report-card PDFs (which print the name
/// and date of birth); and notification records addressed to them. What stays, no longer naming the
/// child: fee and payment records (kept for accounts, as the law requires) and marks and
/// attendance, under the admission number only.
/// </summary>
public class StudentErasureService
{
    private readonly IStudentRepository _students;
    private readonly IUserService _users;
    private readonly INotificationService _notifications;
    private readonly IBlobStorageService _files;
    private readonly IAuditTrail _audit;

    public StudentErasureService(IStudentRepository students, IUserService users, INotificationService notifications, IBlobStorageService files, IAuditTrail audit)
    {
        _students = students;
        _users = users;
        _notifications = notifications;
        _files = files;
        _audit = audit;
    }

    public async Task<StudentErasureResult> EraseAsync(Guid studentId, string confirmAdmissionNumber, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(studentId, ct) ?? throw new KeyNotFoundException("Student not found.");
        if (student.Status == StudentStatuses.Erased)
            throw new InvalidOperationException("This student's personal data has already been erased.");
        if (student.Status is not (StudentStatuses.TransferredOut or StudentStatuses.Removed))
            throw new InvalidOperationException("Only a student who has left can be erased. Remove them, or approve their transfer certificate, first.");
        if (!string.Equals(confirmAdmissionNumber?.Trim(), student.AdmissionNumber, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Type the student's admission number to confirm.");

        var forget = new[] { student.ParentEmail, student.ParentPhone, student.FatherEmail, student.FatherPhone, student.MotherEmail, student.MotherPhone }
            .Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToList();

        // Students admitted before logins were dropped may still have one of their own.
        var studentEmail = student.LinkedUserId is { } studentUserId
            ? await _users.ErasePersonalDataAsync(studentUserId, actorUserId, actorRole, ct)
            : null;
        if (studentEmail is not null) forget.Add(studentEmail);

        var parentErased = false;
        if (student.ParentUserId is { } parentId && await _students.CountOtherChildrenOfParentAsync(parentId, studentId, ct) == 0)
        {
            var parentEmail = await _users.ErasePersonalDataAsync(parentId, actorUserId, actorRole, ct);
            if (parentEmail is not null) forget.Add(parentEmail);
            parentErased = true;
        }

        // Both print the child's name and date of birth. A report card opened later is rebuilt
        // from the anonymised record. Fee receipts print only the student ID, so they stay.
        var documents = await _files.DeleteByPrefixAsync("transfer-certificates", $"{studentId}/", ct)
                         + await _files.DeleteByPrefixAsync("report-cards", $"{studentId}/", ct);
        var notifications = await _notifications.ForgetRecipientsAsync(forget.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), ct);
        await _students.AnonymizeAsync(studentId, ct);

        _audit.Record(new AuditRecord(DateTime.UtcNow, "student.personal-data-erased",
            Guid.TryParse(actorUserId, out var actor) ? actor : null, actorRole, studentId.ToString(),
            Detail: $"admission={student.AdmissionNumber}; parentLoginErased={parentErased}; documents={documents}; notifications={notifications}"));

        return new StudentErasureResult(student.FullName, studentEmail is not null, parentErased, documents, notifications);
    }
}
