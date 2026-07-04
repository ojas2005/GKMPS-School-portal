using SchoolERP.Shared.Entities;

namespace SchoolERP.Notification.Entities;

/// <summary>
/// One row per dispatched (or attempted) notification -- the durable record behind
/// every event this service consumes from RabbitMQ. DispatchChannel/IsDelivered let
/// the dashboard show what actually went out vs. what merely got attempted.
/// </summary>
public class NotificationLog : BaseEntity
{
    public required string EventType { get; set; }     // "UserRegistered" | "StudentEnrolled" | "FeePaid" | "CertificateGenerated"
    public required string RecipientReference { get; set; } // email/phone/userId depending on channel
    public required string DispatchChannel { get; set; }    // Email | SMS | Push
    public required string PayloadJson { get; set; }

    public bool IsDelivered { get; set; } = false;
    public DateTime? DeliveredAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
