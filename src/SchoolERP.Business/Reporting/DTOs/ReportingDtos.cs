namespace SchoolERP.Business.Reporting.DTOs;

public record EnrollmentReportResponse(int TotalStudents, IReadOnlyDictionary<string, int> ActiveCountByClass, DateTime GeneratedAtUtc);
public record FeeCollectionReportResponse(DateTime? FromUtc, DateTime? ToUtc, decimal TotalCollected, DateTime GeneratedAtUtc);
