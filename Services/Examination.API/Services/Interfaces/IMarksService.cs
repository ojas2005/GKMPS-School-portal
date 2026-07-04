using SchoolERP.Examination.DTOs;

namespace SchoolERP.Examination.Services.Interfaces;

public interface IMarksService
{
    Task<MarksSummary> EnterAsync(Guid examId, EnterMarksRequest request, Guid enteredByStaffId, CancellationToken ct = default);
    Task<MarksSummary> CorrectAsync(Guid marksEntryId, decimal marksObtained, string? grade, CancellationToken ct = default);
    Task<IReadOnlyList<StudentResultSummary>> GetForStudentAsync(Guid studentId, bool publishedOnly, CancellationToken ct = default);
}
