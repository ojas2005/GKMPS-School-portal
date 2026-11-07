using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Transport.DTOs;

public record CreateRouteRequest(
    [Required, StringLength(100), Display(Name = "Route name")] string Name,
    [StringLength(200), Display(Name = "Start point")] string? StartPoint,
    [StringLength(200), Display(Name = "End point")] string? EndPoint,
    [Range(0, 1_000_000), Display(Name = "Monthly charge")] decimal MonthlyFee);

public record UpdateRouteRequest(
    [Required, StringLength(100), Display(Name = "Route name")] string Name,
    [StringLength(200), Display(Name = "Start point")] string? StartPoint,
    [StringLength(200), Display(Name = "End point")] string? EndPoint,
    [Range(0, 1_000_000), Display(Name = "Monthly charge")] decimal MonthlyFee,
    bool IsActive);

public record RouteSummary(Guid Id, string Name, string? StartPoint, string? EndPoint, decimal MonthlyFee, bool IsActive = true, int StudentCount = 0);

public record AddVehicleRequest([Required] string RegistrationNumber, [Required] int Capacity, [Required] string DriverName, [Required] string DriverPhone, Guid? RouteId);
public record VehicleSummary(Guid Id, string RegistrationNumber, int Capacity, string DriverName, string DriverPhone, Guid? RouteId);

public record AssignStudentRouteRequest([Required] Guid StudentId, [Required] Guid RouteId, [StringLength(200)] string? PickupPoint);
public record StudentRouteMappingSummary(Guid Id, Guid StudentId, Guid RouteId, string PickupPoint, DateTime? AssignedAtUtc = null);

/// <summary>A student's transport: the route, its current monthly charge and the pickup point.</summary>
public record StudentTransportSummary(Guid StudentId, Guid RouteId, string RouteName, decimal MonthlyFee, bool RouteActive, string PickupPoint, DateTime AssignedAtUtc);

// Route detail page's students-on-route table -- enriched with the student's name/admission
// number (looked up through the Student module, which owns them) so the owner isn't
// just looking at raw GUIDs.
public record StudentRouteMappingWithNameSummary(Guid Id, Guid StudentId, string StudentName, string? AdmissionNumber, Guid RouteId, string PickupPoint);
