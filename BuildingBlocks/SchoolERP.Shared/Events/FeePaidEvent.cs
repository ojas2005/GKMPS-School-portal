namespace SchoolERP.Shared.Events;

/// <summary>
/// Published by Fee.API after a payment is committed atomically with the ledger update.
/// Consumed by Reporting.API to roll up collection totals and by Notification.API to
/// send a payment confirmation.
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
