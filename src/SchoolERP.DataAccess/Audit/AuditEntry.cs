namespace SchoolERP.DataAccess.Audit;

public class AuditEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    public string? EntityId { get; set; }
    public int? StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public string? Detail { get; set; }
}
