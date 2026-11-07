using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.DataAccess.Fee.Repositories.Interfaces;

/// <summary>The configurable fee structure: class-wise rates and the due-date schedule.</summary>
public interface IFeeSetupRepository
{
    Task<IReadOnlyList<ClassFeeRate>> GetRatesAsync(CancellationToken ct = default);
    Task<ClassFeeRate?> FindRateAsync(string classId, CancellationToken ct = default);
    Task AddRateAsync(ClassFeeRate rate, CancellationToken ct = default);

    /// <summary>The schedule row, or null before it has been created.</summary>
    Task<FeeSchedule?> GetScheduleAsync(CancellationToken ct = default);
    Task AddScheduleAsync(FeeSchedule schedule, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
