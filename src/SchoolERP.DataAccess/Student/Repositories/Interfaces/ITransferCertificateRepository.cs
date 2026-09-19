using SchoolERP.DataAccess.Student.Entities;

namespace SchoolERP.DataAccess.Student.Repositories.Interfaces;

public interface ITransferCertificateRepository
{
    Task<TransferCertificate?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<TransferCertificate?> FindByVerificationCodeAsync(string code, CancellationToken ct = default);
    Task<bool> HasPendingRequestAsync(Guid studentId, CancellationToken ct = default);

    Task AddAsync(TransferCertificate certificate, CancellationToken ct = default);

    /// <summary>Step 1 of the two-step workflow: Admin/Teacher submits the request.</summary>
    Task<int> MarkSubmittedAsync(Guid id, Guid submittedByUserId, CancellationToken ct = default);

    /// <summary>Step 2: Principal/Admin approves; only then can the PDF be generated.</summary>
    Task<int> MarkApprovedAsync(Guid id, Guid approvedByUserId, CancellationToken ct = default);

    Task<int> MarkPdfGeneratedAsync(Guid id, string blobPath, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
