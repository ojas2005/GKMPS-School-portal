using SchoolERP.Academic.DTOs;

namespace SchoolERP.Academic.Services.Interfaces;

public interface ITimetableService
{
    Task<TimetableSummary> SetAsync(SetTimetableRequest request, CancellationToken ct = default);

    /// <summary>Redis-cached lookup (5-min TTL) since timetables are read far more often than written.</summary>
    Task<TimetableSummary?> GetAsync(string classId, string sectionId, CancellationToken ct = default);
}
