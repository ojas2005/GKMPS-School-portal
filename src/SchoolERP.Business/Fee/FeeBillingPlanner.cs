using System.Globalization;
using SchoolERP.Common;
using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.Business.Fee;

/// <summary>What a class is charged (copied from ClassFeeRate).</summary>
public sealed record FeeRates(decimal AdmissionFee, decimal MonthlyTuition, decimal QuarterlyExamFee, decimal HalfYearlyExamFee, decimal AnnualExamFee)
{
    // Rounded to paise: the columns are decimal(65,30), which would otherwise carry 30 places into every fee.
    public static FeeRates From(ClassFeeRate r) => new(Inr(r.AdmissionFee), Inr(r.MonthlyTuition), Inr(r.QuarterlyExamFee), Inr(r.HalfYearlyExamFee), Inr(r.AnnualExamFee));

    public static decimal Inr(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    public decimal ExamFee(string period) => period switch
    {
        ExamPeriods.Quarterly => QuarterlyExamFee,
        ExamPeriods.HalfYearly => HalfYearlyExamFee,
        ExamPeriods.Annual => AnnualExamFee,
        _ => 0
    };
}

/// <summary>When fees fall due (copied from FeeSchedule).</summary>
public sealed record FeeDueDates(int MonthlyDueDay, int QuarterlyMonth, int QuarterlyDay, int HalfYearlyMonth, int HalfYearlyDay, int AnnualMonth, int AnnualDay)
{
    public static readonly FeeDueDates Default = From(new FeeSchedule());

    public static FeeDueDates From(FeeSchedule s) => new(s.MonthlyDueDay,
        s.QuarterlyExamDueMonth, s.QuarterlyExamDueDay, s.HalfYearlyExamDueMonth, s.HalfYearlyExamDueDay,
        s.AnnualExamDueMonth, s.AnnualExamDueDay);

    /// <summary>The due date of an exam fee in the session starting April of <paramref name="sessionStartYear"/>.</summary>
    public DateTime ExamDueDate(string period, int sessionStartYear)
    {
        var (month, day) = period switch
        {
            ExamPeriods.Quarterly => (QuarterlyMonth, QuarterlyDay),
            ExamPeriods.HalfYearly => (HalfYearlyMonth, HalfYearlyDay),
            _ => (AnnualMonth, AnnualDay)
        };
        var year = month >= AcademicYears.FirstMonth ? sessionStartYear : sessionStartYear + 1;
        return FeeBillingPlanner.Clamp(year, month, day);
    }
}

/// <summary>Transport the student uses: the route's current monthly charge, from which month.</summary>
public sealed record TransportPlan(string RouteName, decimal MonthlyFee, DateTime StartDate)
{
    public decimal MonthlyFee { get; } = FeeRates.Inr(MonthlyFee);
}

/// <summary>One fee the student should have. (Category, AcademicYear, PeriodKey) identifies it.</summary>
public sealed record PlannedFee(string Category, string AcademicYear, string PeriodKey, string PeriodLabel, string? Description, DateTime DueDate, decimal Amount)
{
    public (string, string, string) Key => (Category, AcademicYear, PeriodKey);
}

/// <summary>
/// The billing rules, in one pure function so the admission preview, admission itself and the
/// monthly billing job always agree:
/// <list type="bullet">
/// <item>Admission fee: once, due the day billing starts (only when asked for).</item>
/// <item>Tuition and transport: one fee per month, from the month billing starts up to the
/// month of <c>asOf</c> -- never months ahead. Due on the schedule's day, except the first
/// month, which is due the day billing starts (it's part of what's paid at admission).
/// Transport only from the month the student joined it, that month due the day they joined.</item>
/// <item>Exam fees: the session's three exams are created together, as soon as the session is
/// reached, each with its own due date; exams already past when billing starts are skipped.</item>
/// </list>
/// Fees of zero are not created. The caller removes the ones that already exist.
/// </summary>
public static class FeeBillingPlanner
{
    // Spelled out rather than taken from a culture, so labels don't depend on the server's ICU data.
    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    public static string MonthLabel(DateTime month) => $"{MonthNames[month.Month - 1]} {month.Year}";

    public static IReadOnlyList<PlannedFee> Plan(DateTime billingStart, DateTime asOf, FeeRates rates, FeeDueDates due,
        TransportPlan? transport, bool includeAdmissionFee)
    {
        var start = billingStart.Date;
        var until = asOf.Date;
        var fees = new List<PlannedFee>();
        if (until < start) until = start;

        var firstSession = SessionStartYear(start);
        if (includeAdmissionFee && rates.AdmissionFee > 0)
            fees.Add(new PlannedFee(FeeCategories.Admission, AcademicYears.Format(firstSession), "Admission",
                $"Admission {AcademicYears.Format(firstSession)}", null, start, rates.AdmissionFee));

        for (var session = firstSession; session <= SessionStartYear(until); session++)
        {
            var year = AcademicYears.Format(session);

            foreach (var exam in ExamPeriods.All)
            {
                var amount = rates.ExamFee(exam);
                var dueDate = due.ExamDueDate(exam, session);
                if (amount > 0 && dueDate >= start)
                    fees.Add(new PlannedFee(FeeCategories.Exam, year, exam, $"{ExamPeriods.Label(exam)} {year}", null, dueDate, amount));
            }

            var sessionFirstMonth = new DateTime(session, AcademicYears.FirstMonth, 1);
            var firstMonth = Max(sessionFirstMonth, new DateTime(start.Year, start.Month, 1));
            var lastMonth = Min(sessionFirstMonth.AddMonths(11), new DateTime(until.Year, until.Month, 1));

            for (var month = firstMonth; month <= lastMonth; month = month.AddMonths(1))
            {
                var key = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                var label = MonthLabel(month);
                var isFirstMonth = month.Year == start.Year && month.Month == start.Month;
                var dueDate = isFirstMonth ? start : Clamp(month.Year, month.Month, due.MonthlyDueDay);

                if (rates.MonthlyTuition > 0)
                    fees.Add(new PlannedFee(FeeCategories.Tuition, year, key, label, null, dueDate, rates.MonthlyTuition));

                if (transport is { MonthlyFee: > 0 } t && month >= new DateTime(t.StartDate.Year, t.StartDate.Month, 1))
                {
                    var joinedThisMonth = month.Year == t.StartDate.Year && month.Month == t.StartDate.Month;
                    fees.Add(new PlannedFee(FeeCategories.Transport, year, key, label, t.RouteName,
                        joinedThisMonth ? Max(t.StartDate.Date, start) : dueDate, t.MonthlyFee));
                }
            }
        }

        return fees;
    }

    /// <summary>April-to-March: the calendar year the date's session started in.</summary>
    public static int SessionStartYear(DateTime date) =>
        date.Month >= AcademicYears.FirstMonth ? date.Year : date.Year - 1;

    /// <summary>A date, with the day pulled back into short months (the 31st of February is the 28th/29th).</summary>
    public static DateTime Clamp(int year, int month, int day) =>
        new(year, month, Math.Clamp(day, 1, DateTime.DaysInMonth(year, month)));

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
