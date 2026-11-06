using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Common;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;
using SchoolERP.Business.Fee.Services.Interfaces;

namespace SchoolERP.Business.Fee.Services;

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
            AcademicYear = AcademicYears.Normalize(request.AcademicYear),
            DueDateUtc = request.DueDateUtc
        };

        await _structures.AddAsync(structure, ct);
        await _structures.SaveChangesAsync(ct);

        return ToSummary(structure);
    }

    public async Task<IReadOnlyList<FeeStructureSummary>> GetByClassAsync(string? classId, string? academicYear, CancellationToken ct = default)
    {
        // Older structures may spell the year differently ("2026-2027"), so match by meaning.
        var items = await _structures.FindByClassAsync(classId, null, ct);
        return items.Where(f => academicYear is null || AcademicYears.Same(f.AcademicYear, academicYear)).Select(ToSummary).ToList();
    }

    private static FeeStructureSummary ToSummary(FeeStructure f) => new(f.Id, f.ClassId, f.Name, f.Amount, f.AcademicYear, f.DueDateUtc);
}
