using RouteEntity = SchoolERP.Transport.Entities.Route;

namespace SchoolERP.Transport.Repositories.Interfaces;

public interface IRouteRepository
{
    Task<RouteEntity?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RouteEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(RouteEntity route, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
