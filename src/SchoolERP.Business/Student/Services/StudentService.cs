using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SchoolERP.Common;
using SchoolERP.Common.Events;
using SchoolERP.Business.Student.DTOs;
using SchoolERP.DataAccess.Student.Entities;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Common.Audit;

namespace SchoolERP.Business.Student.Services;

/// <summary>
/// Business-facing operations for admissions and class/section management. All workflow
/// rules (duplicate admission-number checks, raising events, audit
/// logging) live here -- the controller only translates HTTP <-> this interface.
/// </summary>
public class StudentService : IStudentService
{
    private readonly IStudentRepository _students;
    private readonly IEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly IUserService _users;
    private readonly IAuditTrail _audit;
    private readonly TimeProvider _clock;
    private readonly ILogger<StudentService> _logger;

    private const string ActiveCountsCacheKey = "student:active-count-by-class";

    public StudentService(
        IStudentRepository students,
        IEventPublisher events,
        IDistributedCache cache,
        IUserService users,
        IAuditTrail audit,
        TimeProvider clock,
        ILogger<StudentService> logger)
    {
        _students = students;
        _events = events;
        _cache = cache;
        _users = users;
        _audit = audit;
        _clock = clock;
        _logger = logger;
    }

    public async Task<StudentSummary> CreateAdmissionAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        // Duplicate-prevention check before insert (mirrors HasStudentReviewed()).
        if (await _students.ExistsByAdmissionNumberAsync(request.AdmissionNumber, ct))
            throw new InvalidOperationException($"Admission number '{request.AdmissionNumber}' is already in use.");

        // The parent's login is how the family sees the child's records, so it must exist,
        // be a Parent login, and not already serve another child (a login shows one child).
        var parent = await _users.GetByIdAsync(request.ParentUserId, ct);
        if (parent is null || parent.Role != RoleNames.Parent)
            throw new InvalidOperationException("Create the parent's login first; it must have the Parent role.");
        if (await _students.CountOtherCurrentChildrenOfParentAsync(request.ParentUserId, Guid.Empty, ct) > 0)
            throw new InvalidOperationException("That parent login already belongs to another student. Each child needs its own parent login.");

        var student = new StudentProfile
        {
            AdmissionNumber = request.AdmissionNumber.Trim(),
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            ClassId = request.ClassId,
            SectionId = request.SectionId,
            ParentUserId = request.ParentUserId,
            Address = Clean(request.Address)
        };
        SetParents(student, request.FatherName, request.FatherPhone, request.FatherEmail, request.MotherName, request.MotherPhone, request.MotherEmail);

        await _students.AddAsync(student, ct);
        await _students.SaveChangesAsync(ct);
        await _cache.RemoveAsync(ActiveCountsCacheKey, ct);

        _logger.LogInformation("Student record created: {StudentId} ({AdmissionNumber})", student.Id, student.AdmissionNumber);
        return ToSummary(student);
    }

    public async Task UndoAdmissionAsync(Guid studentId, CancellationToken ct = default)
    {
        await _students.DeleteNewAdmissionAsync(studentId, ct);
        await _cache.RemoveAsync(ActiveCountsCacheKey, ct);
        _logger.LogWarning("Admission undone, student record deleted: {StudentId}", studentId);
    }

    private static void SetParents(StudentProfile s, string fatherName, string fatherPhone, string? fatherEmail,
        string motherName, string motherPhone, string? motherEmail)
    {
        s.FatherName = fatherName.Trim();
        s.FatherPhone = Phones.NormalizeMobile(fatherPhone) ?? fatherPhone.Trim();
        s.FatherEmail = Clean(fatherEmail)?.ToLowerInvariant();
        s.MotherName = motherName.Trim();
        s.MotherPhone = Phones.NormalizeMobile(motherPhone) ?? motherPhone.Trim();
        s.MotherEmail = Clean(motherEmail)?.ToLowerInvariant();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<StudentSummary?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(id, ct);
        return student is null ? null : ToSummary(student);
    }

    public async Task<IReadOnlyList<StudentSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        (await _students.FindByIdsAsync(ids.Distinct().ToList(), ct)).Select(ToSummary).ToList();

    public async Task<PagedResult<StudentSummary>> SearchAsync(string? classId, string? sectionId, string? keyword, RosterFilter roster, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var students = await _students.SearchStudentsAsync(classId, sectionId, keyword, roster, page, pageSize, ct);
        var total = await _students.CountStudentsAsync(classId, sectionId, keyword, roster, ct);

        return new PagedResult<StudentSummary>
        {
            Items = students.Select(ToSummary).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task ReassignClassAsync(Guid studentId, ReassignClassRequest request, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var before = await _students.FindByIdAsync(studentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        await _students.ReassignClassSectionAsync(studentId, request.ClassId, request.SectionId, ct);
        await _cache.RemoveAsync(ActiveCountsCacheKey, ct);
        if (before.ClassId != request.ClassId)
            await _events.PublishAsync(new StudentClassChangedEvent { StudentId = studentId, OldClassId = before.ClassId, NewClassId = request.ClassId }, ct);

        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=Student.ReassignClass entity=StudentProfile entityId={StudentId} before={Before} after={After}",
            actorUserId, actorRole, studentId,
            JsonSerializer.Serialize(new { before.ClassId, before.SectionId }),
            JsonSerializer.Serialize(new { request.ClassId, request.SectionId }));
    }

    public async Task<StudentSummary> UpdateDetailsAsync(Guid studentId, UpdateStudentRequest request, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(studentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        if (!string.Equals(student.AdmissionNumber, request.AdmissionNumber, StringComparison.Ordinal))
        {
            var existing = await _students.FindByAdmissionNumberAsync(request.AdmissionNumber, ct);
            if (existing is not null && existing.Id != studentId)
                throw new InvalidOperationException($"Admission number '{request.AdmissionNumber}' is already in use.");
        }

        var before = new { student.AdmissionNumber, student.FullName, student.DateOfBirth, student.Gender, student.ClassId, student.SectionId };
        var classChanged = student.ClassId != request.ClassId;

        student.AdmissionNumber = request.AdmissionNumber;
        student.FullName = request.FullName;
        student.DateOfBirth = request.DateOfBirth;
        student.Gender = request.Gender;
        student.ClassId = request.ClassId;
        student.SectionId = request.SectionId;
        SetParents(student, request.FatherName, request.FatherPhone, request.FatherEmail, request.MotherName, request.MotherPhone, request.MotherEmail);
        student.Address = Clean(request.Address);

        await _students.SaveChangesAsync(ct);

        if (classChanged)
        {
            await _cache.RemoveAsync(ActiveCountsCacheKey, ct);
            await _events.PublishAsync(new StudentClassChangedEvent { StudentId = studentId, OldClassId = before.ClassId, NewClassId = request.ClassId }, ct);
        }

        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=Student.UpdateDetails entity=StudentProfile entityId={StudentId} before={Before} after={After}",
            actorUserId, actorRole, studentId,
            JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(new { request.AdmissionNumber, request.FullName, request.DateOfBirth, request.Gender, request.ClassId, request.SectionId }));

        return ToSummary(student);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default)
    {
        // Cached dashboard stat, 5-minute TTL as specified in the non-functional requirements.
        var cached = await _cache.GetStringAsync(ActiveCountsCacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<Dictionary<string, int>>(cached)!;

        var counts = await _students.GetActiveCountByClassAsync(ct);

        await _cache.SetStringAsync(
            ActiveCountsCacheKey,
            JsonSerializer.Serialize(counts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            ct);

        return counts;
    }

    public async Task<StudentSummary> LinkParentAccountAsync(Guid studentId, Guid? parentUserId, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(studentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        var before = student.ParentUserId;
        await _students.SetParentUserIdAsync(studentId, parentUserId, ct);
        student.ParentUserId = parentUserId;

        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=Student.LinkParentAccount entity=Student entityId={StudentId} before={Before} after={After}",
            actorUserId, actorRole, studentId, before, parentUserId);

        return ToSummary(student);
    }

    public async Task<StudentSummary> RemoveAsync(Guid studentId, string? reason, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(studentId, ct) ?? throw new KeyNotFoundException("Student not found.");
        if (student.Status != StudentStatuses.Active)
            throw new InvalidOperationException("Only a current student can be removed.");

        // The parent's login goes quiet with the child, unless it still serves a current sibling.
        if (student.ParentUserId is { } parentId && await _students.CountOtherCurrentChildrenOfParentAsync(parentId, studentId, ct) == 0)
            await SetLoginActiveAsync(parentId, false, actorUserId, actorRole, ct);

        var cleanReason = Clean(reason);
        await _students.SetRemovedAsync(studentId, true, cleanReason, ct);
        await _cache.RemoveAsync(ActiveCountsCacheKey, ct);
        Audit("student.removed", studentId, actorUserId, actorRole, $"admission={student.AdmissionNumber}");
        await _events.PublishAsync(new StudentStatusChangedEvent { StudentId = studentId, Status = StudentStatuses.Removed }, ct);

        student.Status = StudentStatuses.Removed;
        student.RemovedAtUtc = DateTime.UtcNow;
        student.RemovalReason = cleanReason;
        return ToSummary(student);
    }

    public async Task<StudentSummary> RestoreAsync(Guid studentId, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(studentId, ct) ?? throw new KeyNotFoundException("Student not found.");
        if (student.Status != StudentStatuses.Removed)
            throw new InvalidOperationException("Only a removed student can be brought back.");

        if (student.ParentUserId is { } parentId)
            await SetLoginActiveAsync(parentId, true, actorUserId, actorRole, ct);

        await _students.SetRemovedAsync(studentId, false, null, ct);
        await _cache.RemoveAsync(ActiveCountsCacheKey, ct);
        Audit("student.restored", studentId, actorUserId, actorRole, $"admission={student.AdmissionNumber}");
        await _events.PublishAsync(new StudentStatusChangedEvent { StudentId = studentId, Status = StudentStatuses.Active }, ct);

        student.Status = StudentStatuses.Active;
        student.RemovedAtUtc = null;
        student.RemovalReason = null;
        return ToSummary(student);
    }

    private async Task SetLoginActiveAsync(Guid userId, bool active, string actorUserId, string actorRole, CancellationToken ct)
    {
        try { await _users.SetActiveStatusAsync(userId, active, actorUserId, actorRole, ct); }
        catch (KeyNotFoundException) { } // the login was deleted already; nothing to switch
    }

    private void Audit(string action, Guid studentId, string actorUserId, string actorRole, string detail) =>
        _audit.Record(new AuditRecord(DateTime.UtcNow, action, Guid.TryParse(actorUserId, out var actor) ? actor : null, actorRole,
            studentId.ToString(), Detail: detail));

    private static StudentSummary ToSummary(StudentProfile s) =>
        new(s.Id, s.LinkedUserId, s.AdmissionNumber, s.FullName, s.DateOfBirth, s.Gender, s.ClassId, s.SectionId, s.Status, s.AdmissionDateUtc,
            s.FatherName, s.FatherPhone, s.FatherEmail, s.MotherName, s.MotherPhone, s.MotherEmail,
            s.ParentName, s.ParentEmail, s.ParentPhone, s.Address, s.ParentUserId, s.RemovedAtUtc, s.RemovalReason);
}
