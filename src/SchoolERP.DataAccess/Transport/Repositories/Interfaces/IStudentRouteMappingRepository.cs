using SchoolERP.DataAccess.Transport.Entities;

namespace SchoolERP.DataAccess.Transport.Repositories.Interfaces;

public interface IStudentRouteMappingRepository
{
    Task<StudentRouteMapping?> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentRouteMapping>> FindByRouteIdAsync(Guid routeId, CancellationToken ct = default);
    Task<bool> ExistsForStudentAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>The route assignments of these students (students without transport are absent).</summary>
    Task<IReadOnlyList<StudentRouteMapping>> FindByStudentIdsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);

    /// <summary>Takes the student off transport.</summary>
    Task<int> DeleteForStudentAsync(Guid studentId, CancellationToken ct = default);
    Task AddAsync(StudentRouteMapping mapping, CancellationToken ct = default);

    /// <summary>Atomic re-assignment to a different route/pickup point.</summary>
    Task<int> ReassignAsync(Guid mappingId, Guid routeId, string pickupPoint, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
