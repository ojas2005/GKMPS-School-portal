using SchoolERP.Business.Communication.DTOs;
using SchoolERP.DataAccess.Communication.Entities;
using SchoolERP.DataAccess.Communication.Repositories.Interfaces;
using SchoolERP.Business.Communication.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Business.Communication.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _announcements;

    public AnnouncementService(IAnnouncementRepository announcements) => _announcements = announcements;

    public async Task<AnnouncementSummary> PostAsync(CreateAnnouncementRequest request, Guid postedByUserId, string actorRole, string? actorClassTeacherOfClassId, CancellationToken ct = default)
    {
        var targetRolesCsv = request.TargetRolesCsv;
        var targetClassId = request.TargetClassId;

        if (actorRole == RoleNames.Teacher)
        {
            if (string.IsNullOrEmpty(actorClassTeacherOfClassId))
                throw new UnauthorizedAccessException("Only a class teacher (head teacher) can send announcements.");

            // A teacher may only reach the students of the one class they head --
            // whatever the request asked for is overridden, not merely validated.
            targetClassId = actorClassTeacherOfClassId;
            targetRolesCsv = RoleNames.Student;
        }

        var announcement = new Announcement
        {
            Title = request.Title,
            Body = request.Body,
            TargetRolesCsv = targetRolesCsv,
            TargetClassId = targetClassId,
            ExpiresAtUtc = request.ExpiresAtUtc,
            PostedByUserId = postedByUserId
        };

        await _announcements.AddAsync(announcement, ct);
        await _announcements.SaveChangesAsync(ct);

        return ToSummary(announcement);
    }

    public async Task<IReadOnlyList<AnnouncementSummary>> GetRelevantAsync(string? role, string? classId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var items = await _announcements.SearchAsync(role, classId, page, pageSize, ct);
        return items.Select(ToSummary).ToList();
    }

    private static AnnouncementSummary ToSummary(Announcement a) => new(a.Id, a.Title, a.Body, a.TargetRolesCsv, a.TargetClassId, a.PublishedAtUtc);
}
