using Microsoft.EntityFrameworkCore;
using SchoolERP.Staff.Data;
using SchoolERP.Staff.Entities;
using SchoolERP.Staff.Repositories.Interfaces;

namespace SchoolERP.Staff.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly StaffDbContext _db;

    public StaffRepository(StaffDbContext db) => _db = db;

    public Task<StaffProfile?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<StaffProfile?> FindByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default) =>
        _db.Staff.FirstOrDefaultAsync(s => s.EmployeeCode == employeeCode, ct);

    public async Task<IReadOnlyList<StaffProfile>> SearchAsync(string? designation, string? keyword, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Staff.AsQueryable();
        if (!string.IsNullOrWhiteSpace(designation)) query = query.Where(s => s.Designation == designation);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(s => EF.Functions.Like(s.FullName, $"%{keyword}%") || EF.Functions.Like(s.EmployeeCode, $"%{keyword}%"));

        return await query.OrderBy(s => s.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountAsync(string? designation, CancellationToken ct = default)
    {
        var query = _db.Staff.AsQueryable();
        if (!string.IsNullOrWhiteSpace(designation)) query = query.Where(s => s.Designation == designation);
        return query.CountAsync(ct);
    }

    public Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default) =>
        _db.Staff.AnyAsync(s => s.EmployeeCode == employeeCode, ct);

    public async Task AddAsync(StaffProfile staff, CancellationToken ct = default) =>
        await _db.Staff.AddAsync(staff, ct);

    public Task<int> UpdateStatusAsync(Guid staffId, string status, CancellationToken ct = default) =>
        _db.Staff.Where(s => s.Id == staffId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, status)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<StaffProfile?> FindByLinkedUserAsync(Guid linkedUserId, CancellationToken ct = default) =>
        _db.Staff.FirstOrDefaultAsync(s => s.LinkedUserId == linkedUserId, ct);

    public Task<int> SetClassTeacherAsync(Guid staffId, string? classId, string? sectionId, CancellationToken ct = default) =>
        _db.Staff.Where(s => s.Id == staffId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.ClassTeacherOfClassId, classId)
                .SetProperty(s => s.ClassTeacherOfSectionId, sectionId)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> ClearClassTeacherForClassAsync(string classId, string sectionId, Guid exceptStaffId, CancellationToken ct = default) =>
        _db.Staff.Where(s => s.Id != exceptStaffId
                             && s.ClassTeacherOfClassId == classId
                             && s.ClassTeacherOfSectionId == sectionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.ClassTeacherOfClassId, (string?)null)
                .SetProperty(s => s.ClassTeacherOfSectionId, (string?)null)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SetSalaryAsync(Guid staffId, decimal monthlySalary, CancellationToken ct = default) =>
        _db.Staff.Where(s => s.Id == staffId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.MonthlySalary, monthlySalary)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
