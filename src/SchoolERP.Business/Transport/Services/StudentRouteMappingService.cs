using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Transport.DTOs;
using SchoolERP.DataAccess.Transport.Entities;
using SchoolERP.DataAccess.Transport.Repositories.Interfaces;
using SchoolERP.Business.Transport.Services.Interfaces;

namespace SchoolERP.Business.Transport.Services;

public class StudentRouteMappingService : IStudentRouteMappingService
{
    private readonly IStudentRouteMappingRepository _mappings;
    private readonly IStudentService _students;

    public StudentRouteMappingService(IStudentRouteMappingRepository mappings, IStudentService students)
    {
        _mappings = mappings;
        _students = students;
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

        // A route's roster is small (tens of students), so look students up one by one --
        // sequentially, because they share this request's database context.
        var lookups = new List<SchoolERP.Business.Student.DTOs.StudentSummary?>();
        foreach (var mapping in mappings)
            lookups.Add(await _students.GetByIdAsync(mapping.StudentId, ct));

        return mappings
            .Zip(lookups, (mapping, student) => new StudentRouteMappingWithNameSummary(
                mapping.Id, mapping.StudentId, student?.FullName ?? "Unknown student", student?.AdmissionNumber,
                mapping.RouteId, mapping.PickupPoint))
            .ToList();
    }
}
