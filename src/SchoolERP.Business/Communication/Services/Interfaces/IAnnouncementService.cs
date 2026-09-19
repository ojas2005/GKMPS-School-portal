using SchoolERP.Business.Communication.DTOs;

namespace SchoolERP.Business.Communication.Services.Interfaces;

public interface IAnnouncementService
{
    /// <summary>
    /// SuperAdmin/Principal/Admin may target any role/class combination as given.
    /// A Teacher may only announce to the Students of the one class they are class
    /// teacher of -- actorClassTeacherOfClassId (from the caller's JWT) overrides
    /// whatever TargetClassId/TargetRolesCsv the request asked for; throws
    /// UnauthorizedAccessException if a Teacher caller isn't a class teacher at all.
    /// </summary>
    Task<AnnouncementSummary> PostAsync(CreateAnnouncementRequest request, Guid postedByUserId, string actorRole, string? actorClassTeacherOfClassId, CancellationToken ct = default);
    Task<IReadOnlyList<AnnouncementSummary>> GetRelevantAsync(string? role, string? classId, int page, int pageSize, CancellationToken ct = default);
}
