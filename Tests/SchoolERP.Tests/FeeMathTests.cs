using SchoolERP.Business.Fee;
using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.Tests;

public class FeeMathTests
{
    private static FeePayment Due(decimal total, decimal paid = 0, decimal waiver = 0) =>
        new() { TotalAmount = total, PaidAmount = paid, WaiverAmount = waiver };

    [Fact]
    public void Paying_part_of_a_fee_leaves_the_rest_pending()
    {
        Assert.Equal(7000, FeeMath.Pending(Due(12000, paid: 5000)));
    }

    [Fact]
    public void A_waiver_counts_towards_clearing_the_due()
    {
        Assert.Equal(2000, FeeMath.Pending(Due(12000, paid: 5000, waiver: 5000)));
    }

    [Fact]
    public void Overpayment_never_reports_a_negative_pending()
    {
        Assert.Equal(0, FeeMath.Pending(Due(1000, paid: 1500)));
    }

    [Fact]
    public void A_receipt_reports_every_other_due_the_student_still_owes()
    {
        // Clearing one fee in full doesn't mean the student owes nothing.
        var dues = new[] { Due(12000, paid: 12000), Due(8000, paid: 3000), Due(500) };

        Assert.Equal(5500, FeeMath.TotalPending(dues));
    }

    [Fact]
    public void A_student_with_no_dues_owes_nothing()
    {
        Assert.Equal(0, FeeMath.TotalPending(Array.Empty<FeePayment>()));
    }
}
