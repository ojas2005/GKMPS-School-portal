using SchoolERP.Shared.Entities;

namespace SchoolERP.Fee.Entities;

/// <summary>
/// One row per student per due. Most rows are per FeeStructure (auto-assessed when a
/// student is admitted into a class, or backfilled later); FeeStructureId is null for
/// an ad-hoc due -- currently just the one-time "opening balance" a student can be
/// admitted with, carrying its own free-text Description instead. PaidAmount is bumped
/// atomically via ExecuteUpdateAsync on every payment -- never a load-then-save.
/// IsWaiverApproved follows the two-step submit/approve workflow for fee waivers.
/// </summary>
public class FeePayment : BaseEntity
{
    public Guid StudentId { get; set; }

    /// <summary>Null for an ad-hoc due (e.g. opening balance) not tied to a class FeeStructure.</summary>
    public Guid? FeeStructureId { get; set; }
    public FeeStructure? FeeStructure { get; set; }

    /// <summary>Denormalized from FeeStructure.ClassId (or set directly for ad-hoc dues) so
    /// dues can be grouped/searched by class without a join -- ClassId is a frontend-owned
    /// GUID string, not a foreign key into another service's table.</summary>
    public string? ClassId { get; set; }

    /// <summary>Set only for ad-hoc dues (FeeStructureId is null), e.g. "Opening balance at admission".</summary>
    public string? Description { get; set; }

    /// <summary>Human-facing period this due/payment covers, e.g. "May 2026 - Jun 2026".
    /// Set on ad-hoc dues submitted directly by the owner (see FeePaymentService.SubmitPaymentAsync);
    /// null for structure-linked dues, which use the FeeStructure's own AcademicYear/Name instead.</summary>
    public string? PeriodLabel { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public bool IsWaiverRequested { get; set; } = false;
    public bool IsWaiverApproved { get; set; } = false;
    public decimal WaiverAmount { get; set; } = 0;
    public Guid? WaiverApprovedByUserId { get; set; }

    public string Status => PaidAmount + WaiverAmount >= TotalAmount ? "Paid" : PaidAmount > 0 ? "PartiallyPaid" : "Due";
}
