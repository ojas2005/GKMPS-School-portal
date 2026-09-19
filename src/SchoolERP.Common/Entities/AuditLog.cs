namespace SchoolERP.Common.Entities;

/// <summary>
/// Records who did what, when, and the before/after state, for every admin action,
/// approval, and record deletion. Each module's database has its own AuditLog table, and
/// admin actions are also written as structured AUDIT lines via ILogger<T> (Serilog).
/// </summary>
public class AuditLog : BaseEntity
{
    public required string ActorUserId { get; set; }
    public required string ActorRole { get; set; }
    public required string Action { get; set; }          // e.g. "Student.Approve", "Fee.WaiverGranted"
    public required string EntityName { get; set; }
    public required string EntityId { get; set; }
    public string? BeforeStateJson { get; set; }
    public string? AfterStateJson { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
