using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Transport.DTOs;

public record CreateRouteRequest([Required] string Name, [Required] string StartPoint, [Required] string EndPoint, [Required] decimal MonthlyFee);
public record RouteSummary(Guid Id, string Name, string StartPoint, string EndPoint, decimal MonthlyFee);

public record AddVehicleRequest([Required] string RegistrationNumber, [Required] int Capacity, [Required] string DriverName, [Required] string DriverPhone, Guid? RouteId);
public record VehicleSummary(Guid Id, string RegistrationNumber, int Capacity, string DriverName, string DriverPhone, Guid? RouteId);

public record AssignStudentRouteRequest([Required] Guid StudentId, [Required] Guid RouteId, [Required] string PickupPoint);
public record StudentRouteMappingSummary(Guid Id, Guid StudentId, Guid RouteId, string PickupPoint);

// Route detail page's students-on-route table -- enriched with the student's name/admission
// number (fetched from Student.API, which Transport.API doesn't own) so the owner isn't
// just looking at raw GUIDs.
public record StudentRouteMappingWithNameSummary(Guid Id, Guid StudentId, string StudentName, string? AdmissionNumber, Guid RouteId, string PickupPoint);
