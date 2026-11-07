using System.Text.Json;
using SchoolERP.Business.Transport.DTOs;
using SchoolERP.Business.Transport.Services.Interfaces;
using SchoolERP.Common.Audit;
using SchoolERP.DataAccess.Transport.Repositories.Interfaces;
using RouteEntity = SchoolERP.DataAccess.Transport.Entities.Route;

namespace SchoolERP.Business.Transport.Services;

public class RouteService : IRouteService
{
    private readonly IRouteRepository _routes;
    private readonly IStudentRouteMappingRepository _mappings;
    private readonly IAuditTrail _audit;

    /// <summary>The routes a new school starts with: Route 1 at ₹500 a month up to Route 10 at ₹1,500. Names and charges are editable.</summary>
    public static readonly IReadOnlyList<(string Name, decimal MonthlyFee)> DefaultRoutes =
    [
        ("Route 1", 500), ("Route 2", 600), ("Route 3", 700), ("Route 4", 800), ("Route 5", 900),
        ("Route 6", 1000), ("Route 7", 1100), ("Route 8", 1200), ("Route 9", 1300), ("Route 10", 1500),
    ];

    public RouteService(IRouteRepository routes, IStudentRouteMappingRepository mappings, IAuditTrail audit)
    {
        _routes = routes;
        _mappings = mappings;
        _audit = audit;
    }

    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        if (await _routes.AnyAsync(ct)) return;
        await _routes.AddRangeAsync(DefaultRoutes.Select(r => new RouteEntity { Name = r.Name, MonthlyFee = r.MonthlyFee }), ct);
        await _routes.SaveChangesAsync(ct);
    }

    public async Task<RouteSummary> CreateAsync(CreateRouteRequest request, CancellationToken ct = default)
    {
        var route = new RouteEntity
        {
            Name = request.Name.Trim(),
            StartPoint = Clean(request.StartPoint),
            EndPoint = Clean(request.EndPoint),
            MonthlyFee = request.MonthlyFee
        };

        await _routes.AddAsync(route, ct);
        await _routes.SaveChangesAsync(ct);

        return ToSummary(route, 0);
    }

    public async Task<RouteSummary> UpdateAsync(Guid id, UpdateRouteRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default)
    {
        var route = await _routes.FindByIdAsync(id, ct) ?? throw new KeyNotFoundException("Route not found.");
        var before = new { route.Name, route.StartPoint, route.EndPoint, route.MonthlyFee, route.IsActive };

        route.Name = request.Name.Trim();
        route.StartPoint = Clean(request.StartPoint);
        route.EndPoint = Clean(request.EndPoint);
        route.MonthlyFee = request.MonthlyFee;
        route.IsActive = request.IsActive;
        route.UpdatedAtUtc = DateTime.UtcNow;
        await _routes.SaveChangesAsync(ct);

        var after = new { route.Name, route.StartPoint, route.EndPoint, route.MonthlyFee, route.IsActive };
        if (!before.Equals(after))
            _audit.Record(new AuditRecord(DateTime.UtcNow, "transport.route.updated", actorUserId, actorRole, route.Id.ToString(),
                Detail: JsonSerializer.Serialize(new { Before = before, After = after })));

        var riders = (await _mappings.FindByRouteIdAsync(route.Id, ct)).Count;
        return ToSummary(route, riders);
    }

    public async Task<IReadOnlyList<RouteSummary>> GetAllAsync(CancellationToken ct = default)
    {
        var routes = await _routes.GetAllAsync(ct);
        var counts = new Dictionary<Guid, int>();
        foreach (var route in routes)
            counts[route.Id] = (await _mappings.FindByRouteIdAsync(route.Id, ct)).Count;
        return routes.Select(r => ToSummary(r, counts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<RouteSummary?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var route = await _routes.FindByIdAsync(id, ct);
        return route is null ? null : ToSummary(route, (await _mappings.FindByRouteIdAsync(id, ct)).Count);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RouteSummary ToSummary(RouteEntity r, int riders) =>
        new(r.Id, r.Name, r.StartPoint, r.EndPoint, r.MonthlyFee, r.IsActive, riders);
}
