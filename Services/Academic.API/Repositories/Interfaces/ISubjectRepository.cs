using SchoolERP.Academic.Entities;

namespace SchoolERP.Academic.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<Subject?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Subject>> FindByClassAsync(string classId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string classId, string code, CancellationToken ct = default);
    Task AddAsync(Subject subject, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
