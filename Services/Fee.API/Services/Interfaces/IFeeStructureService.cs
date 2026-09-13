using SchoolERP.Fee.DTOs;

namespace SchoolERP.Fee.Services.Interfaces;

public interface IFeeStructureService
{
    Task<FeeStructureSummary> CreateAsync(CreateFeeStructureRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FeeStructureSummary>> GetByClassAsync(string? classId, string? academicYear, CancellationToken ct = default);
}
