using Microsoft.EntityFrameworkCore;
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

    public Task<StudentProfile?> FindByParentUserAsync(Guid parentUserId, CancellationToken ct = default) =>
        _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.ParentUserId == parentUserId, ct);

    public Task<StudentProfile?> FindByAdmissionNumberAsync(string admissionNumber, CancellationToken ct = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.AdmissionNumber == admissionNumber, ct);

    public async Task<IReadOnlyList<StudentProfile>> SearchStudentsAsync(string? classId, string? sectionId, string? keyword, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Students.AsQueryable();

        if (!string.IsNullOrWhiteSpace(classId))
            query = query.Where(s => s.ClassId == classId);

        if (!string.IsNullOrWhiteSpace(sectionId))
            query = query.Where(s => s.SectionId == sectionId);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(s =>
                EF.Functions.Like(s.FullName, $"%{keyword}%") ||
                EF.Functions.Like(s.AdmissionNumber, $"%{keyword}%"));

        return await query
            .OrderBy(s => s.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountStudentsAsync(string? classId, string? sectionId, CancellationToken ct = default)
    {
        var query = _db.Students.AsQueryable();
        if (!string.IsNullOrWhiteSpace(classId)) query = query.Where(s => s.ClassId == classId);
        if (!string.IsNullOrWhiteSpace(sectionId)) query = query.Where(s => s.SectionId == sectionId);
        return query.CountAsync(ct);
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
            .Where(s => s.Status == "Active")
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
