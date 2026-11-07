using SchoolERP.Common;
using SchoolERP.Business.Staff.DTOs;
using SchoolERP.DataAccess.Staff.Entities;
using SchoolERP.DataAccess.Staff.Repositories.Interfaces;
using SchoolERP.Business.Staff.Services.Interfaces;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Common.Audit;

namespace SchoolERP.Business.Staff.Services;

public class StaffService : IStaffService
{
    private readonly IStaffRepository _staff;
    private readonly IUserService _users;
    private readonly IAuditTrail _audit;
    private readonly ILogger<StaffService> _logger;

    public StaffService(IStaffRepository staff, IUserService users, IAuditTrail audit, ILogger<StaffService> logger)
    {
        _staff = staff;
        _users = users;
        _audit = audit;
        _logger = logger;
    }

    public async Task<StaffSummary> OnboardAsync(CreateStaffRequest request, CancellationToken ct = default)
    {
        if (await _staff.ExistsByEmployeeCodeAsync(request.EmployeeCode, ct))
            throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' is already in use.");

        var staff = new StaffProfile
        {
            LinkedUserId = request.LinkedUserId,
            EmployeeCode = request.EmployeeCode,
            FullName = request.FullName,
            Designation = request.Designation,
            SubjectsTaughtCsv = request.SubjectsTaughtCsv,
            Phone = request.Phone,
            Email = request.Email,
            ClassTeacherOfClassId = request.ClassTeacherOfClassId,
            ClassTeacherOfSectionId = request.ClassTeacherOfSectionId,
            MonthlySalary = request.MonthlySalary
        };

        await _staff.AddAsync(staff, ct);
        await _staff.SaveChangesAsync(ct);

        // One head teacher per class/section: displace any previous holder.
        if (!string.IsNullOrEmpty(staff.ClassTeacherOfClassId) && !string.IsNullOrEmpty(staff.ClassTeacherOfSectionId))
            await _staff.ClearClassTeacherForClassAsync(staff.ClassTeacherOfClassId, staff.ClassTeacherOfSectionId, staff.Id, ct);

        _logger.LogInformation("Staff onboarded: {StaffId} ({EmployeeCode})", staff.Id, staff.EmployeeCode);

        return ToSummary(staff);
    }

    public async Task<StaffSummary?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var staff = await _staff.FindByIdAsync(id, ct);
        return staff is null ? null : ToSummary(staff);
    }

    public async Task<StaffSummary?> GetByLinkedUserAsync(Guid linkedUserId, CancellationToken ct = default)
    {
        var staff = await _staff.FindByLinkedUserAsync(linkedUserId, ct);
        return staff is null ? null : ToSummary(staff);
    }

    public async Task<StaffSummary> AssignClassTeacherAsync(Guid staffId, AssignClassTeacherRequest request, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var staff = await _staff.FindByIdAsync(staffId, ct)
            ?? throw new KeyNotFoundException("Staff member not found.");

        var before = new { staff.ClassTeacherOfClassId, staff.ClassTeacherOfSectionId };

        await _staff.SetClassTeacherAsync(staffId, request.ClassId, request.SectionId, ct);
        if (!string.IsNullOrEmpty(request.ClassId) && !string.IsNullOrEmpty(request.SectionId))
            await _staff.ClearClassTeacherForClassAsync(request.ClassId, request.SectionId, staffId, ct);

        // Audit trail: class-teacher changes gate attendance-upload rights.
        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=Staff.AssignClassTeacher entity=StaffProfile entityId={StaffId} before={Before} after={After}",
            actorUserId, actorRole, staffId,
            System.Text.Json.JsonSerializer.Serialize(before),
            System.Text.Json.JsonSerializer.Serialize(new { request.ClassId, request.SectionId }));

        staff.ClassTeacherOfClassId = request.ClassId;
        staff.ClassTeacherOfSectionId = request.SectionId;
        return ToSummary(staff);
    }

    public async Task<StaffSummary> SetSalaryAsync(Guid staffId, SetSalaryRequest request, CancellationToken ct = default)
    {
        var staff = await _staff.FindByIdAsync(staffId, ct)
            ?? throw new KeyNotFoundException("Staff member not found.");

        await _staff.SetSalaryAsync(staffId, request.MonthlySalary, ct);

        _logger.LogInformation("AUDIT action=Staff.SetSalary entity=StaffProfile entityId={StaffId} monthlySalary={MonthlySalary}", staffId, request.MonthlySalary);

        staff.MonthlySalary = request.MonthlySalary;
        return ToSummary(staff);
    }

    public async Task<StaffSummary> RemoveAsync(Guid staffId, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var staff = await _staff.FindByIdAsync(staffId, ct) ?? throw new KeyNotFoundException("Staff member not found.");
        if (staff.Status == StaffStatuses.Removed)
            throw new InvalidOperationException("This staff member has already been removed.");

        // Their login goes first: if the caller may not manage that account, nothing changes.
        await SetLoginActiveAsync(staff.LinkedUserId, false, actorUserId, actorRole, ct);
        await _staff.UpdateStatusAsync(staffId, StaffStatuses.Removed, ct);
        if (staff.ClassTeacherOfClassId is not null)
            await _staff.SetClassTeacherAsync(staffId, null, null, ct);
        Audit("staff.removed", staff, actorUserId, actorRole);

        staff.Status = StaffStatuses.Removed;
        staff.RemovedAtUtc = DateTime.UtcNow;
        staff.ClassTeacherOfClassId = staff.ClassTeacherOfSectionId = null;
        return ToSummary(staff);
    }

    public async Task<StaffSummary> RestoreAsync(Guid staffId, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var staff = await _staff.FindByIdAsync(staffId, ct) ?? throw new KeyNotFoundException("Staff member not found.");
        if (staff.Status != StaffStatuses.Removed)
            throw new InvalidOperationException("Only a removed staff member can be brought back.");

        await SetLoginActiveAsync(staff.LinkedUserId, true, actorUserId, actorRole, ct);
        await _staff.UpdateStatusAsync(staffId, StaffStatuses.Active, ct);
        Audit("staff.restored", staff, actorUserId, actorRole);

        staff.Status = StaffStatuses.Active;
        staff.RemovedAtUtc = null;
        return ToSummary(staff);
    }

    private async Task SetLoginActiveAsync(Guid userId, bool active, string actorUserId, string actorRole, CancellationToken ct)
    {
        try { await _users.SetActiveStatusAsync(userId, active, actorUserId, actorRole, ct); }
        catch (KeyNotFoundException) { } // the login was deleted already; nothing to switch
    }

    private void Audit(string action, StaffProfile staff, string actorUserId, string actorRole) =>
        _audit.Record(new AuditRecord(DateTime.UtcNow, action, Guid.TryParse(actorUserId, out var actor) ? actor : null, actorRole,
            staff.Id.ToString(), Detail: $"employee={staff.EmployeeCode}"));

    public async Task<PagedResult<StaffSummary>> SearchAsync(string? designation, string? keyword, RosterFilter roster, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var results = await _staff.SearchAsync(designation, keyword, roster, page, pageSize, ct);
        var total = await _staff.CountAsync(designation, keyword, roster, ct);

        return new PagedResult<StaffSummary>
        {
            Items = results.Select(ToSummary).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private static StaffSummary ToSummary(StaffProfile s) =>
        new(s.Id, s.LinkedUserId, s.EmployeeCode, s.FullName, s.Designation, s.Status, s.DateOfJoiningUtc,
            s.SubjectsTaughtCsv, s.Phone, s.Email, s.ClassTeacherOfClassId, s.ClassTeacherOfSectionId, s.MonthlySalary, s.RemovedAtUtc);
}
