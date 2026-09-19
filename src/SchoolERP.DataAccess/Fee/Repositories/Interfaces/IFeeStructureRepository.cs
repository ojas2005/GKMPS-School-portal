using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.DataAccess.Fee.Repositories.Interfaces;

public interface IFeeStructureRepository
{
    Task<FeeStructure?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FeeStructure>> FindByClassAsync(string? classId, string? academicYear, CancellationToken ct = default);
    Task AddAsync(FeeStructure structure, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
