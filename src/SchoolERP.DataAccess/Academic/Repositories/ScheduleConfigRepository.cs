using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Academic;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Academic.Repositories;

public class ScheduleConfigRepository : IScheduleConfigRepository
{
    private readonly AcademicDbContext _db;

    public ScheduleConfigRepository(AcademicDbContext db) => _db = db;

    // One school -> one config; the oldest row is the canonical one if several ever exist.
    public Task<ScheduleConfig?> GetCurrentAsync(CancellationToken ct = default) =>
        _db.ScheduleConfigs.OrderBy(c => c.CreatedAtUtc).FirstOrDefaultAsync(ct);

    public async Task AddAsync(ScheduleConfig config, CancellationToken ct = default) =>
        await _db.ScheduleConfigs.AddAsync(config, ct);

    public Task<int> UpdateJsonAsync(Guid id, string configJson, CancellationToken ct = default) =>
        _db.ScheduleConfigs.Where(c => c.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.ConfigJson, configJson)
                .SetProperty(c => c.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
