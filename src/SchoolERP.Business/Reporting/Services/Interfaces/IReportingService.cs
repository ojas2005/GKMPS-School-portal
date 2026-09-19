using SchoolERP.Business.Reporting.DTOs;

namespace SchoolERP.Business.Reporting.Services.Interfaces;

public interface IReportingService
{
    Task<EnrollmentReportResponse> GetEnrollmentReportAsync(Guid generatedByUserId, CancellationToken ct = default);
    Task<FeeCollectionReportResponse> GetFeeCollectionReportAsync(DateTime? fromUtc, DateTime? toUtc, Guid generatedByUserId, CancellationToken ct = default);
    Task<byte[]> GenerateEnrollmentPdfAsync(CancellationToken ct = default);
}
