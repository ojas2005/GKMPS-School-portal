using SchoolERP.Student.DTOs;

namespace SchoolERP.Student.Services.Interfaces;

public interface ITransferCertificateService
{
    Task<TransferCertificateSummary> RequestAsync(Guid studentId, RequestTransferCertificateRequest request, Guid submittedByUserId, CancellationToken ct = default);
    Task<TransferCertificateSummary> ApproveAsync(Guid certificateId, Guid approvedByUserId, CancellationToken ct = default);

    /// <summary>Generates the PDF (only once approved), uploads to Blob Storage, returns a SAS URL.</summary>
    Task<string> GenerateAndGetDownloadUrlAsync(Guid certificateId, CancellationToken ct = default);

    /// <summary>Public, unauthenticated verification by code -- no PII beyond name/admission number returned.</summary>
    Task<TransferCertificateVerification> VerifyAsync(string verificationCode, CancellationToken ct = default);
}
