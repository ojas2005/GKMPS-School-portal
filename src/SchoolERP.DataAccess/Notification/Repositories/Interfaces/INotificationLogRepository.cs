using SchoolERP.DataAccess.Notification.Entities;

namespace SchoolERP.DataAccess.Notification.Repositories.Interfaces;

public interface INotificationLogRepository
{
    Task<IReadOnlyList<NotificationLog>> FindByRecipientAsync(string recipientReference, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(NotificationLog log, CancellationToken ct = default);

    /// <summary>Atomic delivery-flag flip -- no load-then-save.</summary>
    Task<int> MarkDeliveredAsync(Guid id, CancellationToken ct = default);
    Task<int> MarkFailedAsync(Guid id, string reason, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
