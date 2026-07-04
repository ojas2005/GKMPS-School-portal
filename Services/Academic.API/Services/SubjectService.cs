using SchoolERP.Academic.DTOs;
using SchoolERP.Academic.Entities;
using SchoolERP.Academic.Repositories.Interfaces;
using SchoolERP.Academic.Services.Interfaces;

namespace SchoolERP.Academic.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjects;

    public SubjectService(ISubjectRepository subjects) => _subjects = subjects;

    public async Task<SubjectSummary> CreateAsync(CreateSubjectRequest request, CancellationToken ct = default)
    {
        if (await _subjects.ExistsAsync(request.ClassId, request.Code, ct))
            throw new InvalidOperationException($"Subject code '{request.Code}' already exists for class '{request.ClassId}'.");

        var subject = new Subject
        {
            Code = request.Code,
            Name = request.Name,
            ClassId = request.ClassId,
            TeacherStaffId = request.TeacherStaffId
        };

        await _subjects.AddAsync(subject, ct);
        await _subjects.SaveChangesAsync(ct);

        return ToSummary(subject);
    }

    public async Task<IReadOnlyList<SubjectSummary>> GetByClassAsync(string classId, CancellationToken ct = default)
    {
        var subjects = await _subjects.FindByClassAsync(classId, ct);
        return subjects.Select(ToSummary).ToList();
    }

    public async Task<SubjectSummary> UpdateSyllabusAsync(Guid subjectId, string? syllabusOutline, CancellationToken ct = default)
    {
        var subject = await _subjects.FindByIdAsync(subjectId, ct)
            ?? throw new KeyNotFoundException("Subject not found.");

        subject.SyllabusOutline = syllabusOutline;
        subject.UpdatedAtUtc = DateTime.UtcNow;
        await _subjects.SaveChangesAsync(ct);

        return ToSummary(subject);
    }

    private static SubjectSummary ToSummary(Subject s) => new(s.Id, s.Code, s.Name, s.ClassId, s.TeacherStaffId, s.SyllabusOutline);
}
