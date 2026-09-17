using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Transport.Entities;

public class Vehicle : BaseEntity
{
    public required string RegistrationNumber { get; set; }
    public int Capacity { get; set; }
    public required string DriverName { get; set; }
    public required string DriverPhone { get; set; }
    public Guid? RouteId { get; set; }
}
