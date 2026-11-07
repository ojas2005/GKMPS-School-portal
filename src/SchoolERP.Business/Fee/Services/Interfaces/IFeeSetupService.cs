using SchoolERP.Business.Fee.DTOs;

namespace SchoolERP.Business.Fee.Services.Interfaces;

/// <summary>The configurable fee structure: class-wise admission, tuition and exam fees, and when fees fall due.</summary>
public interface IFeeSetupService
{
    /// <summary>Every class's fee structure, PG first.</summary>
    Task<IReadOnlyList<ClassFeeRateDto>> GetRatesAsync(CancellationToken ct = default);

    /// <summary>Saves new amounts. Fees already on students' accounts don't change; only fees created afterwards use them.</summary>
    Task<IReadOnlyList<ClassFeeRateDto>> UpdateRatesAsync(UpdateClassFeeRatesRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default);

    Task<FeeScheduleDto> GetScheduleAsync(CancellationToken ct = default);
    Task<FeeScheduleDto> UpdateScheduleAsync(FeeScheduleDto request, Guid? actorUserId, string? actorRole, CancellationToken ct = default);

    /// <summary>Creates the default rates for any class without one, and the default schedule.</summary>
    Task EnsureDefaultsAsync(CancellationToken ct = default);
}
