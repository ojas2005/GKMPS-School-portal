namespace SchoolERP.Common.Events;

/// <summary>
/// Raised by the Identity module after a new account is created (any role). Handled by the
/// Notification module to send a welcome email.
/// </summary>
public record UserRegisteredEvent
{
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public DateTime RegisteredAtUtc { get; init; } = DateTime.UtcNow;
}
