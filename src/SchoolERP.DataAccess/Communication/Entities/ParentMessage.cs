using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Communication.Entities;

/// <summary>One row per Teacher &lt;-&gt; Parent message thread entry. IsRead follows a simple two-state flag flipped atomically on read.</summary>
public class ParentMessage : BaseEntity
{
    public Guid StudentId { get; set; }              // conversation is scoped to a student
    public Guid SenderUserId { get; set; }
    public Guid RecipientUserId { get; set; }
    public required string Body { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAtUtc { get; set; }
}
