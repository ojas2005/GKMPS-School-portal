using System.Text.Json;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Common;
using SchoolERP.Common.Audit;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;

namespace SchoolERP.Business.Fee.Services;

/// <summary>
/// The configurable fee structure. Every class always has a row (created with the school's
/// default amounts the first time), every change is audited with before and after, and
/// nothing here touches fees already on students' accounts.
/// </summary>
public class FeeSetupService : IFeeSetupService
{
    private readonly IFeeSetupRepository _setup;
    private readonly IAuditTrail _audit;

    public FeeSetupService(IFeeSetupRepository setup, IAuditTrail audit)
    {
        _setup = setup;
        _audit = audit;
    }

    /// <summary>
    /// The starting amounts: tuition ₹400 (PG to Class 5), ₹500 (6 to 8), ₹600 (9 and 10);
    /// each exam ₹400 (PG to Class 5), ₹700 (6 to 8), ₹300 (9 and 10). The admission fee
    /// starts at ₹0, so no admission fee is charged until the school sets one.
    /// </summary>
    public static ClassFeeRate DefaultRate(SchoolClass c)
    {
        var (tuition, exam) = c.Order switch
        {
            <= 9 => (400m, 400m),
            <= 12 => (500m, 700m),
            _ => (600m, 300m)
        };
        return new ClassFeeRate
        {
            ClassId = c.Id, AdmissionFee = 0, MonthlyTuition = tuition,
            QuarterlyExamFee = exam, HalfYearlyExamFee = exam, AnnualExamFee = exam
        };
    }

    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        var existing = (await _setup.GetRatesAsync(ct)).Select(r => r.ClassId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var c in SchoolClasses.All.Where(c => !existing.Contains(c.Id)))
            await _setup.AddRateAsync(DefaultRate(c), ct);
        if (await _setup.GetScheduleAsync(ct) is null)
            await _setup.AddScheduleAsync(new FeeSchedule(), ct);
        await _setup.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ClassFeeRateDto>> GetRatesAsync(CancellationToken ct = default)
    {
        var rates = await _setup.GetRatesAsync(ct);
        if (SchoolClasses.All.Any(c => !rates.Any(r => string.Equals(r.ClassId, c.Id, StringComparison.OrdinalIgnoreCase))))
        {
            await EnsureDefaultsAsync(ct);
            rates = await _setup.GetRatesAsync(ct);
        }
        return rates.Select(ToDto).OrderBy(r => r.Order).ThenBy(r => r.ClassName).ToList();
    }

    public async Task<IReadOnlyList<ClassFeeRateDto>> UpdateRatesAsync(UpdateClassFeeRatesRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default)
    {
        foreach (var input in request.Rates)
        {
            if (!SchoolClasses.IsKnown(input.ClassId))
                throw new InvalidOperationException($"Unknown class '{input.ClassId}'.");
        }

        var changes = new List<object>();
        foreach (var input in request.Rates)
        {
            var rate = await _setup.FindRateAsync(input.ClassId, ct);
            if (rate is null)
            {
                rate = new ClassFeeRate { ClassId = SchoolClasses.Find(input.ClassId)!.Id };
                await _setup.AddRateAsync(rate, ct);
            }

            var before = new { rate.AdmissionFee, rate.MonthlyTuition, rate.QuarterlyExamFee, rate.HalfYearlyExamFee, rate.AnnualExamFee };
            var after = new { input.AdmissionFee, input.MonthlyTuition, input.QuarterlyExamFee, input.HalfYearlyExamFee, input.AnnualExamFee };
            if (before.Equals(after)) // same property names and types: anonymous types compare by value
                continue;

            rate.AdmissionFee = input.AdmissionFee;
            rate.MonthlyTuition = input.MonthlyTuition;
            rate.QuarterlyExamFee = input.QuarterlyExamFee;
            rate.HalfYearlyExamFee = input.HalfYearlyExamFee;
            rate.AnnualExamFee = input.AnnualExamFee;
            rate.UpdatedAtUtc = DateTime.UtcNow;
            changes.Add(new { Class = SchoolClasses.NameOf(rate.ClassId), Before = before, After = after });
        }

        await _setup.SaveChangesAsync(ct);

        if (changes.Count > 0)
            _audit.Record(new AuditRecord(DateTime.UtcNow, "fee.structure.updated", actorUserId, actorRole,
                Detail: Truncate(JsonSerializer.Serialize(changes))));

        return await GetRatesAsync(ct);
    }

    public async Task<FeeScheduleDto> GetScheduleAsync(CancellationToken ct = default)
    {
        var schedule = await _setup.GetScheduleAsync(ct);
        if (schedule is null)
        {
            await EnsureDefaultsAsync(ct);
            schedule = (await _setup.GetScheduleAsync(ct))!;
        }
        return ToDto(schedule);
    }

    public async Task<FeeScheduleDto> UpdateScheduleAsync(FeeScheduleDto request, Guid? actorUserId, string? actorRole, CancellationToken ct = default)
    {
        foreach (var (month, day, name) in new[]
        {
            (request.QuarterlyExamDueMonth, request.QuarterlyExamDueDay, "quarterly exam"),
            (request.HalfYearlyExamDueMonth, request.HalfYearlyExamDueDay, "half-yearly exam"),
            (request.AnnualExamDueMonth, request.AnnualExamDueDay, "annual exam"),
        })
        {
            if (day > DateTime.DaysInMonth(2028, month)) // 2028: a leap year, so 29 Feb is allowed
                throw new InvalidOperationException($"The {name} due date doesn't exist ({day}/{month}).");
        }

        var schedule = await _setup.GetScheduleAsync(ct);
        if (schedule is null)
        {
            schedule = new FeeSchedule();
            await _setup.AddScheduleAsync(schedule, ct);
        }
        var before = ToDto(schedule);

        schedule.MonthlyDueDay = request.MonthlyDueDay;
        schedule.QuarterlyExamDueMonth = request.QuarterlyExamDueMonth;
        schedule.QuarterlyExamDueDay = request.QuarterlyExamDueDay;
        schedule.HalfYearlyExamDueMonth = request.HalfYearlyExamDueMonth;
        schedule.HalfYearlyExamDueDay = request.HalfYearlyExamDueDay;
        schedule.AnnualExamDueMonth = request.AnnualExamDueMonth;
        schedule.AnnualExamDueDay = request.AnnualExamDueDay;
        schedule.UpdatedAtUtc = DateTime.UtcNow;
        await _setup.SaveChangesAsync(ct);

        if (before != request)
            _audit.Record(new AuditRecord(DateTime.UtcNow, "fee.schedule.updated", actorUserId, actorRole,
                Detail: Truncate(JsonSerializer.Serialize(new { Before = before, After = request }))));

        return ToDto(schedule);
    }

    private static string Truncate(string s) => s.Length <= 4000 ? s : s[..4000];

    private static ClassFeeRateDto ToDto(ClassFeeRate r)
    {
        var c = SchoolClasses.Find(r.ClassId);
        return new ClassFeeRateDto(r.ClassId, c?.Name ?? r.ClassId, c?.Order ?? 99,
            r.AdmissionFee, r.MonthlyTuition, r.QuarterlyExamFee, r.HalfYearlyExamFee, r.AnnualExamFee, r.UpdatedAtUtc);
    }

    private static FeeScheduleDto ToDto(FeeSchedule s) => new(s.MonthlyDueDay,
        s.QuarterlyExamDueMonth, s.QuarterlyExamDueDay, s.HalfYearlyExamDueMonth, s.HalfYearlyExamDueDay,
        s.AnnualExamDueMonth, s.AnnualExamDueDay);
}
