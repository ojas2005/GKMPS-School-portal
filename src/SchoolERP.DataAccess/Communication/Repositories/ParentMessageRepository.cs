using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Communication;
using SchoolERP.DataAccess.Communication.Entities;
using SchoolERP.DataAccess.Communication.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Communication.Repositories;

public class ParentMessageRepository : IParentMessageRepository
{
    private readonly CommunicationDbContext _db;

    public ParentMessageRepository(CommunicationDbContext db) => _db = db;

    public Task<IReadOnlyList<ParentMessage>> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default) =>
        _db.ParentMessages.Where(m => m.StudentId == studentId).OrderBy(m => m.CreatedAtUtc).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<ParentMessage>)t.Result, ct);

    public Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken ct = default) =>
        _db.ParentMessages.CountAsync(m => m.RecipientUserId == recipientUserId && !m.IsRead, ct);

    public async Task AddAsync(ParentMessage message, CancellationToken ct = default) =>
        await _db.ParentMessages.AddAsync(message, ct);

    public Task<int> MarkReadAsync(Guid messageId, Guid recipientUserId, CancellationToken ct = default) =>
        _db.ParentMessages.Where(m => m.Id == messageId && m.RecipientUserId == recipientUserId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
