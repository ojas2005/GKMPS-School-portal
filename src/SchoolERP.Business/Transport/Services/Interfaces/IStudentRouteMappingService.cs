using SchoolERP.Business.Transport.DTOs;

namespace SchoolERP.Business.Transport.Services.Interfaces;

public interface IStudentRouteMappingService
{
    /// <summary>Puts a student on a route, or moves them to another. Only active routes can be chosen.</summary>
    Task<StudentRouteMappingSummary> AssignAsync(AssignStudentRouteRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<StudentRouteMappingWithNameSummary>> GetByRouteIdAsync(Guid routeId, CancellationToken ct = default);
    Task<StudentTransportSummary?> GetForStudentAsync(Guid studentId, CancellationToken ct = default);
    /// <summary>Transport of each of these students who use it.</summary>
    Task<IReadOnlyList<StudentTransportSummary>> GetForStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);
    /// <summary>Takes the student off transport; no more transport fees are created for them.</summary>
    Task<bool> RemoveForStudentAsync(Guid studentId, CancellationToken ct = default);
}
