using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SchoolERP.Business.Academic.DTOs;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;
using SchoolERP.Business.Academic.Services.Interfaces;

namespace SchoolERP.Business.Academic.Services;

/// <summary>
/// Timetables are JSON-serialized structured data (variable slot count/shape per
/// class), stored as a single column and deserialized here rather than modeled as
/// a rigid relational table -- matching the reference architecture's pattern for
/// exam answer keys / notification payloads.
/// </summary>
public class TimetableService : ITimetableService
{
    private readonly ITimetableRepository _timetables;
    private readonly IDistributedCache _cache;

    public TimetableService(ITimetableRepository timetables, IDistributedCache cache)
    {
        _timetables = timetables;
        _cache = cache;
    }

    public async Task<TimetableSummary> SetAsync(SetTimetableRequest request, CancellationToken ct = default)
    {
        var slotsJson = JsonSerializer.Serialize(request.Slots);
        var existing = await _timetables.FindByClassSectionAsync(request.ClassId, request.SectionId, ct);

        Timetable timetable;
        if (existing is null)
        {
            timetable = new Timetable
            {
                ClassId = request.ClassId,
                SectionId = request.SectionId,
                SlotsJson = slotsJson
            };
            await _timetables.AddAsync(timetable, ct);
            await _timetables.SaveChangesAsync(ct);
        }
        else
        {
            await _timetables.UpdateSlotsAsync(existing.Id, slotsJson, ct);
            existing.SlotsJson = slotsJson;
            timetable = existing;
        }

        await _cache.RemoveAsync(CacheKey(request.ClassId, request.SectionId), ct);

        return ToSummary(timetable);
    }

    public async Task<TimetableSummary?> GetAsync(string classId, string sectionId, CancellationToken ct = default)
    {
        var cacheKey = CacheKey(classId, sectionId);
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<TimetableSummary>(cached);

        var timetable = await _timetables.FindByClassSectionAsync(classId, sectionId, ct);
        if (timetable is null) return null;

        var summary = ToSummary(timetable);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(summary),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            ct);

        return summary;
    }

    private static string CacheKey(string classId, string sectionId) => $"timetable:{classId}:{sectionId}";

    private static TimetableSummary ToSummary(Timetable t) =>
        new(t.Id, t.ClassId, t.SectionId, JsonSerializer.Deserialize<List<TimetableSlot>>(t.SlotsJson) ?? new(), t.EffectiveFromUtc);
}
