using SchoolERP.Business.Academic.DTOs;

namespace SchoolERP.Business.Academic.Services.Interfaces;

public interface ITimetableService
{
    Task<TimetableSummary> SetAsync(SetTimetableRequest request, CancellationToken ct = default);

    /// <summary>Cached lookup (5-min TTL) since timetables are read far more often than written.</summary>
    Task<TimetableSummary?> GetAsync(string classId, string sectionId, CancellationToken ct = default);
}
