using SchoolERP.DataAccess.Academic.Entities;

namespace SchoolERP.DataAccess.Academic.Repositories.Interfaces;

public interface IScheduleConfigRepository
{
    /// <summary>The single current config for the school (null if none saved yet).</summary>
    Task<ScheduleConfig?> GetCurrentAsync(CancellationToken ct = default);

    Task AddAsync(ScheduleConfig config, CancellationToken ct = default);

    /// <summary>Atomic replace of the JSON blob -- no load-then-save.</summary>
    Task<int> UpdateJsonAsync(Guid id, string configJson, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
