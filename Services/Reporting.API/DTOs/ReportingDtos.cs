namespace SchoolERP.Reporting.DTOs;

public record EnrollmentReportResponse(IReadOnlyDictionary<string, int> ActiveCountByClass, DateTime GeneratedAtUtc);
public record FeeCollectionReportResponse(DateTime? FromUtc, DateTime? ToUtc, decimal TotalCollected, DateTime GeneratedAtUtc);
