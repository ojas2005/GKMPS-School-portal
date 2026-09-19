using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Library.Entities;

/// <summary>
/// AvailableCopies is bumped atomically via ExecuteUpdateAsync on every issue/return --
/// never a load-then-save. TotalCopies - AvailableCopies == count of currently issued copies.
/// </summary>
public class Book : BaseEntity
{
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string Category { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
