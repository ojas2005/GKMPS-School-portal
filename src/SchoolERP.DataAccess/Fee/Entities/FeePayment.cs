using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Fee.Entities;

/// <summary>
/// One fee on a student's account: a charge with its own amount, due date and payment
/// status. Generated fees (admission, each month's tuition and transport, each exam) carry
/// Category + AcademicYear + PeriodKey, which a unique index keeps from ever being created
/// twice. Charges added by hand and fees from the older free-form fee structures
/// (FeeStructureId set) have no PeriodKey.
///
/// The amount is copied in when the fee is created, so changing the fee structure later
/// never alters it. PaidAmount and WaiverAmount (concessions and approved waivers) are only
/// ever moved by atomic, guarded UPDATEs -- never a load-then-save.
/// </summary>
public class FeePayment : BaseEntity
{
    public Guid StudentId { get; set; }

    /// <summary>Set only for fees from the older free-form fee structures.</summary>
    public Guid? FeeStructureId { get; set; }
    public FeeStructure? FeeStructure { get; set; }

    /// <summary>The student's class when the fee was created (a frontend-owned GUID string),
    /// so fees can be filtered by class without asking the Student module.</summary>
    public string? ClassId { get; set; }

    /// <summary>One of <see cref="FeeCategories"/>.</summary>
    public string Category { get; set; } = FeeCategories.Other;

    /// <summary>The session the fee belongs to, e.g. "2026-27".</summary>
    public string? AcademicYear { get; set; }

    /// <summary>Which instance of a generated fee this is: "2026-11" for a month, "Quarterly"
    /// for an exam, "Admission" for the admission fee. Null for charges added by hand.</summary>
    public string? PeriodKey { get; set; }

    /// <summary>Free text for charges added by hand, e.g. "Previous dues at admission".</summary>
    public string? Description { get; set; }

    /// <summary>Human-facing period, e.g. "November 2026" or "Quarterly Examination 2026-27".</summary>
    public string? PeriodLabel { get; set; }

    /// <summary>The calendar date the fee falls due (school time). Before it, the fee is upcoming.</summary>
    public DateTime? DueDate { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? LastPaidAtUtc { get; set; }

    public bool IsWaiverRequested { get; set; } = false;
    public bool IsWaiverApproved { get; set; } = false;
    /// <summary>Everything taken off this fee: approved waivers plus concessions.</summary>
    public decimal WaiverAmount { get; set; } = 0;
    public Guid? WaiverApprovedByUserId { get; set; }

    public string Status => FeeStatuses.Of(TotalAmount, PaidAmount, WaiverAmount);
}

public static class FeeStatuses
{
    public const string Pending = "Pending";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";

    public static string Of(decimal total, decimal paid, decimal waiver) =>
        paid + waiver >= total ? Paid : paid > 0 ? PartiallyPaid : Pending;
}
