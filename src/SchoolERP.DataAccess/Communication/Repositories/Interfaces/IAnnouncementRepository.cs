using SchoolERP.DataAccess.Communication.Entities;

namespace SchoolERP.DataAccess.Communication.Repositories.Interfaces;

public interface IAnnouncementRepository
{
    Task<Announcement?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Announcement>> SearchAsync(string? role, string? classId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Announcement announcement, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
