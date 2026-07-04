namespace SchoolERP.Shared.Entities;

/// <summary>
/// Records who did what, when, and the before/after state, for every admin action,
/// approval, and record deletion. Each microservice owns its own AuditLog table
/// (schema-isolated, same as every other entity) and writes to it via ILogger<T> (Serilog)
/// sinks as well as this persisted record for queryable history.
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
