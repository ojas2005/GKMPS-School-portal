using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Student;
using SchoolERP.DataAccess.Student.Entities;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Student.Repositories;

public class TransferCertificateRepository : ITransferCertificateRepository
{
    private readonly StudentDbContext _db;

    public TransferCertificateRepository(StudentDbContext db) => _db = db;

    public Task<TransferCertificate?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.TransferCertificates.Include(tc => tc.Student).FirstOrDefaultAsync(tc => tc.Id == id, ct);

    public Task<TransferCertificate?> FindByVerificationCodeAsync(string code, CancellationToken ct = default) =>
        _db.TransferCertificates.Include(tc => tc.Student).FirstOrDefaultAsync(tc => tc.VerificationCode == code, ct);

    // Duplicate-prevention guard: a student can't have two open (unapproved) TC requests at once.
    public Task<bool> HasPendingRequestAsync(Guid studentId, CancellationToken ct = default) =>
        _db.TransferCertificates.AnyAsync(tc => tc.StudentId == studentId && !tc.IsApproved, ct);

    public async Task AddAsync(TransferCertificate certificate, CancellationToken ct = default) =>
        await _db.TransferCertificates.AddAsync(certificate, ct);

    public Task<int> MarkSubmittedAsync(Guid id, Guid submittedByUserId, CancellationToken ct = default) =>
        _db.TransferCertificates
            .Where(tc => tc.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(tc => tc.IsSubmitted, true)
                .SetProperty(tc => tc.SubmittedAtUtc, DateTime.UtcNow)
                .SetProperty(tc => tc.SubmittedByUserId, submittedByUserId), ct);

    public Task<int> MarkApprovedAsync(Guid id, Guid approvedByUserId, CancellationToken ct = default) =>
        _db.TransferCertificates
            .Where(tc => tc.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(tc => tc.IsApproved, true)
                .SetProperty(tc => tc.ApprovedAtUtc, DateTime.UtcNow)
                .SetProperty(tc => tc.ApprovedByUserId, approvedByUserId), ct);

    public Task<int> MarkPdfGeneratedAsync(Guid id, string blobPath, CancellationToken ct = default) =>
        _db.TransferCertificates
            .Where(tc => tc.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(tc => tc.IsPdfGenerated, true)
                .SetProperty(tc => tc.BlobPath, blobPath), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
