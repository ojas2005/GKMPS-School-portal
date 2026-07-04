using SchoolERP.Transport.Entities;

namespace SchoolERP.Transport.Repositories.Interfaces;

public interface IStudentRouteMappingRepository
{
    Task<StudentRouteMapping?> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentRouteMapping>> FindByRouteIdAsync(Guid routeId, CancellationToken ct = default);
    Task<bool> ExistsForStudentAsync(Guid studentId, CancellationToken ct = default);
    Task AddAsync(StudentRouteMapping mapping, CancellationToken ct = default);

    /// <summary>Atomic re-assignment to a different route/pickup point.</summary>
    Task<int> ReassignAsync(Guid mappingId, Guid routeId, string pickupPoint, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
