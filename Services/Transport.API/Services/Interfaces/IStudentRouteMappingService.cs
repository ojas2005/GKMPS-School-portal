using SchoolERP.Transport.DTOs;

namespace SchoolERP.Transport.Services.Interfaces;

public interface IStudentRouteMappingService
{
    Task<StudentRouteMappingSummary> AssignAsync(AssignStudentRouteRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<StudentRouteMappingWithNameSummary>> GetByRouteIdAsync(Guid routeId, CancellationToken ct = default);
}
