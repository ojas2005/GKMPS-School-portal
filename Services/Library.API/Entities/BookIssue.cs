using SchoolERP.Shared.Entities;

namespace SchoolERP.Library.Entities;

public class BookIssue : BaseEntity
{
    public Guid BookId { get; set; }
    public Book? Book { get; set; }

    public Guid StudentId { get; set; }
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }

    public decimal FineAmount { get; set; } = 0;
    public bool IsFinePaid { get; set; } = false;

    public bool IsReturned => ReturnedAtUtc.HasValue;
}
