using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Transport.DTOs;
using SchoolERP.Business.Transport.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Transport;

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

    // Rename, re-price or switch a route on/off. A new charge applies to transport fees created from now on.
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRouteRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _routeService.UpdateAsync(id, request, User.UserId(), User.Role(), ct);
            return Ok(ApiResponse<RouteSummary>.Ok(result, "Route saved."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _routeService.GetAllAsync(ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _routeService.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Route not found."));

        return Ok(ApiResponse<RouteSummary>.Ok(result));
    }
}
