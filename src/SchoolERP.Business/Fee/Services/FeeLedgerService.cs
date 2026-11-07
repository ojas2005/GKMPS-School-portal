using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Business.Student.DTOs;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Transport.Services.Interfaces;
using SchoolERP.Common;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;

namespace SchoolERP.Business.Fee.Services;

/// <summary>
/// The fee dashboard, lists, monthly report and statements. Totals are computed in the
/// database; names come from the Student module (which owns them) for the rows on screen.
/// "Pending" means owed and already due; fees created for later in the session are "upcoming".
/// </summary>
public class FeeLedgerService : IFeeLedgerService
{
    private readonly IFeePaymentRepository _payments;
    private readonly IFeeAccountRepository _accounts;
    private readonly IFeePaymentService _paymentService;
    private readonly IStudentService _students;
    private readonly IStudentRouteMappingService _transport;
    private readonly TimeProvider _clock;

    public FeeLedgerService(
        IFeePaymentRepository payments,
        IFeeAccountRepository accounts,
        IFeePaymentService paymentService,
        IStudentService students,
        IStudentRouteMappingService transport,
        TimeProvider clock)
    {
        _payments = payments;
        _accounts = accounts;
        _paymentService = paymentService;
        _students = students;
        _transport = transport;
        _clock = clock;
    }

    private DateTime Today => SchoolClock.Today(_clock);

    public async Task<PagedResult<FeeEntryDto>> SearchEntriesAsync(FeeEntryQuery query, CancellationToken ct = default)
    {
        var (page, size) = Paging(query);
        var filter = await BuildFilterAsync(query, ct);
        if (filter is null) return Empty<FeeEntryDto>(page, size);

        var (items, total) = await _payments.SearchAsync(filter, page, size, ct);
        var students = await NamesAsync(items.Select(i => i.StudentId), ct);
        var today = Today;
        return new PagedResult<FeeEntryDto>
        {
            Items = items.Select(p => ToEntry(p, students.GetValueOrDefault(p.StudentId), today)).ToList(),
            PageNumber = page, PageSize = size, TotalCount = total
        };
    }

    public async Task<PagedResult<StudentFeeBalanceDto>> StudentBalancesAsync(FeeEntryQuery query, CancellationToken ct = default)
    {
        var (page, size) = Paging(query);
        var filter = await BuildFilterAsync(query, ct);
        if (filter is null) return Empty<StudentFeeBalanceDto>(page, size);

        var (items, total) = await _payments.StudentTotalsAsync(filter, page, size, ct);
        var students = await NamesAsync(items.Select(i => i.StudentId), ct);
        return new PagedResult<StudentFeeBalanceDto>
        {
            Items = items.Select(t =>
            {
                var s = students.GetValueOrDefault(t.StudentId);
                var classId = s?.ClassId ?? t.ClassId;
                return new StudentFeeBalanceDto(t.StudentId, s?.FullName, s?.AdmissionNumber, classId, SchoolClasses.NameOf(classId), s?.Status,
                    t.Billed, t.Paid, t.Discount, t.PendingDue, t.Upcoming, t.OldestUnpaidDueDate);
            }).ToList(),
            PageNumber = page, PageSize = size, TotalCount = total
        };
    }

    public async Task<FeeOverviewDto> OverviewAsync(string? academicYear, CancellationToken ct = default)
    {
        var today = Today;
        var session = SessionOf(academicYear, today);
        var year = AcademicYears.Format(session);

        var byCategory = await _payments.CategoryTotalsAsync(new FeeEntryFilter { AcademicYear = year, Today = today }, ct);
        // Pending and upcoming across every session, including fees from before this one.
        var everything = await _payments.CategoryTotalsAsync(new FeeEntryFilter { Today = today }, ct);

        var (sessionFrom, sessionTo) = SessionUtcRange(session);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var collectedSession = await _payments.GetTotalCollectedAsync(sessionFrom, sessionTo, ct);
        var collectedMonth = await _payments.GetTotalCollectedAsync(ToUtc(monthStart), ToUtc(monthStart.AddMonths(1)), ct);
        var collectedToday = await _payments.GetTotalCollectedAsync(ToUtc(today), ToUtc(today.AddDays(1)), ct);

        var accounts = await _accounts.GetAllAsync(ct);
        var open = accounts.Count(a => a.IsOpen);
        var current = (await _students.GetActiveCountByClassAsync(ct)).Values.Sum();

        return new FeeOverviewDto(year, collectedSession, collectedMonth, collectedToday,
            everything.Sum(c => c.PendingDue), everything.Sum(c => c.Upcoming), byCategory.Sum(c => c.Discount),
            FeeCategories.All.Select(cat => byCategory.FirstOrDefault(c => c.Category == cat) is { } c
                    ? new FeeCategoryTotalsDto(cat, c.Billed, c.Paid, c.Discount, c.PendingDue, c.Upcoming)
                    : new FeeCategoryTotalsDto(cat, 0, 0, 0, 0, 0)).ToList(),
            open, Math.Max(0, current - open));
    }

    public async Task<IReadOnlyList<MonthlyCollectionDto>> MonthlyCollectionsAsync(string? academicYear, CancellationToken ct = default)
    {
        var session = SessionOf(academicYear, Today);
        var first = new DateTime(session, AcademicYears.FirstMonth, 1);
        var billed = await _payments.BilledByMonthAsync(first, first.AddMonths(12).AddDays(-1), ct);
        var (fromUtc, toUtc) = SessionUtcRange(session);
        var collected = await _payments.CollectedByMonthAsync(fromUtc, toUtc, ct);

        return Enumerable.Range(0, 12).Select(i =>
        {
            var m = first.AddMonths(i);
            return new MonthlyCollectionDto(m.Year, m.Month, FeeBillingPlanner.MonthLabel(m),
                billed.Where(b => b.Year == m.Year && b.Month == m.Month).Sum(b => b.Amount),
                collected.Where(c => c.Year == m.Year && c.Month == m.Month).Sum(c => c.Amount));
        }).ToList();
    }

    public async Task<FeeStatementDto?> StatementAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await _students.GetByIdAsync(studentId, ct);
        if (student is null) return null;

        var today = Today;
        var account = await _accounts.FindByStudentAsync(studentId, ct);
        var transport = await _transport.GetForStudentAsync(studentId, ct);
        var fees = await _payments.FindByStudentAsync(studentId, ct);
        var entries = fees.Select(f => ToEntry(f, student, today)).ToList();
        var labels = entries.ToDictionary(e => e.Id, e => e.Label);
        var history = (await _accounts.GetAdjustmentsAsync(studentId, ct))
            .Select(a => new FeeHistoryDto(a.Id, a.Kind, a.Amount, a.Reason, a.FeePaymentId,
                a.FeePaymentId is { } id ? labels.GetValueOrDefault(id) : null, a.ActorRole, a.CreatedAtUtc))
            .ToList();

        return new FeeStatementDto(
            new FeeStudentDto(student.Id, student.FullName, student.AdmissionNumber, student.ClassId, SchoolClasses.NameOf(student.ClassId), student.Status),
            account is null ? null : new FeeAccountDto(account.IsOpen, account.BillingStartDate, account.ClosedAtUtc, account.ClosedReason, account.LastGeneratedAtUtc),
            transport is null ? null : new FeeTransportDto(transport.RouteId, transport.RouteName, transport.MonthlyFee, transport.PickupPoint, transport.RouteActive),
            new FeeTotalsDto(
                entries.Where(e => e.IsDue).Sum(e => e.TotalAmount),
                entries.Sum(e => e.PaidAmount),
                entries.Sum(e => e.DiscountAmount),
                entries.Where(e => e.IsDue).Sum(e => e.Pending),
                entries.Where(e => !e.IsDue).Sum(e => e.Pending)),
            entries,
            await _paymentService.GetTransactionsForStudentAsync(studentId, ct),
            history);
    }

    /// <summary>The filter for a query, or null when a keyword matches no student (nothing to show).</summary>
    private async Task<FeeEntryFilter?> BuildFilterAsync(FeeEntryQuery q, CancellationToken ct)
    {
        IReadOnlyCollection<Guid>? studentIds = null;
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var matches = await _students.SearchAsync(null, null, q.Keyword.Trim(), RosterFilter.All, 1, 200, ct);
            if (matches.Items.Count == 0) return null;
            studentIds = matches.Items.Select(s => s.Id).ToList();
        }

        int? dueYear = null, dueMonth = null;
        if (!string.IsNullOrWhiteSpace(q.Month))
        {
            if (!DateTime.TryParseExact(q.Month + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var m))
                throw new InvalidOperationException("Month must look like 2026-11.");
            (dueYear, dueMonth) = (m.Year, m.Month);
        }

        if (!string.IsNullOrWhiteSpace(q.Category) && !FeeCategories.IsKnown(q.Category))
            throw new InvalidOperationException($"Unknown fee category '{q.Category}'.");

        var today = Today;
        return new FeeEntryFilter
        {
            StudentIds = studentIds,
            ClassId = string.IsNullOrWhiteSpace(q.ClassId) ? null : q.ClassId,
            AcademicYear = string.IsNullOrWhiteSpace(q.AcademicYear) ? null : AcademicYears.Normalize(q.AcademicYear),
            Category = string.IsNullOrWhiteSpace(q.Category) ? null : q.Category,
            DueYear = dueYear, DueMonth = dueMonth,
            Status = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status,
            DueOnOrBefore = q.IncludeUpcoming ? null : today,
            OnlyOwing = q.OnlyOwing,
            Today = today
        };
    }

    private async Task<Dictionary<Guid, StudentSummary>> NamesAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        (await _students.GetByIdsAsync(ids.Distinct().ToList(), ct)).ToDictionary(s => s.Id);

    internal static FeeEntryDto ToEntry(FeePayment p, StudentSummary? student, DateTime today)
    {
        var pending = FeeMath.Pending(p);
        var isDue = p.DueDate is null || p.DueDate <= today;
        return new FeeEntryDto(p.Id, p.StudentId, student?.FullName, student?.AdmissionNumber,
            p.ClassId, SchoolClasses.NameOf(p.ClassId),
            p.Category, p.AcademicYear, p.PeriodKey, LabelOf(p), p.Description,
            p.DueDate ?? p.FeeStructure?.DueDateUtc.Date,
            p.TotalAmount, p.PaidAmount, p.WaiverAmount, pending,
            p.Status, isDue, pending > 0 && p.DueDate is { } d && d < today, p.LastPaidAtUtc,
            p.IsWaiverRequested && !p.IsWaiverApproved, p.FeeStructureId);
    }

    internal static string LabelOf(FeePayment p) => p.Category switch
    {
        FeeCategories.Admission => "Admission fee",
        FeeCategories.Tuition => $"Tuition fee – {p.PeriodLabel}",
        FeeCategories.Transport => $"Transport fee – {p.PeriodLabel}",
        FeeCategories.Exam => p.PeriodLabel ?? "Examination fee",
        _ => p.FeeStructure?.Name ?? p.Description ?? p.PeriodLabel ?? "Fee"
    };

    private static int SessionOf(string? academicYear, DateTime today)
    {
        var normalized = AcademicYears.Normalize(academicYear);
        return normalized.Length >= 4 && int.TryParse(normalized[..4], out var y) ? y : FeeBillingPlanner.SessionStartYear(today);
    }

    private static DateTime ToUtc(DateTime schoolDate) => DateTime.SpecifyKind(schoolDate - SchoolClock.IndiaOffset, DateTimeKind.Utc);

    private static (DateTime From, DateTime To) SessionUtcRange(int session)
    {
        var from = new DateTime(session, AcademicYears.FirstMonth, 1);
        return (ToUtc(from), ToUtc(from.AddMonths(12)));
    }

    private static (int Page, int Size) Paging(FeeEntryQuery q) =>
        (q.Page < 1 ? 1 : q.Page, q.PageSize is < 1 or > 200 ? 50 : q.PageSize);

    private static PagedResult<T> Empty<T>(int page, int size) => new() { Items = [], PageNumber = page, PageSize = size, TotalCount = 0 };
}
