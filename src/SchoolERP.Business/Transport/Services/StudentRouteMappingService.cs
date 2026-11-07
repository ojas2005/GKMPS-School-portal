using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Transport.DTOs;
using SchoolERP.DataAccess.Transport.Entities;
using SchoolERP.DataAccess.Transport.Repositories.Interfaces;
using SchoolERP.Business.Transport.Services.Interfaces;

namespace SchoolERP.Business.Transport.Services;

/// <summary>
/// Which students use transport and on which route. The Fee module reads this when it creates
/// each month's transport fee, so moving a student changes the charge from the next fee on
/// and taking them off transport stops it; fees already created are left as they were.
/// </summary>
public class StudentRouteMappingService : IStudentRouteMappingService
{
    private const string PickupToBeConfirmed = "To be confirmed";

    private readonly IStudentRouteMappingRepository _mappings;
    private readonly IRouteRepository _routes;
    private readonly IStudentService _students;

    public StudentRouteMappingService(IStudentRouteMappingRepository mappings, IRouteRepository routes, IStudentService students)
    {
        _mappings = mappings;
        _routes = routes;
        _students = students;
    }

    public async Task<StudentRouteMappingSummary> AssignAsync(AssignStudentRouteRequest request, CancellationToken ct = default)
    {
        var route = await _routes.FindByIdAsync(request.RouteId, ct) ?? throw new KeyNotFoundException("Route not found.");
        var existing = await _mappings.FindByStudentIdAsync(request.StudentId, ct);
        if (!route.IsActive && existing?.RouteId != route.Id)
            throw new InvalidOperationException($"{route.Name} is switched off; choose an active route.");

        var pickup = string.IsNullOrWhiteSpace(request.PickupPoint) ? existing?.PickupPoint ?? PickupToBeConfirmed : request.PickupPoint.Trim();
        if (existing is not null)
        {
            await _mappings.ReassignAsync(existing.Id, request.RouteId, pickup, ct);
            return new StudentRouteMappingSummary(existing.Id, request.StudentId, request.RouteId, pickup, existing.AssignedAtUtc);
        }

        var mapping = new StudentRouteMapping
        {
            StudentId = request.StudentId,
            RouteId = request.RouteId,
            PickupPoint = pickup
        };

        await _mappings.AddAsync(mapping, ct);
        await _mappings.SaveChangesAsync(ct);

        return new StudentRouteMappingSummary(mapping.Id, mapping.StudentId, mapping.RouteId, mapping.PickupPoint, mapping.AssignedAtUtc);
    }

    public async Task<StudentTransportSummary?> GetForStudentAsync(Guid studentId, CancellationToken ct = default) =>
        (await GetForStudentsAsync([studentId], ct)).FirstOrDefault();

    public async Task<IReadOnlyList<StudentTransportSummary>> GetForStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default)
    {
        var mappings = await _mappings.FindByStudentIdsAsync(studentIds, ct);
        if (mappings.Count == 0) return [];
        var routes = (await _routes.GetAllAsync(ct)).ToDictionary(r => r.Id);
        return mappings
            .Where(m => routes.ContainsKey(m.RouteId))
            .Select(m =>
            {
                var r = routes[m.RouteId];
                return new StudentTransportSummary(m.StudentId, r.Id, r.Name, r.MonthlyFee, r.IsActive, m.PickupPoint, m.AssignedAtUtc);
            })
            .ToList();
    }

    public async Task<bool> RemoveForStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await _mappings.DeleteForStudentAsync(studentId, ct) > 0;

    public async Task<IReadOnlyList<StudentRouteMappingWithNameSummary>> GetByRouteIdAsync(Guid routeId, CancellationToken ct = default)
    {
        var mappings = await _mappings.FindByRouteIdAsync(routeId, ct);
        var students = (await _students.GetByIdsAsync(mappings.Select(m => m.StudentId).ToList(), ct)).ToDictionary(s => s.Id);

        return mappings
            .Select(mapping =>
            {
                var student = students.GetValueOrDefault(mapping.StudentId);
                return new StudentRouteMappingWithNameSummary(
                    mapping.Id, mapping.StudentId, student?.FullName ?? "Unknown student", student?.AdmissionNumber,
                    mapping.RouteId, mapping.PickupPoint);
            })
            .ToList();
    }
}
