using System.Security.Cryptography;
using MassTransit;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using SchoolERP.Shared.Events;
using SchoolERP.Student.Documents;
using SchoolERP.Student.DTOs;
using SchoolERP.Student.Entities;
using SchoolERP.Student.Repositories.Interfaces;
using SchoolERP.Student.Services.Interfaces;
using SchoolERP.Student.Storage;

namespace SchoolERP.Student.Services;

/// <summary>
/// Implements the two-step submit/approve workflow for Transfer Certificates, then
/// QuestPDF rendering, Blob upload, and SAS-URL delivery -- the standard document
/// chain used for every generated certificate/receipt/ID card in this system.
/// </summary>
public class TransferCertificateService : ITransferCertificateService
{
    private readonly ITransferCertificateRepository _certificates;
    private readonly IStudentRepository _students;
    private readonly IBlobStorageService _blobStorage;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TransferCertificateService> _logger;
    private readonly string _schoolName;

    private const string ContainerName = "transfer-certificates";

    public TransferCertificateService(
        ITransferCertificateRepository certificates,
        IStudentRepository students,
        IBlobStorageService blobStorage,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ILogger<TransferCertificateService> logger)
    {
        _certificates = certificates;
        _students = students;
        _blobStorage = blobStorage;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _schoolName = configuration["School:Name"] ?? "School";
    }

    public async Task<TransferCertificateSummary> RequestAsync(Guid studentId, RequestTransferCertificateRequest request, Guid submittedByUserId, CancellationToken ct = default)
    {
        var student = await _students.FindByIdAsync(studentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        // Duplicate-prevention guard: no second open request while one is pending approval.
        if (await _certificates.HasPendingRequestAsync(studentId, ct))
            throw new InvalidOperationException("A transfer certificate request is already pending for this student.");

        var certificate = new TransferCertificate
        {
            StudentId = studentId,
            Reason = request.Reason,
            RequestedLeavingDateUtc = request.RequestedLeavingDateUtc,
            VerificationCode = GenerateVerificationCode()
        };

        await _certificates.AddAsync(certificate, ct);
        await _certificates.SaveChangesAsync(ct);

        // Step 1 of the two-step workflow.
        await _certificates.MarkSubmittedAsync(certificate.Id, submittedByUserId, ct);
        certificate.IsSubmitted = true;
        certificate.SubmittedAtUtc = DateTime.UtcNow;
        certificate.SubmittedByUserId = submittedByUserId;

        _logger.LogInformation("Transfer certificate requested: {CertificateId} for student {StudentId}", certificate.Id, studentId);

        return ToSummary(certificate);
    }

    public async Task<TransferCertificateSummary> ApproveAsync(Guid certificateId, Guid approvedByUserId, CancellationToken ct = default)
    {
        var certificate = await _certificates.FindByIdAsync(certificateId, ct)
            ?? throw new KeyNotFoundException("Transfer certificate request not found.");

        if (!certificate.IsSubmitted)
            throw new InvalidOperationException("This request has not been submitted yet.");

        if (certificate.IsApproved)
            throw new InvalidOperationException("This request has already been approved.");

        // Step 2 of the two-step workflow -- only Principal/Admin reaches this via [Authorize(Roles=...)] on the controller.
        await _certificates.MarkApprovedAsync(certificateId, approvedByUserId, ct);

        _logger.LogInformation("Transfer certificate approved: {CertificateId} by {ApproverId}", certificateId, approvedByUserId);

        var refreshed = await _certificates.FindByIdAsync(certificateId, ct) ?? certificate;
        return ToSummary(refreshed);
    }

    public async Task<string> GenerateAndGetDownloadUrlAsync(Guid certificateId, CancellationToken ct = default)
    {
        var certificate = await _certificates.FindByIdAsync(certificateId, ct)
            ?? throw new KeyNotFoundException("Transfer certificate request not found.");

        if (!certificate.IsApproved)
            throw new InvalidOperationException("The transfer certificate has not been approved yet.");

        var student = certificate.Student ?? await _students.FindByIdAsync(certificate.StudentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        if (!certificate.IsPdfGenerated || string.IsNullOrEmpty(certificate.BlobPath))
        {
            var document = new TransferCertificateDocument(student, certificate, _schoolName);
            var pdfBytes = document.GeneratePdf();

            var blobPath = $"{student.Id}/{certificate.Id}.pdf";
            using var stream = new MemoryStream(pdfBytes);
            await _blobStorage.UploadAsync(ContainerName, blobPath, stream, "application/pdf", ct);

            await _certificates.MarkPdfGeneratedAsync(certificateId, blobPath, ct);
            certificate.BlobPath = blobPath;

            await _publishEndpoint.Publish(new CertificateGeneratedEvent
            {
                DocumentType = "TransferCertificate",
                SubjectId = student.Id,
                VerificationCode = certificate.VerificationCode
            }, ct);
        }

        // Time-limited SAS URL -- never a permanent public link.
        return await _blobStorage.GetSasUrlAsync(ContainerName, certificate.BlobPath!, TimeSpan.FromMinutes(15), ct);
    }

    public async Task<TransferCertificateVerification> VerifyAsync(string verificationCode, CancellationToken ct = default)
    {
        var certificate = await _certificates.FindByVerificationCodeAsync(verificationCode, ct);

        if (certificate is null || !certificate.IsApproved || certificate.Student is null)
            return new TransferCertificateVerification(false, null, null, null);

        return new TransferCertificateVerification(
            true,
            certificate.Student.FullName,
            certificate.Student.AdmissionNumber,
            certificate.ApprovedAtUtc);
    }

    private static string GenerateVerificationCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(9);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").ToUpperInvariant();
    }

    private static TransferCertificateSummary ToSummary(TransferCertificate tc) =>
        new(tc.Id, tc.StudentId, tc.Reason, tc.RequestedLeavingDateUtc, tc.IsSubmitted, tc.IsApproved, tc.IsPdfGenerated, tc.VerificationCode);
}
