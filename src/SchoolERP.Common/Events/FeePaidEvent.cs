namespace SchoolERP.Common.Events;

/// <summary>
/// Raised by the Fee module after a payment is recorded. Handled by the Notification
/// module to send a payment confirmation.
/// </summary>
public record FeePaidEvent
{
    public Guid PaymentId { get; init; }
    public Guid StudentId { get; init; }
    public decimal AmountPaid { get; init; }
    public required string Currency { get; init; }
    public required string ReceiptNumber { get; init; }
    public DateTime PaidAtUtc { get; init; } = DateTime.UtcNow;
}
