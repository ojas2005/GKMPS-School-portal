using SchoolERP.Shared.Entities;

namespace SchoolERP.Reporting.Entities;

/// <summary>
/// A durable record of every generated report -- who ran it, over what range, and the
/// resulting figures (ResultJson) -- so historical reports remain reproducible even if
/// the source data in other services later changes. This is Reporting.API's own owned
/// data; the underlying numbers themselves are always fetched fresh from the owning
/// service via IHttpClientFactory, never joined cross-schema.
/// </summary>
public class ReportSnapshot : BaseEntity
{
    public required string ReportType { get; set; } // "EnrollmentByClass" | "FeeCollection"
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public required string ResultJson { get; set; }
    public Guid GeneratedByUserId { get; set; }
}
