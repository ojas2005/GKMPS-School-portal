using Microsoft.EntityFrameworkCore;
using SchoolERP.Common;
using SchoolERP.DataAccess.Student;
using SchoolERP.DataAccess.Student.Entities;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Student.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly StudentDbContext _db;

    public StudentRepository(StudentDbContext db) => _db = db;

    public Task<StudentProfile?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<StudentProfile?> FindByLinkedUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.LinkedUserId == userId, ct);

    // A current child first, should the login also be linked to one who has left.
    public Task<StudentProfile?> FindByParentUserAsync(Guid parentUserId, CancellationToken ct = default) =>
        _db.Students.AsNoTracking()
            .Where(s => s.ParentUserId == parentUserId)
            .OrderBy(s => s.Status == StudentStatuses.Active ? 0 : 1)
            .FirstOrDefaultAsync(ct);

    public Task<StudentProfile?> FindByAdmissionNumberAsync(string admissionNumber, CancellationToken ct = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.AdmissionNumber == admissionNumber, ct);

    public async Task<IReadOnlyList<StudentProfile>> SearchStudentsAsync(string? classId, string? sectionId, string? keyword, RosterFilter roster, int page, int pageSize, CancellationToken ct = default) =>
        await Filtered(classId, sectionId, keyword, roster)
            .OrderBy(s => s.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountStudentsAsync(string? classId, string? sectionId, string? keyword, RosterFilter roster, CancellationToken ct = default) =>
        Filtered(classId, sectionId, keyword, roster).CountAsync(ct);

    private IQueryable<StudentProfile> Filtered(string? classId, string? sectionId, string? keyword, RosterFilter roster)
    {
        var query = _db.Students.AsQueryable();
        query = roster switch
        {
            RosterFilter.Current => query.Where(s => s.Status == StudentStatuses.Active),
            RosterFilter.Removed => query.Where(s => s.Status != StudentStatuses.Active),
            _ => query
        };
        if (!string.IsNullOrWhiteSpace(classId)) query = query.Where(s => s.ClassId == classId);
        if (!string.IsNullOrWhiteSpace(sectionId)) query = query.Where(s => s.SectionId == sectionId);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(s =>
                EF.Functions.Like(s.FullName, $"%{keyword}%") ||
                EF.Functions.Like(s.AdmissionNumber, $"%{keyword}%"));
        return query;
    }

    public Task<bool> ExistsByAdmissionNumberAsync(string admissionNumber, CancellationToken ct = default) =>
        _db.Students.AnyAsync(s => s.AdmissionNumber == admissionNumber, ct);

    public async Task AddAsync(StudentProfile student, CancellationToken ct = default) =>
        await _db.Students.AddAsync(student, ct);

    public Task<int> ReassignClassSectionAsync(Guid studentId, string classId, string sectionId, CancellationToken ct = default) =>
        _db.Students
            .Where(s => s.Id == studentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.ClassId, classId)
                .SetProperty(s => s.SectionId, sectionId)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> AnonymizeAsync(Guid studentId, CancellationToken ct = default) =>
        _db.Students
            .Where(s => s.Id == studentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.FullName, s => "Former student " + s.AdmissionNumber)
                .SetProperty(s => s.DateOfBirth, new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .SetProperty(s => s.Gender, "Not recorded")
                .SetProperty(s => s.ParentName, (string?)null)
                .SetProperty(s => s.ParentEmail, (string?)null)
                .SetProperty(s => s.ParentPhone, (string?)null)
                .SetProperty(s => s.FatherName, (string?)null)
                .SetProperty(s => s.FatherPhone, (string?)null)
                .SetProperty(s => s.FatherEmail, (string?)null)
                .SetProperty(s => s.MotherName, (string?)null)
                .SetProperty(s => s.MotherPhone, (string?)null)
                .SetProperty(s => s.MotherEmail, (string?)null)
                .SetProperty(s => s.RemovalReason, (string?)null)
                .SetProperty(s => s.Address, (string?)null)
                .SetProperty(s => s.ParentUserId, (Guid?)null)
                .SetProperty(s => s.Status, StudentStatuses.Erased)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> CountOtherChildrenOfParentAsync(Guid parentUserId, Guid exceptStudentId, CancellationToken ct = default) =>
        _db.Students.CountAsync(s => s.ParentUserId == parentUserId && s.Id != exceptStudentId && s.Status != StudentStatuses.Erased, ct);

    public Task<int> CountOtherCurrentChildrenOfParentAsync(Guid parentUserId, Guid exceptStudentId, CancellationToken ct = default) =>
        _db.Students.CountAsync(s => s.ParentUserId == parentUserId && s.Id != exceptStudentId && s.Status == StudentStatuses.Active, ct);

    public Task<int> SetRemovedAsync(Guid studentId, bool removed, string? reason, CancellationToken ct = default) =>
        _db.Students
            .Where(s => s.Id == studentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, removed ? StudentStatuses.Removed : StudentStatuses.Active)
                .SetProperty(s => s.RemovedAtUtc, removed ? DateTime.UtcNow : null)
                .SetProperty(s => s.RemovalReason, removed ? reason : null)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> UpdateStatusAsync(Guid studentId, string status, DateTime? transferredOutAtUtc, CancellationToken ct = default) =>
        _db.Students
            .Where(s => s.Id == studentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, status)
                .SetProperty(s => s.TransferredOutAtUtc, transferredOutAtUtc)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public async Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default)
    {
        // Aggregate query via GroupBy/Count executed in PostgreSQL -- rows never materialize in app memory.
        var grouped = await _db.Students
            .Where(s => s.Status == StudentStatuses.Active)
            .GroupBy(s => s.ClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped.ToDictionary(g => g.ClassId, g => g.Count);
    }

    public Task<int> SetParentUserIdAsync(Guid studentId, Guid? parentUserId, CancellationToken ct = default) =>
        _db.Students
            .Where(s => s.Id == studentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.ParentUserId, parentUserId)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
