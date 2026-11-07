namespace SchoolERP.DataAccess.Fee.Entities;

/// <summary>
/// What a fee entry is for. A fixed list rather than a table: billing rules hang off each
/// category (once at admission, every month, three exams a year, only with transport), so a
/// new category always needs code, never just a row.
/// </summary>
public static class FeeCategories
{
    public const string Admission = "Admission";
    public const string Tuition = "Tuition";
    public const string Exam = "Exam";
    public const string Transport = "Transport";
    /// <summary>Anything added by hand: an extra charge, previous dues, older fee structures.</summary>
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = [Admission, Tuition, Exam, Transport, Other];

    public static bool IsKnown(string? value) => All.Contains(value);
}

/// <summary>The three exams a year, as stored in FeePayment.PeriodKey for exam fees.</summary>
public static class ExamPeriods
{
    public const string Quarterly = "Quarterly";
    public const string HalfYearly = "HalfYearly";
    public const string Annual = "Annual";

    public static readonly IReadOnlyList<string> All = [Quarterly, HalfYearly, Annual];

    public static string Label(string period) => period switch
    {
        Quarterly => "Quarterly Examination",
        HalfYearly => "Half-Yearly Examination",
        Annual => "Annual Examination",
        _ => period
    };
}

/// <summary>Kinds of change recorded in the fee history (FeeAdjustment).</summary>
public static class FeeAdjustmentKinds
{
    public const string Concession = "Concession";
    public const string WaiverApproved = "WaiverApproved";
    public const string ChargeAdded = "ChargeAdded";
    public const string Repriced = "Repriced";
    public const string AccountOpened = "AccountOpened";
    public const string AccountClosed = "AccountClosed";
    public const string AccountReopened = "AccountReopened";
    public const string TransportChanged = "TransportChanged";
}
