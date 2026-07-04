using SchoolERP.Shared.Entities;

namespace SchoolERP.Transport.Entities;

/// <summary>One active route assignment per student -- unique index on StudentId enforces that a student can't be mapped to two routes at once.</summary>
public class StudentRouteMapping : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid RouteId { get; set; }
    public required string PickupPoint { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
