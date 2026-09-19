using SchoolERP.Business.Transport.DTOs;

namespace SchoolERP.Business.Transport.Services.Interfaces;

public interface IVehicleService
{
    Task<VehicleSummary> AddAsync(AddVehicleRequest request, CancellationToken ct = default);
}
