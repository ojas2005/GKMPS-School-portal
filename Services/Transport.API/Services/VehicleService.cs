using SchoolERP.Transport.DTOs;
using SchoolERP.Transport.Entities;
using SchoolERP.Transport.Repositories.Interfaces;
using SchoolERP.Transport.Services.Interfaces;

namespace SchoolERP.Transport.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicles;

    public VehicleService(IVehicleRepository vehicles) => _vehicles = vehicles;

    public async Task<VehicleSummary> AddAsync(AddVehicleRequest request, CancellationToken ct = default)
    {
        if (await _vehicles.ExistsByRegistrationAsync(request.RegistrationNumber, ct))
            throw new InvalidOperationException($"A vehicle with registration '{request.RegistrationNumber}' already exists.");

        var vehicle = new Vehicle
        {
            RegistrationNumber = request.RegistrationNumber,
            Capacity = request.Capacity,
            DriverName = request.DriverName,
            DriverPhone = request.DriverPhone,
            RouteId = request.RouteId
        };

        await _vehicles.AddAsync(vehicle, ct);
        await _vehicles.SaveChangesAsync(ct);

        return ToSummary(vehicle);
    }

    private static VehicleSummary ToSummary(Vehicle v) => new(v.Id, v.RegistrationNumber, v.Capacity, v.DriverName, v.DriverPhone, v.RouteId);
}
