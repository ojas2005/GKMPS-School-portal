using SchoolERP.Fee.DTOs;
using SchoolERP.Fee.Entities;
using SchoolERP.Fee.Repositories.Interfaces;
using SchoolERP.Fee.Services.Interfaces;

namespace SchoolERP.Fee.Services;

public class FeeStructureService : IFeeStructureService
{
    private readonly IFeeStructureRepository _structures;

    public FeeStructureService(IFeeStructureRepository structures) => _structures = structures;

    public async Task<FeeStructureSummary> CreateAsync(CreateFeeStructureRequest request, CancellationToken ct = default)
    {
        var structure = new FeeStructure
        {
            ClassId = request.ClassId,
            Name = request.Name,
            Amount = request.Amount,
            AcademicYear = request.AcademicYear,
            DueDateUtc = request.DueDateUtc
        };

        await _structures.AddAsync(structure, ct);
        await _structures.SaveChangesAsync(ct);

        return ToSummary(structure);
    }

    public async Task<IReadOnlyList<FeeStructureSummary>> GetByClassAsync(string? classId, string? academicYear, CancellationToken ct = default)
    {
        var items = await _structures.FindByClassAsync(classId, academicYear, ct);
        return items.Select(ToSummary).ToList();
    }

    private static FeeStructureSummary ToSummary(FeeStructure f) => new(f.Id, f.ClassId, f.Name, f.Amount, f.AcademicYear, f.DueDateUtc);
}
