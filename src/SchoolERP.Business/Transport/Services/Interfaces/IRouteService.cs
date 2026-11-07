using SchoolERP.Business.Transport.DTOs;

namespace SchoolERP.Business.Transport.Services.Interfaces;

public interface IRouteService
{
    Task<RouteSummary> CreateAsync(CreateRouteRequest request, CancellationToken ct = default);
    /// <summary>Renames, re-prices, or switches a route on/off. A new charge applies to transport fees created from now on.</summary>
    Task<RouteSummary> UpdateAsync(Guid id, UpdateRouteRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default);
    /// <summary>Every route, active ones first, with how many students use each.</summary>
    Task<IReadOnlyList<RouteSummary>> GetAllAsync(CancellationToken ct = default);
    Task<RouteSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Creates the ten starting routes when there are no routes at all.</summary>
    Task EnsureDefaultsAsync(CancellationToken ct = default);
}
