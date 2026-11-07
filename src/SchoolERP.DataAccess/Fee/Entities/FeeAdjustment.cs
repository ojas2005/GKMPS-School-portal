using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Fee.Entities;

/// <summary>
/// One line of a student's fee history that isn't a payment: a concession, an approved
/// waiver, a charge added by hand, a fee re-priced after a class change, the account being
/// opened or closed. Never updated or deleted. Payments have their own ledger
/// (PaymentTransaction).
/// </summary>
public class FeeAdjustment : BaseEntity
{
    public Guid StudentId { get; set; }

    /// <summary>The fee entry it concerns; null for account-level events.</summary>
    public Guid? FeePaymentId { get; set; }

    public required string Kind { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }

    public Guid? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
}
