using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Transport;
using SchoolERP.DataAccess.Transport.Repositories.Interfaces;
using RouteEntity = SchoolERP.DataAccess.Transport.Entities.Route;

namespace SchoolERP.DataAccess.Transport.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly TransportDbContext _db;

    public RouteRepository(TransportDbContext db) => _db = db;

    public Task<RouteEntity?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Routes.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<IReadOnlyList<RouteEntity>> GetAllAsync(CancellationToken ct = default) =>
        _db.Routes.OrderByDescending(r => r.IsActive).ThenBy(r => r.Name.Length).ThenBy(r => r.Name).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<RouteEntity>)t.Result, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.Routes.AnyAsync(ct);

    public async Task AddAsync(RouteEntity route, CancellationToken ct = default) =>
        await _db.Routes.AddAsync(route, ct);

    public async Task AddRangeAsync(IEnumerable<RouteEntity> routes, CancellationToken ct = default) =>
        await _db.Routes.AddRangeAsync(routes, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
