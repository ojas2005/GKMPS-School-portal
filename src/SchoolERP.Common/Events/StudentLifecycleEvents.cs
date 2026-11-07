namespace SchoolERP.Common.Events;

/// <summary>
/// Raised by the Student module when a student leaves the rolls (removed, transfer
/// certificate issued) or comes back. The Fee module stops or resumes billing them.
/// </summary>
public record StudentStatusChangedEvent
{
    public Guid StudentId { get; init; }
    public required string Status { get; init; }
    public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Raised by the Student module when a student moves to another class. The Fee module
/// re-prices the student's fees that haven't fallen due yet; history is left alone.
/// </summary>
public record StudentClassChangedEvent
{
    public Guid StudentId { get; init; }
    public required string OldClassId { get; init; }
    public required string NewClassId { get; init; }
    public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Raised by the Transport module when a student is put on a route, moved to another, or
/// taken off transport (NewRouteName null). The Fee module records it in the fee history.
/// </summary>
public record StudentTransportChangedEvent
{
    public Guid StudentId { get; init; }
    public string? OldRouteName { get; init; }
    public string? NewRouteName { get; init; }
    public decimal NewMonthlyFee { get; init; }
    public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;
}
