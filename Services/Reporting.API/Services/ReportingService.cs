using System.Text.Json;
using QuestPDF.Fluent;
using SchoolERP.Reporting.Clients;
using SchoolERP.Reporting.Documents;
using SchoolERP.Reporting.DTOs;
using SchoolERP.Reporting.Entities;
using SchoolERP.Reporting.Repositories.Interfaces;
using SchoolERP.Reporting.Services.Interfaces;

namespace SchoolERP.Reporting.Services;

/// <summary>
/// Every figure here is fetched live from the owning service's own HTTP API (via typed
/// clients wrapped in Polly retry/circuit-breaker, configured in Program.cs) -- never by
/// querying another service's database directly. Each run is persisted as a
/// ReportSnapshot for audit/reproducibility.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IStudentServiceClient _studentClient;
    private readonly IFeeServiceClient _feeClient;
    private readonly IReportSnapshotRepository _snapshots;
    private readonly IConfiguration _configuration;

    public ReportingService(
        IStudentServiceClient studentClient,
        IFeeServiceClient feeClient,
        IReportSnapshotRepository snapshots,
        IConfiguration configuration)
    {
        _studentClient = studentClient;
        _feeClient = feeClient;
        _snapshots = snapshots;
        _configuration = configuration;
    }

    public async Task<EnrollmentReportResponse> GetEnrollmentReportAsync(Guid generatedByUserId, CancellationToken ct = default)
    {
        var counts = await _studentClient.GetActiveCountByClassAsync(ct);
        var response = new EnrollmentReportResponse(counts, DateTime.UtcNow);

        await PersistSnapshotAsync("EnrollmentByClass", null, null, response, generatedByUserId, ct);

        return response;
    }

    public async Task<FeeCollectionReportResponse> GetFeeCollectionReportAsync(DateTime? fromUtc, DateTime? toUtc, Guid generatedByUserId, CancellationToken ct = default)
    {
        var total = await _feeClient.GetTotalCollectedAsync(fromUtc, toUtc, ct);
        var response = new FeeCollectionReportResponse(fromUtc, toUtc, total, DateTime.UtcNow);

        await PersistSnapshotAsync("FeeCollection", fromUtc, toUtc, response, generatedByUserId, ct);

        return response;
    }

    public async Task<byte[]> GenerateEnrollmentPdfAsync(CancellationToken ct = default)
    {
        var counts = await _studentClient.GetActiveCountByClassAsync(ct);
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
