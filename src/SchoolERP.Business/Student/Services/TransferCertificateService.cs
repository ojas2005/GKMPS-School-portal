using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using SchoolERP.Common.Events;
using SchoolERP.Business.Student.Documents;
using SchoolERP.Business.Student.DTOs;
using SchoolERP.DataAccess.Student.Entities;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.DataAccess.Storage;

namespace SchoolERP.Business.Student.Services;

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
    private readonly IEventPublisher _events;
    private readonly ILogger<TransferCertificateService> _logger;
    private readonly string _schoolName;

    private const string ContainerName = "transfer-certificates";

    public TransferCertificateService(
        ITransferCertificateRepository certificates,
        IStudentRepository students,
        IBlobStorageService blobStorage,
        IEventPublisher events,
        IConfiguration configuration,
        ILogger<TransferCertificateService> logger)
    {
        _certificates = certificates;
        _students = students;
        _blobStorage = blobStorage;
        _events = events;
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
        // An approved transfer certificate means the student has left: they drop out of
        // active counts and class lists, and their data can later be erased.
        await _students.UpdateStatusAsync(certificate.StudentId, SchoolERP.DataAccess.Student.Entities.StudentStatuses.TransferredOut, DateTime.UtcNow, ct);

        _logger.LogInformation("Transfer certificate approved: {CertificateId} by {ApproverId}", certificateId, approvedByUserId);

        var refreshed = await _certificates.FindByIdAsync(certificateId, ct) ?? certificate;
        return ToSummary(refreshed);
    }

    public async Task<string> GenerateAndGetDownloadUrlAsync(Guid certificateId, Guid? requiredStudentId = null, CancellationToken ct = default)
    {
        var certificate = await _certificates.FindByIdAsync(certificateId, ct)
            ?? throw new KeyNotFoundException("Transfer certificate request not found.");

        if (requiredStudentId.HasValue && certificate.StudentId != requiredStudentId.Value)
            throw new UnauthorizedAccessException("You can only download your own transfer certificate.");

        if (!certificate.IsApproved)
            throw new InvalidOperationException("The transfer certificate has not been approved yet.");

        var student = certificate.Student ?? await _students.FindByIdAsync(certificate.StudentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        var alreadyGenerated = certificate.IsPdfGenerated && !string.IsNullOrEmpty(certificate.BlobPath);
        if (!alreadyGenerated || !await _blobStorage.ExistsAsync(ContainerName, certificate.BlobPath!, ct))
        {
            var document = new TransferCertificateDocument(student, certificate, _schoolName);
            var pdfBytes = document.GeneratePdf();

            var blobPath = $"{student.Id}/{certificate.Id}.pdf";
            using var stream = new MemoryStream(pdfBytes);
            await _blobStorage.UploadAsync(ContainerName, blobPath, stream, "application/pdf", ct);

            await _certificates.MarkPdfGeneratedAsync(certificateId, blobPath, ct);
            certificate.BlobPath = blobPath;

            // Announce the certificate once, not again when a missing file is rebuilt.
            if (!alreadyGenerated)
            {
                await _events.PublishAsync(new CertificateGeneratedEvent
                {
                    DocumentType = "TransferCertificate",
                    SubjectId = student.Id,
                    VerificationCode = certificate.VerificationCode
                }, ct);
            }
        }

        // Time-limited signed link -- never a permanent public one.
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
