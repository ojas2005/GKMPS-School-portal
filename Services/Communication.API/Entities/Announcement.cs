using SchoolERP.Shared.Entities;

namespace SchoolERP.Communication.Entities;

/// <summary>
/// TargetRolesCsv/TargetClassId let an announcement be scoped to everyone, one role
/// (e.g. "Teacher"), or one class -- resolved client-side by the Angular app filtering
/// on the logged-in user's role/class, since Communication.API doesn't own user records.
/// </summary>
public class Announcement : BaseEntity
{
    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? TargetRolesCsv { get; set; }   // null/empty = everyone
    public string? TargetClassId { get; set; }     // null = all classes

    public Guid PostedByUserId { get; set; }
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
}
