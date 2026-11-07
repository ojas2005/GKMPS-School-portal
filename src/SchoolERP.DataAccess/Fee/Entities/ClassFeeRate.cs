using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Fee.Entities;

/// <summary>
/// The fee structure for one class: what a student of this class is charged at admission,
/// every month, and for each of the three exams. One row per class, edited in place: a fee
/// entry copies the amount when it is created, so changing a rate only affects fees created
/// afterwards. Every change is written to the audit trail.
/// </summary>
public class ClassFeeRate : BaseEntity
{
    public required string ClassId { get; set; }
    public decimal AdmissionFee { get; set; }
    public decimal MonthlyTuition { get; set; }
    public decimal QuarterlyExamFee { get; set; }
    public decimal HalfYearlyExamFee { get; set; }
    public decimal AnnualExamFee { get; set; }
}
