using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Student;
using SchoolERP.DataAccess.Student.Entities;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Student.Repositories;

public class StudentDocumentRepository : IStudentDocumentRepository
{
    private readonly StudentDbContext _db;

    public StudentDocumentRepository(StudentDbContext db) => _db = db;

    public Task<StudentDocument?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<IReadOnlyList<StudentDocument>> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default) =>
        _db.Documents.Where(d => d.StudentId == studentId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StudentDocument>)t.Result, ct);

    // Duplicate-prevention guard, mirroring HasStudentReviewed() -- e.g. don't let the
    // same birth certificate get uploaded twice for one student.
    public Task<bool> HasDocumentTypeAsync(Guid studentId, string documentType, CancellationToken ct = default) =>
        _db.Documents.AnyAsync(d => d.StudentId == studentId && d.DocumentType == documentType, ct);

    public async Task AddAsync(StudentDocument document, CancellationToken ct = default) =>
        await _db.Documents.AddAsync(document, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
