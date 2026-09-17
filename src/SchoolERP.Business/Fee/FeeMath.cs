using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.Business.Fee;

/// <summary>
/// The one definition of "pending": what's left after payments and waivers, never negative.
/// Receipts, the class summary and a student's ledger all read from here so they can't drift.
/// </summary>
public static class FeeMath
{
    public static decimal Pending(decimal totalAmount, decimal paidAmount, decimal waiverAmount) =>
        Math.Max(0, totalAmount - paidAmount - waiverAmount);

    public static decimal Pending(FeePayment due) =>
        Pending(due.TotalAmount, due.PaidAmount, due.WaiverAmount);

    /// <summary>Everything a student still owes across every due of theirs.</summary>
    public static decimal TotalPending(IEnumerable<FeePayment> dues) =>
        dues.Sum(Pending);
}
