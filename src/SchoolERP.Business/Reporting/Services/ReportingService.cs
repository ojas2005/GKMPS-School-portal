using System.Text.Json;
using QuestPDF.Fluent;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Reporting.Documents;
using SchoolERP.Business.Reporting.DTOs;
using SchoolERP.DataAccess.Reporting.Entities;
using SchoolERP.DataAccess.Reporting.Repositories.Interfaces;
using SchoolERP.Business.Reporting.Services.Interfaces;

namespace SchoolERP.Business.Reporting.Services;

/// <summary>
/// Every figure comes from the owning module's business service (Student, Fee) -- never by
/// querying another module's tables directly. Each run is persisted as a ReportSnapshot for
/// audit/reproducibility.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IStudentService _students;
    private readonly IFeePaymentService _fees;
    private readonly IReportSnapshotRepository _snapshots;
    private readonly IConfiguration _configuration;

    public ReportingService(
        IStudentService students,
        IFeePaymentService fees,
        IReportSnapshotRepository snapshots,
        IConfiguration configuration)
    {
        _students = students;
        _fees = fees;
        _snapshots = snapshots;
        _configuration = configuration;
    }

    public async Task<EnrollmentReportResponse> GetEnrollmentReportAsync(Guid generatedByUserId, CancellationToken ct = default)
    {
        var counts = await _students.GetActiveCountByClassAsync(ct);
        var response = new EnrollmentReportResponse(counts.Values.Sum(), counts, DateTime.UtcNow);

        await PersistSnapshotAsync("EnrollmentByClass", null, null, response, generatedByUserId, ct);

        return response;
    }

    public async Task<FeeCollectionReportResponse> GetFeeCollectionReportAsync(DateTime? fromUtc, DateTime? toUtc, Guid generatedByUserId, CancellationToken ct = default)
    {
        var total = (await _fees.GetCollectionTotalsAsync(fromUtc, toUtc, ct)).TotalCollected;
        var response = new FeeCollectionReportResponse(fromUtc, toUtc, total, DateTime.UtcNow);

        await PersistSnapshotAsync("FeeCollection", fromUtc, toUtc, response, generatedByUserId, ct);

        return response;
    }

    public async Task<byte[]> GenerateEnrollmentPdfAsync(CancellationToken ct = default)
    {
        var counts = await _students.GetActiveCountByClassAsync(ct);
        var schoolName = _configuration["School:Name"] ?? "School";
        var document = new EnrollmentReportDocument(counts, schoolName);
        return document.GeneratePdf();
    }

    private async Task PersistSnapshotAsync(string reportType, DateTime? fromUtc, DateTime? toUtc, object result, Guid generatedByUserId, CancellationToken ct)
    {
        var snapshot = new ReportSnapshot
        {
            ReportType = reportType,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            ResultJson = JsonSerializer.Serialize(result),
            GeneratedByUserId = generatedByUserId
        };

        await _snapshots.AddAsync(snapshot, ct);
        await _snapshots.SaveChangesAsync(ct);
    }
}
