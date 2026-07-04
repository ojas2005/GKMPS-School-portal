using Microsoft.EntityFrameworkCore;
using SchoolERP.Transport.Data;
using SchoolERP.Transport.Repositories.Interfaces;
using RouteEntity = SchoolERP.Transport.Entities.Route;

namespace SchoolERP.Transport.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly TransportDbContext _db;

    public RouteRepository(TransportDbContext db) => _db = db;

    public Task<RouteEntity?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Routes.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<IReadOnlyList<RouteEntity>> GetAllAsync(CancellationToken ct = default) =>
        _db.Routes.OrderBy(r => r.Name).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<RouteEntity>)t.Result, ct);

    public async Task AddAsync(RouteEntity route, CancellationToken ct = default) =>
        await _db.Routes.AddAsync(route, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
