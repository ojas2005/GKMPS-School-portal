using SchoolERP.Business.Fee;
using SchoolERP.Business.Fee.Services;
using SchoolERP.Common;
using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.Tests;

public class FeeBillingPlannerTests
{
    // Class 10 defaults: ₹600 a month, ₹300 per exam; admission fee set to ₹2,000 for these tests.
    private static readonly FeeRates Class10 = new(AdmissionFee: 2000, MonthlyTuition: 600, QuarterlyExamFee: 300, HalfYearlyExamFee: 300, AnnualExamFee: 300);
    private static readonly FeeDueDates Due = FeeDueDates.Default; // 10th monthly; exams 10 Sep, 10 Dec, 10 Feb

    private static IReadOnlyList<PlannedFee> Plan(DateTime start, DateTime asOf, TransportPlan? transport = null, bool admission = true, FeeRates? rates = null) =>
        FeeBillingPlanner.Plan(start, asOf, rates ?? Class10, Due, transport, admission);

    [Fact]
    public void Admission_creates_the_admission_fee_this_months_tuition_and_the_years_three_exams()
    {
        var fees = Plan(new DateTime(2026, 5, 4), new DateTime(2026, 5, 4));

        Assert.Single(fees, f => f.Category == FeeCategories.Admission && f.Amount == 2000 && f.DueDate == new DateTime(2026, 5, 4));
        var tuition = Assert.Single(fees, f => f.Category == FeeCategories.Tuition);
        // The admission month is paid at admission: due that day, not on the 10th.
        Assert.Equal(("2026-05", "May 2026", 600m, new DateTime(2026, 5, 4)), (tuition.PeriodKey, tuition.PeriodLabel, tuition.Amount, tuition.DueDate));
        Assert.Equal(["Quarterly", "HalfYearly", "Annual"], fees.Where(f => f.Category == FeeCategories.Exam).Select(f => f.PeriodKey));
        Assert.All(fees, f => Assert.Equal("2026-27", f.AcademicYear));
    }

    [Fact]
    public void Future_months_are_never_billed_ahead()
    {
        var fees = Plan(new DateTime(2026, 5, 4), new DateTime(2026, 5, 31));
        Assert.Single(fees, f => f.Category == FeeCategories.Tuition);
    }

    [Fact]
    public void Each_month_up_to_today_gets_one_tuition_fee()
    {
        var fees = Plan(new DateTime(2026, 5, 4), new DateTime(2026, 11, 7), admission: false);
        Assert.Equal(["2026-05", "2026-06", "2026-07", "2026-08", "2026-09", "2026-10", "2026-11"],
            fees.Where(f => f.Category == FeeCategories.Tuition).Select(f => f.PeriodKey));
        Assert.DoesNotContain(fees, f => f.Category == FeeCategories.Admission);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(22)]
    public void The_first_months_fee_is_due_on_the_admission_date(int day)
    {
        var tuition = Plan(new DateTime(2026, 7, day), new DateTime(2026, 7, day)).Single(f => f.Category == FeeCategories.Tuition);
        Assert.Equal(new DateTime(2026, 7, day), tuition.DueDate);
    }

    [Fact]
    public void Later_months_fall_due_on_the_schedules_day()
    {
        var aug = Plan(new DateTime(2026, 7, 22), new DateTime(2026, 8, 1), admission: false).Single(f => f.PeriodKey == "2026-08");
        Assert.Equal(new DateTime(2026, 8, 10), aug.DueDate);
    }

    [Fact]
    public void Exams_already_past_at_admission_are_not_charged()
    {
        var exams = Plan(new DateTime(2027, 1, 5), new DateTime(2027, 1, 5)).Where(f => f.Category == FeeCategories.Exam).ToList();
        var annual = Assert.Single(exams);
        Assert.Equal(("Annual", new DateTime(2027, 2, 10), "2026-27"), (annual.PeriodKey, annual.DueDate, annual.AcademicYear));
    }

    [Fact]
    public void A_new_session_brings_its_own_three_exam_fees()
    {
        var fees = Plan(new DateTime(2026, 5, 4), new DateTime(2027, 4, 2), admission: false);
        Assert.Equal(3, fees.Count(f => f.Category == FeeCategories.Exam && f.AcademicYear == "2026-27"));
        Assert.Equal(3, fees.Count(f => f.Category == FeeCategories.Exam && f.AcademicYear == "2027-28"));
        Assert.Contains(fees, f => f.Category == FeeCategories.Tuition && f.PeriodKey == "2027-04" && f.AcademicYear == "2027-28");
        Assert.Contains(fees, f => f.Category == FeeCategories.Tuition && f.PeriodKey == "2027-03" && f.AcademicYear == "2026-27");
    }

    [Fact]
    public void Transport_is_billed_monthly_only_from_the_month_the_student_joined_it()
    {
        var route = new TransportPlan("Route 3", 700, new DateTime(2026, 8, 15));
        var transport = Plan(new DateTime(2026, 5, 4), new DateTime(2026, 9, 1), route, admission: false)
            .Where(f => f.Category == FeeCategories.Transport).ToList();
        Assert.Equal(["2026-08", "2026-09"], transport.Select(f => f.PeriodKey));
        Assert.All(transport, f => Assert.Equal(("Route 3", 700m), (f.Description, f.Amount)));
        Assert.Equal(new DateTime(2026, 8, 15), transport[0].DueDate); // the day they joined
        Assert.Equal(new DateTime(2026, 9, 10), transport[1].DueDate); // then the usual day
    }

    [Fact]
    public void No_transport_means_no_transport_fees()
    {
        Assert.DoesNotContain(Plan(new DateTime(2026, 5, 4), new DateTime(2026, 9, 1)), f => f.Category == FeeCategories.Transport);
    }

    [Fact]
    public void Fees_of_zero_are_not_created()
    {
        var fees = Plan(new DateTime(2026, 5, 4), new DateTime(2026, 5, 4), rates: new FeeRates(0, 400, 0, 400, 0));
        Assert.DoesNotContain(fees, f => f.Category == FeeCategories.Admission);
        Assert.Equal(["HalfYearly"], fees.Where(f => f.Category == FeeCategories.Exam).Select(f => f.PeriodKey));
    }

    [Fact]
    public void Planning_twice_gives_the_same_keys_so_the_unique_index_prevents_doubles()
    {
        var a = Plan(new DateTime(2026, 5, 4), new DateTime(2026, 11, 7)).Select(f => f.Key);
        var b = Plan(new DateTime(2026, 5, 4), new DateTime(2026, 11, 7)).Select(f => f.Key);
        Assert.Equal(a, b);
        Assert.Equal(a.Count(), a.Distinct().Count());
    }

    [Fact]
    public void Due_days_are_pulled_into_short_months()
    {
        var due = new FeeDueDates(31, 9, 31, 12, 10, 2, 30);
        Assert.Equal(new DateTime(2026, 9, 30), due.ExamDueDate(ExamPeriods.Quarterly, 2026));
        Assert.Equal(new DateTime(2027, 2, 28), due.ExamDueDate(ExamPeriods.Annual, 2026));
    }

    [Theory]
    [InlineData("PG", 400, 400)]
    [InlineData("Class 5", 400, 400)]
    [InlineData("Class 6", 500, 700)]
    [InlineData("Class 8", 500, 700)]
    [InlineData("Class 9", 600, 300)]
    [InlineData("Class 10", 600, 300)]
    public void Default_fee_structure_follows_the_schools_bands(string className, decimal tuition, decimal exam)
    {
        var rate = FeeSetupService.DefaultRate(SchoolClasses.All.Single(c => c.Name == className));
        Assert.Equal((tuition, exam, exam, exam, 0m), (rate.MonthlyTuition, rate.QuarterlyExamFee, rate.HalfYearlyExamFee, rate.AnnualExamFee, rate.AdmissionFee));
    }

    [Fact]
    public void School_date_is_India_time()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 31, 20, 0, 0, TimeSpan.Zero)); // 01:30 IST on 1 Nov
        Assert.Equal(new DateTime(2026, 11, 1), SchoolClock.Today(clock));
    }

    [Fact]
    public void Fee_status_follows_payments_and_discounts()
    {
        Assert.Equal(FeeStatuses.Pending, FeeStatuses.Of(600, 0, 0));
        Assert.Equal(FeeStatuses.PartiallyPaid, FeeStatuses.Of(600, 200, 0));
        Assert.Equal(FeeStatuses.Paid, FeeStatuses.Of(600, 500, 100));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
