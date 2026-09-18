namespace SchoolERP.Common.Audit;

/// <summary>One entry in the audit trail: who did what, to which record, from where, with what result.</summary>
public sealed record AuditRecord(
    DateTime TimestampUtc,
    string Action,
    Guid? ActorUserId = null,
    string? ActorRole = null,
    string? EntityId = null,
    int? StatusCode = null,
    string? IpAddress = null,
    string? Detail = null);

/// <summary>
/// The durable audit trail. Records are queued and written to the "audit" database in the
/// background, so recording never slows a request down. Kept for Audit:RetentionDays
/// (400 by default -- India's DPDP Rules ask for at least a year of processing logs).
/// </summary>
public interface IAuditTrail
{
    void Record(AuditRecord record);
}

/// <summary>Where queued records end up (the database in production, a fake in tests).</summary>
public interface IAuditSink
{
    Task WriteAsync(IReadOnlyList<AuditRecord> records, CancellationToken ct);
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct);
}
