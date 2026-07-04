using SchoolERP.Transport.DTOs;
using SchoolERP.Transport.Repositories.Interfaces;
using SchoolERP.Transport.Services.Interfaces;
using RouteEntity = SchoolERP.Transport.Entities.Route;

namespace SchoolERP.Transport.Services;

public class RouteService : IRouteService
{
    private readonly IRouteRepository _routes;

    public RouteService(IRouteRepository routes) => _routes = routes;

    public async Task<RouteSummary> CreateAsync(CreateRouteRequest request, CancellationToken ct = default)
    {
        var route = new RouteEntity
        {
            Name = request.Name,
            StartPoint = request.StartPoint,
            EndPoint = request.EndPoint,
            MonthlyFee = request.MonthlyFee
        };

        await _routes.AddAsync(route, ct);
        await _routes.SaveChangesAsync(ct);

        return ToSummary(route);
    }

    public async Task<IReadOnlyList<RouteSummary>> GetAllAsync(CancellationToken ct = default)
    {
        var routes = await _routes.GetAllAsync(ct);
        return routes.Select(ToSummary).ToList();
    }

    private static RouteSummary ToSummary(RouteEntity r) => new(r.Id, r.Name, r.StartPoint, r.EndPoint, r.MonthlyFee);
}
