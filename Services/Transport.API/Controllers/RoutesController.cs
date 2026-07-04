using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Transport.DTOs;
using SchoolERP.Transport.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Transport.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly IRouteService _routeService;

    public RoutesController(IRouteService routeService) => _routeService = routeService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var result = await _routeService.CreateAsync(request, ct);
        return Ok(ApiResponse<RouteSummary>.Ok(result, "Route created."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _routeService.GetAllAsync(ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
