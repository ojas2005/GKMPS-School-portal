using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Fee.Entities;

/// <summary>
/// A student's fee account. Opened at admission (or by the office for a student admitted
/// before automatic billing), it is what makes the billing job generate monthly and exam
/// fees for the student. Closed when the student leaves; reopening starts billing again from
/// that day, so the months away are never charged. The fees themselves are FeePayment rows.
/// </summary>
public class StudentFeeAccount : BaseEntity
{
    public Guid StudentId { get; set; }

    /// <summary>The first day fees are generated for (the admission date, or the reopening date).</summary>
    public DateTime BillingStartDate { get; set; }

    public bool IsOpen { get; set; } = true;
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedReason { get; set; }

    /// <summary>When the billing job last brought this account up to date.</summary>
    public DateTime? LastGeneratedAtUtc { get; set; }
}
