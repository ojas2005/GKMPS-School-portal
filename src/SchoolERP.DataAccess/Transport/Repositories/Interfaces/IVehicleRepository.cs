using SchoolERP.DataAccess.Transport.Entities;

namespace SchoolERP.DataAccess.Transport.Repositories.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> FindByRouteAsync(Guid routeId, CancellationToken ct = default);
    Task<bool> ExistsByRegistrationAsync(string registrationNumber, CancellationToken ct = default);
    Task AddAsync(Vehicle vehicle, CancellationToken ct = default);

    /// <summary>Atomic route reassignment for a vehicle.</summary>
    Task<int> AssignRouteAsync(Guid vehicleId, Guid? routeId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
