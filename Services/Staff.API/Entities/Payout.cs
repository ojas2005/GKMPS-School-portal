using SchoolERP.Shared.Entities;

namespace SchoolERP.Staff.Entities;

/// <summary>
/// One salary/payout ledger entry for a staff member, recorded by the owner/admin.
/// Immutable once written (corrections are new entries), mirroring Fee.API's
/// payment-transaction ledger approach.
/// </summary>
public class Payout : BaseEntity
{
    public Guid StaffId { get; set; }
    public StaffProfile? Staff { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Human-facing pay period, e.g. "July 2026".</summary>
    public required string PeriodLabel { get; set; }

    public DateTime PaidOnUtc { get; set; } = DateTime.UtcNow;

    /// <summary>BankTransfer | Cash | Cheque | UPI.</summary>
    public string Method { get; set; } = "BankTransfer";

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid RecordedByUserId { get; set; }
}
