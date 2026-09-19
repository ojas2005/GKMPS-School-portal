using SchoolERP.Business.Fee.DTOs;

namespace SchoolERP.Business.Fee.Services.Interfaces;

public interface IFeeStructureService
{
    Task<FeeStructureSummary> CreateAsync(CreateFeeStructureRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FeeStructureSummary>> GetByClassAsync(string? classId, string? academicYear, CancellationToken ct = default);
}
