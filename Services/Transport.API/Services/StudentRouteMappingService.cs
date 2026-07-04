using SchoolERP.Transport.DTOs;
using SchoolERP.Transport.Entities;
using SchoolERP.Transport.Repositories.Interfaces;
using SchoolERP.Transport.Services.Interfaces;

namespace SchoolERP.Transport.Services;

public class StudentRouteMappingService : IStudentRouteMappingService
{
    private readonly IStudentRouteMappingRepository _mappings;

    public StudentRouteMappingService(IStudentRouteMappingRepository mappings) => _mappings = mappings;

    public async Task<StudentRouteMappingSummary> AssignAsync(AssignStudentRouteRequest request, CancellationToken ct = default)
    {
        var existing = await _mappings.FindByStudentIdAsync(request.StudentId, ct);
        if (existing is not null)
        {
            await _mappings.ReassignAsync(existing.Id, request.RouteId, request.PickupPoint, ct);
            return new StudentRouteMappingSummary(existing.Id, request.StudentId, request.RouteId, request.PickupPoint);
        }

        var mapping = new StudentRouteMapping
        {
            StudentId = request.StudentId,
            RouteId = request.RouteId,
            PickupPoint = request.PickupPoint
        };

        await _mappings.AddAsync(mapping, ct);
        await _mappings.SaveChangesAsync(ct);

        return new StudentRouteMappingSummary(mapping.Id, mapping.StudentId, mapping.RouteId, mapping.PickupPoint);
    }
}
