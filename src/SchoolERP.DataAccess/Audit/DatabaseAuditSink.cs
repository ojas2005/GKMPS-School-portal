using Microsoft.EntityFrameworkCore;
using SchoolERP.Common.Audit;

namespace SchoolERP.DataAccess.Audit;

public class DatabaseAuditSink : IAuditSink
{
    private readonly AuditDbContext _db;

    public DatabaseAuditSink(AuditDbContext db) => _db = db;

    public async Task WriteAsync(IReadOnlyList<AuditRecord> records, CancellationToken ct)
    {
        _db.Entries.AddRange(records.Select(r => new AuditEntry
        {
            TimestampUtc = r.TimestampUtc,
            Action = Clip(r.Action, 150)!,
            ActorUserId = r.ActorUserId,
            ActorRole = Clip(r.ActorRole, 32),
            EntityId = Clip(r.EntityId, 64),
            StatusCode = r.StatusCode,
            IpAddress = Clip(r.IpAddress, 64),
            Detail = Clip(r.Detail, 500)
        }));
        await _db.SaveChangesAsync(ct);
        _db.ChangeTracker.Clear();
    }

    public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct) =>
        _db.Entries.Where(e => e.TimestampUtc < cutoffUtc).ExecuteDeleteAsync(ct);

    private static string? Clip(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}
