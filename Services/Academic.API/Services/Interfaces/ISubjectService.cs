using SchoolERP.Academic.DTOs;

namespace SchoolERP.Academic.Services.Interfaces;

public interface ISubjectService
{
    Task<SubjectSummary> CreateAsync(CreateSubjectRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SubjectSummary>> GetByClassAsync(string classId, CancellationToken ct = default);
    Task<SubjectSummary> UpdateSyllabusAsync(Guid subjectId, string? syllabusOutline, CancellationToken ct = default);
}
