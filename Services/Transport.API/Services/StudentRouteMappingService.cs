using SchoolERP.Transport.Clients;
using SchoolERP.Transport.DTOs;
using SchoolERP.Transport.Entities;
using SchoolERP.Transport.Repositories.Interfaces;
using SchoolERP.Transport.Services.Interfaces;

namespace SchoolERP.Transport.Services;

public class StudentRouteMappingService : IStudentRouteMappingService
{
    private readonly IStudentRouteMappingRepository _mappings;
    private readonly IStudentServiceClient _studentServiceClient;

    public StudentRouteMappingService(IStudentRouteMappingRepository mappings, IStudentServiceClient studentServiceClient)
    {
        _mappings = mappings;
        _studentServiceClient = studentServiceClient;
    }

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

    public async Task<IReadOnlyList<StudentRouteMappingWithNameSummary>> GetByRouteIdAsync(Guid routeId, CancellationToken ct = default)
    {
        var mappings = await _mappings.FindByRouteIdAsync(routeId, ct);

        // A route's roster is small (tens of students, single-school scale), so per-student
        // lookups run in parallel rather than adding a bulk endpoint to Student.API just for this.
        var lookups = await Task.WhenAll(mappings.Select(m => _studentServiceClient.GetByIdAsync(m.StudentId, ct)));

        return mappings
            .Zip(lookups, (mapping, student) => new StudentRouteMappingWithNameSummary(
                mapping.Id, mapping.StudentId, student?.FullName ?? "Unknown student", student?.AdmissionNumber,
                mapping.RouteId, mapping.PickupPoint))
            .ToList();
    }
}
