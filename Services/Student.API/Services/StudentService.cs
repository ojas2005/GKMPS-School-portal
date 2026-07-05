using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SchoolERP.Shared.Common;
using SchoolERP.Shared.Events;
using SchoolERP.Student.DTOs;
using SchoolERP.Student.Entities;
using SchoolERP.Student.Repositories.Interfaces;
using SchoolERP.Student.Services.Interfaces;

namespace SchoolERP.Student.Services;

/// <summary>
/// Business-facing operations for admissions and class/section management. All workflow
/// rules (duplicate admission-number checks, cross-service event publishing, audit
/// logging) live here -- the controller only translates HTTP <-> this interface.
/// </summary>
public class StudentService : IStudentService
{
    private readonly IStudentRepository _students;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDistributedCache _cache;
    private readonly ILogger<StudentService> _logger;

    private const string ActiveCountsCacheKey = "student:active-count-by-class";

    public StudentService(
        IStudentRepository students,
        IPublishEndpoint publishEndpoint,
        IDistributedCache cache,
        ILogger<StudentService> logger)
    {
        _students = students;
        _publishEndpoint = publishEndpoint;
        _cache = cache;
        _logger = logger;
    }

    public async Task<StudentSummary> AdmitStudentAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        // Duplicate-prevention check before insert (mirrors HasStudentReviewed()).
        if (await _students.ExistsByAdmissionNumberAsync(request.AdmissionNumber, ct))
            throw new InvalidOperationException($"Admission number '{request.AdmissionNumber}' is already in use.");

        var student = new StudentProfile
        {
            LinkedUserId = request.LinkedUserId,
            AdmissionNumber = request.AdmissionNumber,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            ClassId = request.ClassId,
            SectionId = request.SectionId,
            ParentName = request.ParentName,
            ParentEmail = request.ParentEmail,
            ParentPhone = request.ParentPhone,
            Address = request.Address
        };

        await _students.AddAsync(student, ct);
        await _students.SaveChangesAsync(ct);

        // Async event: Notification.API sends a welcome email; Reporting.API updates enrollment totals.
        await _publishEndpoint.Publish(new StudentEnrolledEvent
        {
            StudentId = student.Id,
            AdmissionNumber = student.AdmissionNumber,
            FullName = student.FullName,
            ClassId = student.ClassId,
            SectionId = student.SectionId,
            ParentEmail = student.ParentEmail
        }, ct);

        await _cache.RemoveAsync(ActiveCountsCacheKey, ct);

        _logger.LogInformation("Student admitted: {StudentId} ({AdmissionNumber})", student.Id, student.AdmissionNumber);

        return ToSummary(student);
    }

    public async Task<StudentSummary?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(id, ct);
        return student is null ? null : ToSummary(student);
    }

    public async Task<PagedResult<StudentSummary>> SearchAsync(string? classId, string? sectionId, string? keyword, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var students = await _students.SearchStudentsAsync(classId, sectionId, keyword, page, pageSize, ct);
        var total = await _students.CountStudentsAsync(classId, sectionId, ct);

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
        student.ParentName = request.ParentName;
        student.ParentEmail = request.ParentEmail;
        student.ParentPhone = request.ParentPhone;
        student.Address = request.Address;

        await _students.SaveChangesAsync(ct);

        if (classChanged)
            await _cache.RemoveAsync(ActiveCountsCacheKey, ct);

        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=Student.UpdateDetails entity=StudentProfile entityId={StudentId} before={Before} after={After}",
            actorUserId, actorRole, studentId,
            JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(new { request.AdmissionNumber, request.FullName, request.DateOfBirth, request.Gender, request.ClassId, request.SectionId }));

        return ToSummary(student);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default)
    {
        // Redis-cached dashboard stat, 5-minute TTL as specified in the non-functional requirements.
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

    private static StudentSummary ToSummary(StudentProfile s) =>
        new(s.Id, s.LinkedUserId, s.AdmissionNumber, s.FullName, s.DateOfBirth, s.Gender, s.ClassId, s.SectionId, s.Status, s.AdmissionDateUtc,
            s.ParentName, s.ParentEmail, s.ParentPhone, s.Address);
}
