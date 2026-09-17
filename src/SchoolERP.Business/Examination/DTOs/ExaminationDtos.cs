using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Examination.DTOs;

public record CreateExamRequest(
    [Required] string Name, [Required] string ClassId, [Required] Guid SubjectId,
    [Required] DateTime ExamDateUtc, [Required] int MaxMarks, [Required] int PassingMarks, string? AnswerKeyJson);

public record ExamSummary(Guid Id, string Name, string ClassId, Guid SubjectId, DateTime ExamDateUtc, int MaxMarks, bool IsResultPublished);

public record EnterMarksRequest([Required] Guid StudentId, [Required] decimal MarksObtained, string? Grade, string? Remarks);

public record MarksSummary(Guid Id, Guid ExamId, Guid StudentId, decimal MarksObtained, string? Grade);

public record StudentResultSummary(
    Guid ExamId, string ExamName, Guid SubjectId, DateTime ExamDateUtc,
    decimal MarksObtained, int MaxMarks, int PassingMarks, string? Grade, bool IsResultPublished);

public record ExamStatsResponse(Guid ExamId, double AverageMarks, IReadOnlyList<RankingEntry> Rankings);

public record RankingEntry(Guid StudentId, decimal MarksObtained, int Rank);
