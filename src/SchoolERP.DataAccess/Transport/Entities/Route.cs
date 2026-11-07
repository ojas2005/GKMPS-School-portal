using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Transport.Entities;

/// <summary>
/// A bus route and what it costs a student each month. A route that is switched off
/// (IsActive false) can't be chosen for new students; students already on it keep it until
/// moved. The monthly fee is read when each month's transport fee is created, so a new charge
/// applies from the next month on and past fees stay as they were.
/// </summary>
public class Route : BaseEntity
{
    public required string Name { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public decimal MonthlyFee { get; set; }
    public bool IsActive { get; set; } = true;
}
