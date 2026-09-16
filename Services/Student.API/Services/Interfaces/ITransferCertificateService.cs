using SchoolERP.Student.DTOs;

namespace SchoolERP.Student.Services.Interfaces;

public interface ITransferCertificateService
{
    Task<TransferCertificateSummary> RequestAsync(Guid studentId, RequestTransferCertificateRequest request, Guid submittedByUserId, CancellationToken ct = default);
    Task<TransferCertificateSummary> ApproveAsync(Guid certificateId, Guid approvedByUserId, CancellationToken ct = default);

    /// <summary>Generates the PDF (only once approved), uploads to Blob Storage, returns a SAS URL.</summary>
    /// <param name="requiredStudentId">When set (student/parent callers), the certificate must
    /// belong to this student or an UnauthorizedAccessException is thrown.</param>
    Task<string> GenerateAndGetDownloadUrlAsync(Guid certificateId, Guid? requiredStudentId = null, CancellationToken ct = default);

    /// <summary>Public, unauthenticated verification by code -- no PII beyond name/admission number returned.</summary>
    Task<TransferCertificateVerification> VerifyAsync(string verificationCode, CancellationToken ct = default);
}
