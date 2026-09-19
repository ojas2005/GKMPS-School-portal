using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SchoolERP.Business.Academic.Constants;
using SchoolERP.Business.Academic.DTOs;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;
using SchoolERP.Business.Academic.Services.Interfaces;

namespace SchoolERP.Business.Academic.Services;

/// <summary>
/// Turns the owner's saved config into concrete per-class timetables.
///
/// Junior (Classes 1-5) and Senior (Classes 6-10) are scheduled as two INDEPENDENT
/// problems, each with only its own group's teachers and classes -- matching the owner's
/// mental model of "two separate rosters". This also means a shortfall in one group (too
/// few teachers to cover every period) only blocks that group; the other group's classes
/// still get their timetable written.
///
/// For each of the 8 periods we solve a bipartite matching (classes <-> eligible teachers)
/// that must cover EVERY class; a teacher can take at most one class per period and at most
/// MaxPeriodsPerDay across the day. A class must NOT see the same teacher/subject for the
/// entire day when other eligible teachers exist for it: each teacher's daily cap is first
/// split into a per-class period TARGET (as evenly as possible, across the classes they
/// teach, in the order given), and the per-period matching prefers continuing the class's
/// current teacher only until that target is used up, then rotates to whichever eligible
/// teacher still owes periods to that class -- e.g. a teacher with an 8-period cap across
/// 5 classes gets contiguous blocks of 2/2/2/1/1, not a single class for the whole day.
/// Targets only steer WHICH valid matching is picked among ties; they never block a class
/// from being covered, so feasibility is unaffected. The algorithm is deterministic (sorted
/// inputs, stable tie-breaks) so re-running produces the identical timetable -- and that
/// single day is copied to all six teaching days, satisfying "same timetable, not shuffled".
///
/// Pre-primary classes (PG/Nursery/LKG/UKG) skip the matching entirely: their own class
/// teacher takes all 8 periods (there is no other teacher to rotate in).
/// </summary>
public class TimetableGeneratorService : ITimetableGeneratorService
{
    private readonly IScheduleConfigRepository _configs;
    private readonly ITimetableRepository _timetables;
    private readonly IDistributedCache _cache;

    public TimetableGeneratorService(
        IScheduleConfigRepository configs,
        ITimetableRepository timetables,
        IDistributedCache cache)
    {
        _configs = configs;
        _timetables = timetables;
        _cache = cache;
    }

    private sealed class Teacher
    {
        public required Guid StaffId { get; init; }
        public required string SubjectName { get; init; }
        // Ordered (not a HashSet) -- the order drives the block-splitting below, and is
        // what the owner sees/controls via the order they picked classes in the UI.
        public required List<string> ClassIds { get; init; }
        public int Remaining { get; set; }

        // How many of today's periods this teacher is meant to spend in each class
        // (computed once, see AssignClassTargets) vs. how many they've actually used so far.
        public Dictionary<string, int> ClassTarget { get; } = new();
        public Dictionary<string, int> ClassUsed { get; } = new();
    }

    public async Task<GenerateTimetableResult> GenerateAsync(CancellationToken ct = default)
    {
        var configEntity = await _configs.GetCurrentAsync(ct);
        var config = configEntity is null
            ? null
            : JsonSerializer.Deserialize<SaveScheduleConfigRequest>(configEntity.ConfigJson);

        if (config is null)
            return new GenerateTimetableResult(new(), new() { "No timetable configuration has been saved yet." });

        var warnings = new List<string>();
        var generated = new List<string>();

        // Junior and Senior are scheduled as two INDEPENDENT problems (see class remarks),
        // which only guarantees no double-booking *within* each one. Two things can still
        // put the same physical person in two places at once, so both are resolved here,
        // before either group is scheduled, by excluding the offending staff entirely and
        // warning the owner:
        //   1. A pre-primary class teacher already teaches all 8 periods in their class --
        //      zero capacity left for anything else.
        //   2. The same teacher listed in BOTH Junior and Senior -- each group's matcher
        //      would schedule them with no idea the other group exists.
        var prePrimaryTeacherIds = config.PrePrimary.Select(pp => pp.ClassTeacherStaffId).ToHashSet();
        var crossGroupTeacherIds = config.Junior.Select(a => a.StaffId)
            .Intersect(config.Senior.Select(a => a.StaffId)).ToHashSet();

        // ---- Junior and Senior: two independent scheduling problems ----
        foreach (var (groupName, groupAssignments) in new[] { ("Junior", config.Junior), ("Senior", config.Senior) })
        {
            foreach (var a in groupAssignments.Where(a => prePrimaryTeacherIds.Contains(a.StaffId)))
                warnings.Add(
                    $"Teacher {a.StaffId} is a pre-primary class teacher (already teaching all 8 periods there) " +
                    $"and can't also be scheduled in the {groupName} group -- remove them here or reassign the pre-primary class.");
            foreach (var a in groupAssignments.Where(a => crossGroupTeacherIds.Contains(a.StaffId)))
                warnings.Add(
                    $"Teacher {a.StaffId} is listed in both Junior and Senior groups, which could double-book them -- " +
                    "keep them in only one group.");

            var excluded = prePrimaryTeacherIds.Union(crossGroupTeacherIds);
            var pool = BuildTeacherPool(groupAssignments.Where(a => !excluded.Contains(a.StaffId)));
            var coveredClasses = pool.SelectMany(t => t.ClassIds).Distinct().OrderBy(c => c).ToList();
            if (coveredClasses.Count == 0) continue;

            var (slotsByClass, groupWarnings) = ScheduleGroup(coveredClasses, pool);
            if (groupWarnings.Count > 0)
            {
                // Interdependent schedule: if any class/period in THIS group can't be
                // covered, don't persist a half-filled set for it -- report what to fix.
                // The other group is scheduled and written independently of this outcome.
                warnings.AddRange(groupWarnings);
            }
            else
            {
                foreach (var (classId, slots) in slotsByClass)
                {
                    await UpsertTimetableAsync(classId, TimetableConstants.DefaultSectionId, slots, ct);
                    generated.Add(classId);
                }
            }
        }

        // ---- Pre-primary: class teacher takes every period (always feasible) ----
        foreach (var pp in config.PrePrimary)
        {
            var slots = BuildAllPeriodSlots(pp.ClassTeacherStaffId, string.IsNullOrWhiteSpace(pp.SubjectName) ? "General" : pp.SubjectName);
            await UpsertTimetableAsync(pp.ClassId, TimetableConstants.DefaultSectionId, slots, ct);
            generated.Add(pp.ClassId);
        }

        return new GenerateTimetableResult(generated.OrderBy(c => c).ToList(), warnings);
    }

    /// <summary>Merge the two input lists by staffId so a teacher listed in both groups is a
    /// single scheduling unit (unioned classes, one cap, first subject seen).</summary>
    private static List<Teacher> BuildTeacherPool(IEnumerable<TeacherAssignmentDto> assignments)
    {
        var byStaff = new Dictionary<Guid, Teacher>();
        foreach (var a in assignments)
        {
            if (byStaff.TryGetValue(a.StaffId, out var existing))
            {
                foreach (var c in a.ClassIds) if (!existing.ClassIds.Contains(c)) existing.ClassIds.Add(c);
                existing.Remaining = Math.Max(existing.Remaining, a.MaxPeriodsPerDay);
            }
            else
            {
                byStaff[a.StaffId] = new Teacher
                {
                    StaffId = a.StaffId,
                    SubjectName = a.SubjectName,
                    ClassIds = a.ClassIds.Distinct().ToList(),
                    Remaining = a.MaxPeriodsPerDay
                };
            }
        }
        var pool = byStaff.Values.OrderBy(t => t.StaffId).ToList();
        foreach (var t in pool) AssignClassTargets(t);
        return pool;
    }

    /// <summary>Split a teacher's daily cap into a per-class target as evenly as possible,
    /// in the order their classes were listed: e.g. an 8-period cap across 5 classes gives
    /// targets of 2,2,2,1,1 -- the first (Remaining % classCount) classes get one extra
    /// period. This is what turns into contiguous same-teacher blocks in ScheduleGroup.</summary>
    private static void AssignClassTargets(Teacher t)
    {
        var n = t.ClassIds.Count;
        if (n == 0) return;
        var basePeriods = t.Remaining / n;
        var remainder = t.Remaining % n;
        for (var i = 0; i < n; i++)
        {
            t.ClassTarget[t.ClassIds[i]] = basePeriods + (i < remainder ? 1 : 0);
            t.ClassUsed[t.ClassIds[i]] = 0;
        }
    }

    /// <summary>Assign a teacher to every (class, period) for the representative day, then
    /// fan the result out across all six teaching days. Returns slots-per-class, or warnings
    /// if some period couldn't be fully covered.</summary>
    private static (Dictionary<string, List<TimetableSlot>> slotsByClass, List<string> warnings) ScheduleGroup(
        List<string> classes, List<Teacher> pool)
    {
        var warnings = new List<string>();
        // period (1..8) -> classId -> chosen teacher
        var dayPlan = new Dictionary<int, Dictionary<string, Teacher>>();
        // classId -> staffId who held this class last period, so a teacher who hasn't yet
        // used up their target for this class keeps it (contiguous block) instead of the
        // matcher re-picking arbitrarily every single period.
        var lastTeacherForClass = new Dictionary<string, Guid>();

        foreach (var pt in TimetableConstants.Periods)
        {
            // Try WITH continuations locked in first (clean contiguous blocks); a teacher
            // can only ever be "continuing" one class at a time, so locked pairs can never
            // conflict with each other. Only if that leaves some class uncoverable do we
            // fall back to matching everyone from scratch -- coverage must never regress
            // just to keep a block contiguous.
            var (locked, lockedUnmatched) = TryMatchPeriod(classes, pool, lastTeacherForClass, lockContinuations: true);
            var (classToTeacher, unmatched) = lockedUnmatched.Count == 0
                ? (locked, lockedUnmatched)
                : TryMatchPeriod(classes, pool, lastTeacherForClass, lockContinuations: false);

            if (unmatched.Count > 0)
            {
                foreach (var c in unmatched)
                    warnings.Add($"Class {c} · Period {pt.Period}: no available teacher — assign another teacher to this class or raise a daily cap.");
                continue; // keep collecting warnings for other periods
            }

            foreach (var (c, teacher) in classToTeacher)
            {
                teacher.Remaining--;
                teacher.ClassUsed[c] = teacher.ClassUsed.GetValueOrDefault(c) + 1;
                lastTeacherForClass[c] = teacher.StaffId;
            }
            dayPlan[pt.Period] = classToTeacher;
        }

        if (warnings.Count > 0) return (new(), warnings);

        // Fan the single-day plan out to every teaching day.
        var slotsByClass = classes.ToDictionary(c => c, _ => new List<TimetableSlot>());
        foreach (var day in TimetableConstants.TeachingDays)
            foreach (var pt in TimetableConstants.Periods)
            {
                var teacher = dayPlan[pt.Period];
                foreach (var c in classes)
                    slotsByClass[c].Add(new TimetableSlot(day, pt.Period, null, teacher[c].SubjectName, teacher[c].StaffId, pt.StartTime, pt.EndTime));
            }

        return (slotsByClass, warnings);
    }

    /// <summary>Matches every class to a teacher for one period. When <paramref name="lockContinuations"/>
    /// is true, any class whose previous-period teacher hasn't yet used up their target for it
    /// is assigned to them directly first (these locked pairs can never conflict with each
    /// other, since a teacher can only have been "last" for one class) -- this is what
    /// keeps a teacher's periods in a class as one contiguous block instead of the matcher
    /// re-picking freely every period. Whatever's left (or everything, when the flag is
    /// false) is solved by the normal augmenting-path matching, preferring teachers who
    /// still owe the class the most periods. Returns the assignment plus any classes that
    /// still couldn't be covered.</summary>
    private static (Dictionary<string, Teacher> assigned, List<string> unmatched) TryMatchPeriod(
        List<string> classes, List<Teacher> pool, Dictionary<string, Guid> lastTeacherForClass, bool lockContinuations)
    {
        var classToTeacher = new Dictionary<string, Teacher>();
        var teacherToClass = new Dictionary<Guid, string>();

        if (lockContinuations)
        {
            foreach (var c in classes)
            {
                if (!lastTeacherForClass.TryGetValue(c, out var lastStaffId)) continue;
                var teacher = pool.FirstOrDefault(t => t.StaffId == lastStaffId);
                if (teacher is null || teacher.Remaining <= 0 || !teacher.ClassIds.Contains(c)) continue;
                if (teacher.ClassUsed.GetValueOrDefault(c) >= teacher.ClassTarget.GetValueOrDefault(c)) continue;
                classToTeacher[c] = teacher;
                teacherToClass[teacher.StaffId] = c;
            }
        }

        // Only classes/teachers not already locked above take part in the matching --
        // this is also what keeps TryAssign's recursive re-matching from ever touching (and
        // so never breaks) a locked pair: it can only reach teachers reachable via `eligible`.
        var remaining = classes.Where(c => !classToTeacher.ContainsKey(c)).ToList();
        var eligible = remaining.ToDictionary(
            c => c,
            c => pool.Where(t => t.Remaining > 0 && t.ClassIds.Contains(c) && !teacherToClass.ContainsKey(t.StaffId))
                .OrderByDescending(t => t.ClassTarget.GetValueOrDefault(c) - t.ClassUsed.GetValueOrDefault(c))
                .ThenByDescending(t => t.Remaining)
                .ThenBy(t => t.StaffId)
                .ToList());

        // Most-constrained class first (fewest eligible teachers) reduces match failures.
        var order = remaining.OrderBy(c => eligible[c].Count).ThenBy(c => c).ToList();
        foreach (var c in order)
        {
            var visited = new HashSet<Guid>();
            TryAssign(c, eligible, classToTeacher, teacherToClass, visited);
        }

        var unmatched = classes.Where(c => !classToTeacher.ContainsKey(c)).ToList();
        return (classToTeacher, unmatched);
    }

    // Kuhn's augmenting-path step: try to give class `c` an eligible teacher, bumping an
    // already-matched teacher to another option if needed.
    private static bool TryAssign(
        string c,
        Dictionary<string, List<Teacher>> eligible,
        Dictionary<string, Teacher> classToTeacher,
        Dictionary<Guid, string> teacherToClass,
        HashSet<Guid> visited)
    {
        foreach (var teacher in eligible[c])
        {
            if (!visited.Add(teacher.StaffId)) continue;
            if (!teacherToClass.TryGetValue(teacher.StaffId, out var otherClass)
                || TryAssign(otherClass, eligible, classToTeacher, teacherToClass, visited))
            {
                classToTeacher[c] = teacher;
                teacherToClass[teacher.StaffId] = c;
                return true;
            }
        }
        return false;
    }

    private static List<TimetableSlot> BuildAllPeriodSlots(Guid teacherStaffId, string subjectName)
    {
        var slots = new List<TimetableSlot>();
        foreach (var day in TimetableConstants.TeachingDays)
            foreach (var pt in TimetableConstants.Periods)
                slots.Add(new TimetableSlot(day, pt.Period, null, subjectName, teacherStaffId, pt.StartTime, pt.EndTime));
        return slots;
    }

    private async Task UpsertTimetableAsync(string classId, string sectionId, List<TimetableSlot> slots, CancellationToken ct)
    {
        var slotsJson = JsonSerializer.Serialize(slots);
        var existing = await _timetables.FindByClassSectionAsync(classId, sectionId, ct);
        if (existing is null)
        {
            await _timetables.AddAsync(new Timetable { ClassId = classId, SectionId = sectionId, SlotsJson = slotsJson }, ct);
            await _timetables.SaveChangesAsync(ct);
        }
        else
        {
            await _timetables.UpdateSlotsAsync(existing.Id, slotsJson, ct);
        }
        // Same key TimetableService caches under, so the class view reflects the new schedule.
        await _cache.RemoveAsync($"timetable:{classId}:{sectionId}", ct);
    }

    public async Task<TeacherTimetableResponse> GetForTeacherAsync(Guid staffId, CancellationToken ct = default)
    {
        var all = await _timetables.FindAllAsync(ct);
        var dayOrder = TimetableConstants.TeachingDays
            .Select((d, i) => (d, i)).ToDictionary(x => x.d, x => x.i);

        var slots = new List<TeacherSlot>();
        foreach (var tt in all)
        {
            var parsed = JsonSerializer.Deserialize<List<TimetableSlot>>(tt.SlotsJson) ?? new();
            foreach (var s in parsed.Where(s => s.TeacherStaffId == staffId))
                slots.Add(new TeacherSlot(s.Day, s.Period, tt.ClassId, tt.SectionId, s.SubjectName, s.StartTime, s.EndTime));
        }

        var ordered = slots
            .OrderBy(s => dayOrder.TryGetValue(s.Day, out var i) ? i : int.MaxValue)
            .ThenBy(s => s.Period)
            .ToList();

        return new TeacherTimetableResponse(staffId, ordered);
    }
}
