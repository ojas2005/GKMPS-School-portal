using SchoolERP.Transport.DTOs;

namespace SchoolERP.Transport.Services.Interfaces;

public interface IVehicleService
{
    Task<VehicleSummary> AddAsync(AddVehicleRequest request, CancellationToken ct = default);
}
