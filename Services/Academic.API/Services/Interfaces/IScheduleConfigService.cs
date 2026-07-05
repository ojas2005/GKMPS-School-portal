using SchoolERP.Academic.DTOs;

namespace SchoolERP.Academic.Services.Interfaces;

public interface IScheduleConfigService
{
    /// <summary>Current owner config (empty groups if nothing saved yet).</summary>
    Task<ScheduleConfigResponse> GetAsync(CancellationToken ct = default);

    Task SaveAsync(SaveScheduleConfigRequest request, CancellationToken ct = default);
}
