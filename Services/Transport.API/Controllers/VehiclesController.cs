using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Transport.DTOs;
using SchoolERP.Transport.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Transport.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService) => _vehicleService = vehicleService;

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddVehicleRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _vehicleService.AddAsync(request, ct);
            return Ok(ApiResponse<VehicleSummary>.Ok(result, "Vehicle added."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
