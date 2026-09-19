using SchoolERP.Business.Transport.DTOs;

namespace SchoolERP.Business.Transport.Services.Interfaces;

public interface IRouteService
{
    Task<RouteSummary> CreateAsync(CreateRouteRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RouteSummary>> GetAllAsync(CancellationToken ct = default);
    Task<RouteSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
