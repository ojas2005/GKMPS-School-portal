using Microsoft.EntityFrameworkCore;
using SchoolERP.Transport.Data;
using SchoolERP.Transport.Entities;
using SchoolERP.Transport.Repositories.Interfaces;

namespace SchoolERP.Transport.Repositories;

public class StudentRouteMappingRepository : IStudentRouteMappingRepository
{
    private readonly TransportDbContext _db;

    public StudentRouteMappingRepository(TransportDbContext db) => _db = db;

    public Task<StudentRouteMapping?> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default) =>
        _db.StudentRouteMappings.FirstOrDefaultAsync(m => m.StudentId == studentId, ct);

    public Task<IReadOnlyList<StudentRouteMapping>> FindByRouteIdAsync(Guid routeId, CancellationToken ct = default) =>
        _db.StudentRouteMappings.Where(m => m.RouteId == routeId).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StudentRouteMapping>)t.Result, ct);

    public Task<bool> ExistsForStudentAsync(Guid studentId, CancellationToken ct = default) =>
        _db.StudentRouteMappings.AnyAsync(m => m.StudentId == studentId, ct);

    public async Task AddAsync(StudentRouteMapping mapping, CancellationToken ct = default) =>
        await _db.StudentRouteMappings.AddAsync(mapping, ct);

    public Task<int> ReassignAsync(Guid mappingId, Guid routeId, string pickupPoint, CancellationToken ct = default) =>
        _db.StudentRouteMappings.Where(m => m.Id == mappingId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.RouteId, routeId)
                .SetProperty(m => m.PickupPoint, pickupPoint)
                .SetProperty(m => m.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
