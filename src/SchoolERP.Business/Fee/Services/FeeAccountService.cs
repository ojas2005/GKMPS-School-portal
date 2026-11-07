using Microsoft.EntityFrameworkCore;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Transport.Services.Interfaces;
using SchoolERP.Common;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;

namespace SchoolERP.Business.Fee.Services;

/// <summary>
/// Opens, bills and closes students' fee accounts. All amounts come from the fee structure
/// (ClassFeeRate, FeeSchedule) and the routes' charges at the moment a fee is created; the
/// rules for which fees exist live in <see cref="FeeBillingPlanner"/>, and the unique index on
/// (student, category, session, period) makes creating the same fee twice impossible.
/// </summary>
public class FeeAccountService : IFeeAccountService
{
    private readonly IFeeSetupRepository _setup;
    private readonly IFeeSetupService _setupService;
    private readonly IFeeAccountRepository _accounts;
    private readonly IFeePaymentRepository _payments;
    private readonly IStudentService _students;
    private readonly IStudentRouteMappingService _transport;
    private readonly IRouteService _routes;
    private readonly TimeProvider _clock;
    private readonly ILogger<FeeAccountService> _logger;

    public FeeAccountService(
        IFeeSetupRepository setup,
        IFeeSetupService setupService,
        IFeeAccountRepository accounts,
        IFeePaymentRepository payments,
        IStudentService students,
        IStudentRouteMappingService transport,
        IRouteService routes,
        TimeProvider clock,
        ILogger<FeeAccountService> logger)
    {
        _setup = setup;
        _setupService = setupService;
        _accounts = accounts;
        _payments = payments;
        _students = students;
        _transport = transport;
        _routes = routes;
        _clock = clock;
        _logger = logger;
    }

    private DateTime Today => SchoolClock.Today(_clock);

    public async Task<FeeBreakdown> PreviewAsync(string classId, Guid? routeId, DateTime? startDate, decimal? previousDues, CancellationToken ct = default)
    {
        var start = (startDate ?? Today).Date;
        var (rates, due) = await LoadStructureAsync(classId, ct);
        var transport = await ResolveRouteAsync(routeId, requireActive: true, ct);
        var planned = FeeBillingPlanner.Plan(start, start, rates, due, transport is null ? null : new TransportPlan(transport.RouteName, transport.MonthlyFee, start), includeAdmissionFee: true);
        return Breakdown(classId, start, rates, due, transport, planned, previousDues);
    }

    public async Task<FeeBreakdown> OpenAccountAsync(OpenFeeAccountCommand command, CancellationToken ct = default)
    {
        if (await _accounts.FindByStudentAsync(command.StudentId, ct) is not null)
            throw new InvalidOperationException("This student already has a fee account.");

        var start = command.BillingStartDate.Date;
        var (rates, due) = await LoadStructureAsync(command.ClassId, ct);
        var transport = await ResolveRouteAsync(command.RouteId, requireActive: false, ct);

        // Up to today: an account opened from an earlier date catches up on the months since.
        var asOf = Today > start ? Today : start;
        var planned = FeeBillingPlanner.Plan(start, asOf, rates, due,
            transport is null ? null : new TransportPlan(transport.RouteName, transport.MonthlyFee, start), command.ChargeAdmissionFee);

        var entries = planned.Select(f => ToEntity(f, command.StudentId, command.ClassId)).ToList();
        if (command.PreviousDues is > 0)
            entries.Add(new FeePayment
            {
                StudentId = command.StudentId, ClassId = command.ClassId, Category = FeeCategories.Other,
                AcademicYear = AcademicYears.Format(FeeBillingPlanner.SessionStartYear(start)),
                Description = "Previous dues at admission", PeriodLabel = "Previous dues",
                DueDate = start, TotalAmount = command.PreviousDues.Value
            });

        await _accounts.AddAsync(new StudentFeeAccount
        {
            StudentId = command.StudentId,
            BillingStartDate = start,
            LastGeneratedAtUtc = DateTime.UtcNow
        }, ct);
        await _payments.AddRangeAsync(entries, ct);
        await _accounts.AddAdjustmentAsync(new FeeAdjustment
        {
            StudentId = command.StudentId, Kind = FeeAdjustmentKinds.AccountOpened, Amount = entries.Sum(e => e.TotalAmount),
            Reason = $"Fee account opened from {start:dd MMM yyyy} in {SchoolClasses.NameOf(command.ClassId)}"
                     + (transport is null ? "" : $", transport on {transport.RouteName}"),
            ActorUserId = command.ActorUserId, ActorRole = command.ActorRole
        }, ct);

        // The account, its fees and the history line share one DbContext: one SaveChanges,
        // one transaction -- all of it is saved or none of it is.
        await _accounts.SaveChangesAsync(ct);

        _logger.LogInformation("Fee account opened for {StudentId}: {Count} fee(s), ₹{Total}", command.StudentId, entries.Count, entries.Sum(e => e.TotalAmount));
        return Breakdown(command.ClassId, start, rates, due, transport, planned, command.PreviousDues);
    }

    public async Task DiscardAccountAsync(Guid studentId, CancellationToken ct = default)
    {
        await using var tx = await _payments.BeginTransactionAsync(ct);
        await _payments.DeleteUnpaidForStudentAsync(studentId, ct);
        await _accounts.DeleteAdjustmentsAsync(studentId, ct);
        await _accounts.DeleteAsync(studentId, ct);
        await tx.CommitAsync(ct);
        _logger.LogWarning("Fee account discarded for {StudentId} (admission undone)", studentId);
    }

    public async Task<FeeBreakdown> OpenForExistingStudentAsync(Guid studentId, OpenFeeAccountRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default)
    {
        var student = await _students.GetByIdAsync(studentId, ct) ?? throw new KeyNotFoundException("Student not found.");
        if (student.Status != "Active")
            throw new InvalidOperationException("Fee accounts are for current students.");

        var start = (request.BillingStartDate ?? Today).Date;
        if (start > Today.AddYears(1))
            throw new InvalidOperationException("Billing can't start more than a year from now.");

        var transport = await _transport.GetForStudentAsync(studentId, ct);
        return await OpenAccountAsync(new OpenFeeAccountCommand(studentId, student.ClassId, start, transport?.RouteId,
            request.ChargeAdmissionFee, null, actorUserId, actorRole), ct);
    }

    public async Task<OpenAccountsResult> OpenForAllCurrentStudentsAsync(OpenFeeAccountRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default)
    {
        var students = new List<Guid>();
        for (var page = 1; ; page++)
        {
            var batch = await _students.SearchAsync(null, null, null, RosterFilter.Current, page, 200, ct);
            students.AddRange(batch.Items.Select(s => s.Id));
            if (batch.Items.Count < 200) break;
        }

        var withAccounts = await _accounts.StudentsWithAccountsAsync(students, ct);
        int opened = 0, fees = 0;
        foreach (var id in students.Where(id => !withAccounts.Contains(id)))
        {
            try
            {
                var result = await OpenForExistingStudentAsync(id, request, actorUserId, actorRole, ct);
                opened++;
                fees += result.Lines.Count;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Couldn't open a fee account for {StudentId}: {Reason}", id, ex.Message);
            }
        }
        return new OpenAccountsResult(opened, withAccounts.Count, fees);
    }

    public async Task CloseAsync(Guid studentId, string reason, CancellationToken ct = default)
    {
        var account = await _accounts.FindByStudentAsync(studentId, ct);
        if (account is null || !account.IsOpen) return;
        await CloseAccountAsync(account, reason, ct);
        await _accounts.SaveChangesAsync(ct);
    }

    public async Task ReopenAsync(Guid studentId, CancellationToken ct = default)
    {
        var account = await _accounts.FindByStudentAsync(studentId, ct);
        if (account is null || account.IsOpen) return;
        await ReopenAccountAsync(account, ct);
        await _accounts.SaveChangesAsync(ct);
        await GenerateDueFeesAsync(studentId, ct);
    }

    private async Task CloseAccountAsync(StudentFeeAccount account, string reason, CancellationToken ct)
    {
        // Fees for later this session (exam fees, untouched) are future fees: a student who has
        // left won't sit those exams, so they go. Anything already due, paid or discounted stays.
        var (count, total) = await _payments.DeleteUntouchedUpcomingAsync(account.StudentId, Today, ct);
        account.IsOpen = false;
        account.ClosedAtUtc = DateTime.UtcNow;
        account.ClosedReason = reason.Length > 200 ? reason[..200] : reason;
        account.UpdatedAtUtc = DateTime.UtcNow;
        await _accounts.AddAdjustmentAsync(new FeeAdjustment
        {
            StudentId = account.StudentId, Kind = FeeAdjustmentKinds.AccountClosed, Amount = total,
            Reason = count > 0 ? $"{reason}. Billing stopped; {count} upcoming fee(s) cancelled." : $"{reason}. Billing stopped."
        }, ct);
        _logger.LogInformation("Fee account closed for {StudentId}: {Reason}", account.StudentId, reason);
    }

    private async Task ReopenAccountAsync(StudentFeeAccount account, CancellationToken ct)
    {
        account.IsOpen = true;
        account.BillingStartDate = Today; // the months away are never charged
        account.ClosedAtUtc = null;
        account.ClosedReason = null;
        account.UpdatedAtUtc = DateTime.UtcNow;
        await _accounts.AddAdjustmentAsync(new FeeAdjustment
        {
            StudentId = account.StudentId, Kind = FeeAdjustmentKinds.AccountReopened,
            Reason = $"Student back on the rolls; billing resumes from {Today:dd MMM yyyy}."
        }, ct);
        _logger.LogInformation("Fee account reopened for {StudentId}", account.StudentId);
    }

    public async Task<int> RepriceUpcomingAsync(Guid studentId, string newClassId, CancellationToken ct = default)
    {
        var account = await _accounts.FindByStudentAsync(studentId, ct);
        if (account is null) return 0;

        var (rates, _) = await LoadStructureAsync(newClassId, ct);
        var today = Today;
        var repriced = 0;
        foreach (var fee in await _payments.FindByStudentAsync(studentId, ct))
        {
            if (fee.Category != FeeCategories.Exam || fee.DueDate is null || fee.DueDate <= today
                || fee.PaidAmount > 0 || fee.WaiverAmount > 0 || fee.PeriodKey is null)
                continue;
            var newAmount = rates.ExamFee(fee.PeriodKey);
            if (newAmount == fee.TotalAmount && fee.ClassId == newClassId) continue;

            if (await _payments.RepriceUntouchedAsync(fee.Id, newAmount, newClassId, ct) == 0) continue;
            await _accounts.AddAdjustmentAsync(new FeeAdjustment
            {
                StudentId = studentId, FeePaymentId = fee.Id, Kind = FeeAdjustmentKinds.Repriced, Amount = newAmount,
                Reason = $"Moved to {SchoolClasses.NameOf(newClassId)}: {fee.PeriodLabel} ₹{fee.TotalAmount:0.##} → ₹{newAmount:0.##}"
            }, ct);
            repriced++;
        }
        await _accounts.SaveChangesAsync(ct);
        return repriced;
    }

    public async Task<FeeGenerationResult> GenerateDueFeesAsync(Guid? studentId = null, CancellationToken ct = default)
    {
        IReadOnlyList<StudentFeeAccount> accounts = studentId is { } one
            ? (await _accounts.FindByStudentAsync(one, ct)) is { } a ? [a] : []
            : await _accounts.GetAllAsync(ct);
        if (accounts.Count == 0) return new FeeGenerationResult(0, 0, 0, 0);

        var ids = accounts.Select(a => a.StudentId).ToList();
        var students = (await _students.GetByIdsAsync(ids, ct)).ToDictionary(s => s.Id);
        var transport = (await _transport.GetForStudentsAsync(ids, ct)).ToDictionary(t => t.StudentId);
        var rates = (await _setup.GetRatesAsync(ct)).ToDictionary(r => r.ClassId, StringComparer.OrdinalIgnoreCase);
        var due = await LoadDueDatesAsync(ct);
        var existing = (await _payments.FindGeneratedKeysAsync(ids, ct))
            .Select(k => (k.StudentId, k.Category, k.AcademicYear ?? "", k.PeriodKey ?? ""))
            .ToHashSet();
        var today = Today;

        int created = 0, closed = 0, reopened = 0;
        foreach (var account in accounts)
        {
            try
            {
                var student = students.GetValueOrDefault(account.StudentId);
                if (student is null || student.Status != "Active")
                {
                    // The student has left (or the "left" event was missed): stop billing.
                    if (account.IsOpen)
                    {
                        await CloseAccountAsync(account, student is null ? "Student record not found" : $"Student is {student.Status}", ct);
                        await _accounts.SaveChangesAsync(ct);
                        closed++;
                    }
                    continue;
                }

                if (!account.IsOpen)
                {
                    // Back on the rolls (or the "came back" event was missed): resume from today.
                    await ReopenAccountAsync(account, ct);
                    reopened++;
                }

                if (!rates.TryGetValue(student.ClassId, out var rate))
                {
                    _logger.LogWarning("No fee structure for class {ClassId}; skipping {StudentId}", student.ClassId, student.Id);
                    await _accounts.SaveChangesAsync(ct);
                    continue;
                }

                var plan = transport.GetValueOrDefault(account.StudentId) is { } t
                    ? new TransportPlan(t.RouteName, t.MonthlyFee, SchoolClock.DateOf(t.AssignedAtUtc))
                    : null;

                var missing = FeeBillingPlanner.Plan(account.BillingStartDate, today, FeeRates.From(rate), due, plan, includeAdmissionFee: false)
                    .Where(f => !existing.Contains((account.StudentId, f.Category, f.AcademicYear, f.PeriodKey)))
                    .Select(f => ToEntity(f, account.StudentId, student.ClassId))
                    .ToList();

                if (missing.Count > 0) await _payments.AddRangeAsync(missing, ct);
                account.LastGeneratedAtUtc = DateTime.UtcNow;
                await _accounts.SaveChangesAsync(ct);
                created += missing.Count;
            }
            catch (DbUpdateException ex)
            {
                // Most likely another run created the same fee first (the unique index said no).
                _accounts.DiscardUnsavedInserts();
                _logger.LogWarning(ex, "Fee generation for {StudentId} didn't save; it will be retried on the next run", account.StudentId);
            }
        }

        if (created > 0 || closed > 0 || reopened > 0)
            _logger.LogInformation("Fee generation: {Accounts} account(s), {Created} fee(s) created, {Closed} closed, {Reopened} reopened",
                accounts.Count, created, closed, reopened);
        return new FeeGenerationResult(accounts.Count, created, closed, reopened);
    }

    private async Task<(FeeRates Rates, FeeDueDates Due)> LoadStructureAsync(string classId, CancellationToken ct)
    {
        if (!SchoolClasses.IsKnown(classId))
            throw new InvalidOperationException($"Unknown class '{classId}'.");
        var rate = await _setup.FindRateAsync(classId, ct);
        if (rate is null)
        {
            await _setupService.EnsureDefaultsAsync(ct);
            rate = await _setup.FindRateAsync(classId, ct)
                ?? throw new InvalidOperationException($"No fee structure is set for {SchoolClasses.NameOf(classId)}.");
        }
        return (FeeRates.From(rate), await LoadDueDatesAsync(ct));
    }

    private async Task<FeeDueDates> LoadDueDatesAsync(CancellationToken ct) =>
        await _setup.GetScheduleAsync(ct) is { } s ? FeeDueDates.From(s) : FeeDueDates.Default;

    private async Task<TransportChoice?> ResolveRouteAsync(Guid? routeId, bool requireActive, CancellationToken ct)
    {
        if (routeId is null) return null;
        var route = await _routes.GetByIdAsync(routeId.Value, ct) ?? throw new KeyNotFoundException("That transport route doesn't exist.");
        if (requireActive && !route.IsActive)
            throw new InvalidOperationException($"{route.Name} is switched off; choose an active route.");
        return new TransportChoice(route.Id, route.Name, FeeRates.Inr(route.MonthlyFee));
    }

    private static FeePayment ToEntity(PlannedFee f, Guid studentId, string classId) => new()
    {
        StudentId = studentId,
        ClassId = classId,
        Category = f.Category,
        AcademicYear = f.AcademicYear,
        PeriodKey = f.PeriodKey,
        PeriodLabel = f.PeriodLabel,
        Description = f.Description,
        DueDate = f.DueDate,
        TotalAmount = f.Amount
    };

    private static FeeBreakdown Breakdown(string classId, DateTime start, FeeRates rates, FeeDueDates due,
        TransportChoice? transport, IReadOnlyList<PlannedFee> planned, decimal? previousDues)
    {
        var session = FeeBillingPlanner.SessionStartYear(start);
        var exams = ExamPeriods.All.Select(p => new ExamFeeLine(p, ExamPeriods.Label(p), rates.ExamFee(p), due.ExamDueDate(p, session),
            planned.Any(f => f.Category == FeeCategories.Exam && f.PeriodKey == p))).ToList();

        var lines = planned
            .Select(f => new FeeLine(f.Category, f.PeriodLabel, f.Description, f.AcademicYear, f.PeriodKey, f.DueDate, f.Amount, f.DueDate <= start))
            .ToList();
        var dues = previousDues is > 0 ? previousDues.Value : 0;
        if (dues > 0)
            lines.Add(new FeeLine(FeeCategories.Other, "Previous dues", "Previous dues at admission", AcademicYears.Format(session), null, start, dues, true));

        var warnings = new List<string>();
        if (rates.AdmissionFee <= 0) warnings.Add($"No admission fee is set for {SchoolClasses.NameOf(classId)}, so none will be charged.");
        if (rates.MonthlyTuition <= 0) warnings.Add($"No monthly tuition fee is set for {SchoolClasses.NameOf(classId)}.");

        return new FeeBreakdown(
            AcademicYears.Format(session), classId, SchoolClasses.NameOf(classId), start,
            rates.AdmissionFee, rates.MonthlyTuition,
            exams, exams.Where(e => e.Charged).Sum(e => e.Amount),
            transport, dues, lines,
            lines.Where(l => l.DueNow).Sum(l => l.Amount),
            lines.Where(l => !l.DueNow).Sum(l => l.Amount),
            rates.MonthlyTuition + (transport?.MonthlyFee ?? 0),
            warnings.Count == 0 ? null : string.Join(" ", warnings));
    }
}
