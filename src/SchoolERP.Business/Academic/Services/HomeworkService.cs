using SchoolERP.Business.Academic.DTOs;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;
using SchoolERP.Business.Academic.Services.Interfaces;

namespace SchoolERP.Business.Academic.Services;

public class HomeworkService : IHomeworkService
{
    private readonly IHomeworkRepository _homework;

    public HomeworkService(IHomeworkRepository homework) => _homework = homework;

    public async Task<HomeworkSummary> AssignAsync(CreateHomeworkRequest request, Guid assignedByStaffId, CancellationToken ct = default)
    {
        var homework = new Homework
        {
            ClassId = request.ClassId,
            SectionId = request.SectionId,
            SubjectId = request.SubjectId,
            Title = request.Title,
            Description = request.Description,
            DueDateUtc = request.DueDateUtc,
            AssignedByStaffId = assignedByStaffId
        };

        await _homework.AddAsync(homework, ct);
        await _homework.SaveChangesAsync(ct);

        return ToSummary(homework);
    }

    public async Task<IReadOnlyList<HomeworkSummary>> GetByClassAsync(string classId, string sectionId, CancellationToken ct = default)
    {
        var items = await _homework.FindByClassAsync(classId, sectionId, ct);
        return items.Select(ToSummary).ToList();
    }

    private static HomeworkSummary ToSummary(Homework h) => new(h.Id, h.ClassId, h.SectionId, h.SubjectId, h.Title, h.DueDateUtc);
}
