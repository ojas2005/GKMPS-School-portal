using SchoolERP.Business.Examination.DTOs;

namespace SchoolERP.Business.Examination.Services.Interfaces;

public interface IExamService
{
    Task<ExamSummary> CreateAsync(CreateExamRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ExamSummary>> GetByClassAsync(string? classId, CancellationToken ct = default);
    Task PublishResultsAsync(Guid examId, CancellationToken ct = default);
    Task<ExamStatsResponse> GetStatsAsync(Guid examId, CancellationToken ct = default);
    Task<string> GenerateReportCardAsync(Guid examId, Guid studentId, CancellationToken ct = default);
}
