using System.Text.Json;
using SchoolERP.Business.Academic.DTOs;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;
using SchoolERP.Business.Academic.Services.Interfaces;

namespace SchoolERP.Business.Academic.Services;

/// <summary>
/// Loads/saves the owner's timetable-generation setup as a single JSON blob (see
/// <see cref="ScheduleConfig"/>). The generator reads this back to build timetables.
/// </summary>
public class ScheduleConfigService : IScheduleConfigService
{
    private readonly IScheduleConfigRepository _configs;

    public ScheduleConfigService(IScheduleConfigRepository configs) => _configs = configs;

    public async Task<ScheduleConfigResponse> GetAsync(CancellationToken ct = default)
    {
        var config = await _configs.GetCurrentAsync(ct);
        if (config is null)
            return new ScheduleConfigResponse(new(), new(), new());

        var saved = JsonSerializer.Deserialize<SaveScheduleConfigRequest>(config.ConfigJson);
        return new ScheduleConfigResponse(
            saved?.Junior ?? new(),
            saved?.Senior ?? new(),
            saved?.PrePrimary ?? new());
    }

    public async Task SaveAsync(SaveScheduleConfigRequest request, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(request);
        var existing = await _configs.GetCurrentAsync(ct);
        if (existing is null)
        {
            await _configs.AddAsync(new ScheduleConfig { ConfigJson = json }, ct);
            await _configs.SaveChangesAsync(ct);
        }
        else
        {
            await _configs.UpdateJsonAsync(existing.Id, json, ct);
        }
    }
}
