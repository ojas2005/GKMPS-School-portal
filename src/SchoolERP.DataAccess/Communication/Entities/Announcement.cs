using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Communication.Entities;

/// <summary>
/// TargetRolesCsv/TargetClassId let an announcement be scoped to everyone, one role
/// (e.g. "Teacher"), or one class. Non-admin callers are matched against the role/class
/// claims in their token (see AnnouncementsController).
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
