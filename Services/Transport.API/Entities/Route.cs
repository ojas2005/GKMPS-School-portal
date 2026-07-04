using SchoolERP.Shared.Entities;

namespace SchoolERP.Transport.Entities;

public class Route : BaseEntity
{
    public required string Name { get; set; }
    public required string StartPoint { get; set; }
    public required string EndPoint { get; set; }
    public decimal MonthlyFee { get; set; }
}
