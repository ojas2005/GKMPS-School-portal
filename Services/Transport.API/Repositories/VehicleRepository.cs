using Microsoft.EntityFrameworkCore;
using SchoolERP.Transport.Data;
using SchoolERP.Transport.Entities;
using SchoolERP.Transport.Repositories.Interfaces;

namespace SchoolERP.Transport.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly TransportDbContext _db;

    public VehicleRepository(TransportDbContext db) => _db = db;

    public Task<Vehicle?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<IReadOnlyList<Vehicle>> FindByRouteAsync(Guid routeId, CancellationToken ct = default) =>
        _db.Vehicles.Where(v => v.RouteId == routeId).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Vehicle>)t.Result, ct);

    public Task<bool> ExistsByRegistrationAsync(string registrationNumber, CancellationToken ct = default) =>
        _db.Vehicles.AnyAsync(v => v.RegistrationNumber == registrationNumber, ct);

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct = default) =>
        await _db.Vehicles.AddAsync(vehicle, ct);

    public Task<int> AssignRouteAsync(Guid vehicleId, Guid? routeId, CancellationToken ct = default) =>
        _db.Vehicles.Where(v => v.Id == vehicleId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(v => v.RouteId, routeId)
                .SetProperty(v => v.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
