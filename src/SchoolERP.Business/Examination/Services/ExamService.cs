using QuestPDF.Fluent;
using SchoolERP.Business.Examination.Documents;
using SchoolERP.Business.Examination.DTOs;
using SchoolERP.DataAccess.Examination.Entities;
using SchoolERP.DataAccess.Examination.Repositories.Interfaces;
using SchoolERP.Business.Examination.Services.Interfaces;
using SchoolERP.DataAccess.Storage;

namespace SchoolERP.Business.Examination.Services;

public class ExamService : IExamService
{
    private readonly IExamRepository _exams;
    private readonly IMarksRepository _marks;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<ExamService> _logger;
    private readonly string _schoolName;

    private const string ContainerName = "report-cards";

    public ExamService(IExamRepository exams, IMarksRepository marks, IBlobStorageService blobStorage, IConfiguration configuration, ILogger<ExamService> logger)
    {
        _exams = exams;
        _marks = marks;
        _blobStorage = blobStorage;
        _logger = logger;
        _schoolName = configuration["School:Name"] ?? "School";
    }

    public async Task<ExamSummary> CreateAsync(CreateExamRequest request, CancellationToken ct = default)
    {
        var exam = new Exam
        {
            Name = request.Name,
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            ExamDateUtc = request.ExamDateUtc,
            MaxMarks = request.MaxMarks,
            PassingMarks = request.PassingMarks,
            AnswerKeyJson = request.AnswerKeyJson
        };

        await _exams.AddAsync(exam, ct);
        await _exams.SaveChangesAsync(ct);

        return ToSummary(exam);
    }

    public async Task<IReadOnlyList<ExamSummary>> GetByClassAsync(string? classId, CancellationToken ct = default)
    {
        var exams = await _exams.FindByClassAsync(classId, ct);
        return exams.Select(ToSummary).ToList();
    }

    public async Task PublishResultsAsync(Guid examId, CancellationToken ct = default)
    {
        _ = await _exams.FindByIdAsync(examId, ct) ?? throw new KeyNotFoundException("Exam not found.");

        // Two-step workflow: results are entered progressively, then explicitly published --
        // students/parents can't see marks until this flag flips.
        await _exams.PublishResultsAsync(examId, ct);

        _logger.LogInformation("Exam results published: {ExamId}", examId);
    }

    public async Task<ExamStatsResponse> GetStatsAsync(Guid examId, CancellationToken ct = default)
    {
        var average = await _marks.GetAverageMarksAsync(examId, ct);
        var rankings = await _marks.GetRankingsAsync(examId, ct);

        return new ExamStatsResponse(
            examId,
            average,
            rankings.Select(r => new RankingEntry(r.StudentId, r.MarksObtained, r.Rank)).ToList());
    }

    public async Task<string> GenerateReportCardAsync(Guid examId, Guid studentId, CancellationToken ct = default)
    {
        var exam = await _exams.FindByIdAsync(examId, ct) ?? throw new KeyNotFoundException("Exam not found.");

        if (!exam.IsResultPublished)
            throw new InvalidOperationException("Results for this exam have not been published yet.");

        var marks = await _marks.FindByExamAndStudentAsync(examId, studentId, ct)
            ?? throw new KeyNotFoundException("No marks entry found for this student on this exam.");

        var rows = new List<(string, decimal, int, string?)> { (exam.Name, marks.MarksObtained, exam.MaxMarks, marks.Grade) };

        // The file name carries a fingerprint of everything printed on the card, so an
        // unchanged card is served from storage instead of being rebuilt on every request
        // (rebuilding cost ~75 database units each time -- enough for one account to drain
        // the free monthly allowance). Changed marks give a new fingerprint and a fresh card.
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            $"{_schoolName}|{exam.Name}|{marks.MarksObtained}|{exam.MaxMarks}|{marks.Grade}")))[..16].ToLowerInvariant();
        var blobPath = $"{studentId}/{examId}-{fingerprint}.pdf";

        if (!await _blobStorage.ExistsAsync(ContainerName, blobPath, ct))
        {
            var pdfBytes = new ReportCardDocument(studentId, exam.Name, rows, _schoolName).GeneratePdf();
            using var stream = new MemoryStream(pdfBytes);
            await _blobStorage.UploadAsync(ContainerName, blobPath, stream, "application/pdf", ct);
        }

        return await _blobStorage.GetSasUrlAsync(ContainerName, blobPath, TimeSpan.FromMinutes(15), ct);
    }

    private static ExamSummary ToSummary(Exam e) => new(e.Id, e.Name, e.ClassId, e.SubjectId, e.ExamDateUtc, e.MaxMarks, e.IsResultPublished);
}
