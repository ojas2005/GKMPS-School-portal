using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Notification;
using SchoolERP.DataAccess.Notification.Entities;
using SchoolERP.DataAccess.Notification.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Notification.Repositories;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly NotificationDbContext _db;

    public NotificationLogRepository(NotificationDbContext db) => _db = db;

    public Task<IReadOnlyList<NotificationLog>> FindByRecipientAsync(string recipientReference, int page, int pageSize, CancellationToken ct = default) =>
        _db.NotificationLogs.Where(n => n.RecipientReference == recipientReference)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<NotificationLog>)t.Result, ct);

    public async Task AddAsync(NotificationLog log, CancellationToken ct = default) =>
        await _db.NotificationLogs.AddAsync(log, ct);

    public Task<int> MarkDeliveredAsync(Guid id, CancellationToken ct = default) =>
        _db.NotificationLogs.Where(n => n.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsDelivered, true)
                .SetProperty(n => n.DeliveredAtUtc, DateTime.UtcNow), ct);

    public Task<int> MarkFailedAsync(Guid id, string reason, CancellationToken ct = default) =>
        _db.NotificationLogs.Where(n => n.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.FailureReason, reason), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
