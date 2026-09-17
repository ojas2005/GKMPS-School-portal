using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Reporting.Entities;

/// <summary>
/// A durable record of every generated report -- who ran it, over what range, and the
/// resulting figures (ResultJson) -- so historical reports remain reproducible even if
/// the source data in other modules later changes. This is the Reporting module's own
/// data; the underlying numbers are always fetched fresh through the owning module's
/// business service, never joined across databases.
/// </summary>
public class ReportSnapshot : BaseEntity
{
    public required string ReportType { get; set; } // "EnrollmentByClass" | "FeeCollection"
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public required string ResultJson { get; set; }
    public Guid GeneratedByUserId { get; set; }
}
