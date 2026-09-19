using Microsoft.EntityFrameworkCore;
using SchoolERP.Common;

namespace SchoolERP.DataAccess.Audit;

public record AuditQuery(Guid? ActorUserId, string? EntityId, string? Action, DateTime? FromUtc, DateTime? ToUtc, int Page, int PageSize);

public interface IAuditReader
{
    Task<PagedResult<AuditEntry>> SearchAsync(AuditQuery query, CancellationToken ct = default);
}

public class AuditReader : IAuditReader
{
    private readonly AuditDbContext _db;

    public AuditReader(AuditDbContext db) => _db = db;

    public async Task<PagedResult<AuditEntry>> SearchAsync(AuditQuery q, CancellationToken ct = default)
    {
        var entries = _db.Entries.AsNoTracking().AsQueryable();
        if (q.ActorUserId is { } actor) entries = entries.Where(e => e.ActorUserId == actor);
        if (!string.IsNullOrWhiteSpace(q.EntityId)) entries = entries.Where(e => e.EntityId == q.EntityId);
        if (!string.IsNullOrWhiteSpace(q.Action)) entries = entries.Where(e => e.Action.StartsWith(q.Action));
        if (q.FromUtc is { } from) entries = entries.Where(e => e.TimestampUtc >= from);
        if (q.ToUtc is { } to) entries = entries.Where(e => e.TimestampUtc <= to);

        var total = await entries.CountAsync(ct);
        var items = await entries.OrderByDescending(e => e.TimestampUtc).ThenByDescending(e => e.Id)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return new PagedResult<AuditEntry> { Items = items, PageNumber = q.Page, PageSize = q.PageSize, TotalCount = total };
    }
}
