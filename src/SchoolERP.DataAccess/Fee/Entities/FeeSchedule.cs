using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Fee.Entities;

/// <summary>
/// When fees fall due: the day of the month for monthly fees, and the date in the session
/// each exam fee is due. A single row. Months from January to March belong to the second
/// calendar year of the session (the session runs April to March).
/// </summary>
public class FeeSchedule : BaseEntity
{
    public int MonthlyDueDay { get; set; } = 10;

    public int QuarterlyExamDueMonth { get; set; } = 9;
    public int QuarterlyExamDueDay { get; set; } = 10;

    public int HalfYearlyExamDueMonth { get; set; } = 12;
    public int HalfYearlyExamDueDay { get; set; } = 10;

    public int AnnualExamDueMonth { get; set; } = 2;
    public int AnnualExamDueDay { get; set; } = 10;
}
