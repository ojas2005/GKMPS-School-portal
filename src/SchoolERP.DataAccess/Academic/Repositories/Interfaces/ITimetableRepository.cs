using SchoolERP.DataAccess.Academic.Entities;

namespace SchoolERP.DataAccess.Academic.Repositories.Interfaces;

public interface ITimetableRepository
{
    Task<Timetable?> FindByClassSectionAsync(string classId, string sectionId, CancellationToken ct = default);

    /// <summary>Every timetable -- used to build a single teacher's cross-class schedule.</summary>
    Task<IReadOnlyList<Timetable>> FindAllAsync(CancellationToken ct = default);

    Task AddAsync(Timetable timetable, CancellationToken ct = default);

    /// <summary>Atomic replace of the JSON slot config -- no load-then-save.</summary>
    Task<int> UpdateSlotsAsync(Guid id, string slotsJson, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
