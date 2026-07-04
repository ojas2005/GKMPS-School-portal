using SchoolERP.Examination.DTOs;
using SchoolERP.Examination.Entities;
using SchoolERP.Examination.Repositories.Interfaces;
using SchoolERP.Examination.Services.Interfaces;

namespace SchoolERP.Examination.Services;

public class MarksService : IMarksService
{
    private readonly IMarksRepository _marks;
    private readonly IExamRepository _exams;
    private readonly ILogger<MarksService> _logger;

    public MarksService(IMarksRepository marks, IExamRepository exams, ILogger<MarksService> logger)
    {
        _marks = marks;
        _exams = exams;
        _logger = logger;
    }

    public async Task<MarksSummary> EnterAsync(Guid examId, EnterMarksRequest request, Guid enteredByStaffId, CancellationToken ct = default)
    {
        var exam = await _exams.FindByIdAsync(examId, ct) ?? throw new KeyNotFoundException("Exam not found.");

        if (request.MarksObtained > exam.MaxMarks)
            throw new InvalidOperationException($"Marks obtained ({request.MarksObtained}) cannot exceed max marks ({exam.MaxMarks}).");

        // Duplicate-prevention guard: marks entered once per student per exam; use CorrectAsync for corrections.
        if (await _marks.HasMarksEnteredAsync(examId, request.StudentId, ct))
            throw new InvalidOperationException("Marks have already been entered for this student on this exam. Use the correction endpoint instead.");

        var entry = new MarksEntry
        {
            ExamId = examId,
            StudentId = request.StudentId,
            MarksObtained = request.MarksObtained,
            Grade = request.Grade,
            Remarks = request.Remarks,
            EnteredByStaffId = enteredByStaffId
        };

        await _marks.AddAsync(entry, ct);
        await _marks.SaveChangesAsync(ct);

        return ToSummary(entry);
    }

    public async Task<MarksSummary> CorrectAsync(Guid marksEntryId, decimal marksObtained, string? grade, CancellationToken ct = default)
    {
        var entry = await _marks.FindByIdAsync(marksEntryId, ct) ?? throw new KeyNotFoundException("Marks entry not found.");

        await _marks.UpdateMarksAsync(marksEntryId, marksObtained, grade, ct);

        _logger.LogInformation("AUDIT action=Marks.Correct entity=MarksEntry entityId={MarksEntryId} before={Before} after={After}",
            marksEntryId, entry.MarksObtained, marksObtained);

        entry.MarksObtained = marksObtained;
        entry.Grade = grade;
        return ToSummary(entry);
    }

    public async Task<IReadOnlyList<StudentResultSummary>> GetForStudentAsync(Guid studentId, bool publishedOnly, CancellationToken ct = default)
    {
        var entries = await _marks.FindByStudentAsync(studentId, publishedOnly, ct);
        return entries.Select(m => new StudentResultSummary(
            m.ExamId,
            m.Exam?.Name ?? "Exam",
            m.Exam?.SubjectId ?? Guid.Empty,
            m.Exam?.ExamDateUtc ?? default,
            m.MarksObtained,
            m.Exam?.MaxMarks ?? 0,
            m.Exam?.PassingMarks ?? 0,
            m.Grade,
            m.Exam?.IsResultPublished ?? false)).ToList();
    }

    private static MarksSummary ToSummary(MarksEntry m) => new(m.Id, m.ExamId, m.StudentId, m.MarksObtained, m.Grade);
}
