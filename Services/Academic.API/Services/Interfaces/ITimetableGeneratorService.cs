using SchoolERP.Academic.DTOs;

namespace SchoolERP.Academic.Services.Interfaces;

public interface ITimetableGeneratorService
{
    /// <summary>Builds and persists a timetable for every covered class from the saved
    /// config. Returns the classes written plus any infeasibility warnings.</summary>
    Task<GenerateTimetableResult> GenerateAsync(CancellationToken ct = default);

    /// <summary>One teacher's own schedule, gathered across every class they appear in.</summary>
    Task<TeacherTimetableResponse> GetForTeacherAsync(Guid staffId, CancellationToken ct = default);
}
