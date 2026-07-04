namespace SchoolERP.Shared.Events;

/// <summary>
/// Published by Identity.API after a new account is created (any role). Consumed by
/// Notification.API to send a "welcome / set your password" email.
/// </summary>
public record UserRegisteredEvent
{
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public DateTime RegisteredAtUtc { get; init; } = DateTime.UtcNow;
}
