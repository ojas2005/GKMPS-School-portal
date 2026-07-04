using Microsoft.EntityFrameworkCore;
using SchoolERP.Communication.Data;
using SchoolERP.Communication.Entities;
using SchoolERP.Communication.Repositories.Interfaces;

namespace SchoolERP.Communication.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly CommunicationDbContext _db;

    public AnnouncementRepository(CommunicationDbContext db) => _db = db;

    public Task<Announcement?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Announcement>> SearchAsync(string? role, string? classId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Announcements.Where(a => a.ExpiresAtUtc == null || a.ExpiresAtUtc > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(a => a.TargetRolesCsv == null || a.TargetRolesCsv == "" || a.TargetRolesCsv.Contains(role));

        if (!string.IsNullOrWhiteSpace(classId))
            query = query.Where(a => a.TargetClassId == null || a.TargetClassId == classId);

        return await query.OrderByDescending(a => a.PublishedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task AddAsync(Announcement announcement, CancellationToken ct = default) =>
        await _db.Announcements.AddAsync(announcement, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
