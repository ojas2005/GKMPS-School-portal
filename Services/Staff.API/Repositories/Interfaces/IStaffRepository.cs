using SchoolERP.Staff.Entities;

namespace SchoolERP.Staff.Repositories.Interfaces;

public interface IStaffRepository
{
    Task<StaffProfile?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<StaffProfile?> FindByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default);
    Task<StaffProfile?> FindByLinkedUserAsync(Guid linkedUserId, CancellationToken ct = default);
    Task<IReadOnlyList<StaffProfile>> SearchAsync(string? designation, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(string? designation, CancellationToken ct = default);
    Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default);
    Task AddAsync(StaffProfile staff, CancellationToken ct = default);

    /// <summary>Atomic status transition (Active/OnLeave/Suspended/Resigned).</summary>
    Task<int> UpdateStatusAsync(Guid staffId, string status, CancellationToken ct = default);

    /// <summary>Atomic class-teacher (head teacher) assignment; pass nulls to unassign.</summary>
    Task<int> SetClassTeacherAsync(Guid staffId, string? classId, string? sectionId, CancellationToken ct = default);

    /// <summary>Clears any OTHER staff member currently holding the class-teacher role for this class/section (one head teacher per class).</summary>
    Task<int> ClearClassTeacherForClassAsync(string classId, string sectionId, Guid exceptStaffId, CancellationToken ct = default);

    /// <summary>Atomic salary update -- ExecuteUpdateAsync, never load-then-save.</summary>
    Task<int> SetSalaryAsync(Guid staffId, decimal monthlySalary, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
