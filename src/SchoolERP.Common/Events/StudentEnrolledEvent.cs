namespace SchoolERP.Common.Events;

/// <summary>
/// Raised by the Student module after a new admission is saved. Handled by the
/// Notification module to email the parent an admission confirmation.
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
