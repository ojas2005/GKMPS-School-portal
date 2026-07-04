using SchoolERP.Shared.Entities;

namespace SchoolERP.Fee.Entities;

/// <summary>Immutable ledger row for a single payment (never updated after creation) -- the audit trail behind FeePayment.PaidAmount.</summary>
public class PaymentTransaction : BaseEntity
{
    public Guid FeePaymentId { get; set; }
    public decimal Amount { get; set; }
    public required string ReceiptNumber { get; set; }
    public required string PaymentMethod { get; set; } // Cash | Card | UPI | BankTransfer | Gateway
    public string? GatewayReference { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public string? ReceiptBlobPath { get; set; }
}
