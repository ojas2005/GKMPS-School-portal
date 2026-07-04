using SchoolERP.Communication.Entities;

namespace SchoolERP.Communication.Repositories.Interfaces;

public interface IParentMessageRepository
{
    Task<IReadOnlyList<ParentMessage>> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default);
    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken ct = default);
    Task AddAsync(ParentMessage message, CancellationToken ct = default);

    /// <summary>Atomic read-flag flip -- no load-then-save.</summary>
    Task<int> MarkReadAsync(Guid messageId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
