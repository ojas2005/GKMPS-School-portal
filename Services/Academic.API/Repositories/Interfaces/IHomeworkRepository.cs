using SchoolERP.Academic.Entities;

namespace SchoolERP.Academic.Repositories.Interfaces;

public interface IHomeworkRepository
{
    Task<Homework?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Homework>> FindByClassAsync(string classId, string sectionId, CancellationToken ct = default);
    Task AddAsync(Homework homework, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
