namespace SchoolERP.Shared.Events;

/// <summary>
/// Published by Student.API (via the transactional Outbox) after a new admission is
/// finalized. Consumed by Notification.API to send a welcome email/SMS, and by
/// Reporting.API to update enrollment aggregates.
/// </summary>
public record StudentEnrolledEvent
{
    public Guid StudentId { get; init; }
    public required string AdmissionNumber { get; init; }
    public required string FullName { get; init; }
    public required string ClassId { get; init; }
    public required string SectionId { get; init; }
    public string? ParentEmail { get; init; }
    public DateTime EnrolledAtUtc { get; init; } = DateTime.UtcNow;
}
