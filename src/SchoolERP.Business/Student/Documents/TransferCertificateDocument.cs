using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolERP.DataAccess.Student.Entities;

namespace SchoolERP.Business.Student.Documents;

/// <summary>
/// QuestPDF document definition for a Transfer Certificate. Rendered to bytes by
/// TransferCertificateService once a request has cleared the two-step approval
/// workflow, then uploaded to Blob Storage and delivered via a SAS URL.
/// </summary>
public class TransferCertificateDocument : IDocument
{
    private readonly StudentProfile _student;
    private readonly TransferCertificate _certificate;
    private readonly string _schoolName;

    public TransferCertificateDocument(StudentProfile student, TransferCertificate certificate, string schoolName)
    {
        _student = student;
        _certificate = certificate;
        _schoolName = schoolName;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Column(col =>
            {
                col.Item().AlignCenter().Text(_schoolName).FontSize(20).Bold();
                col.Item().AlignCenter().Text("Transfer Certificate").FontSize(14).SemiBold();
                col.Item().PaddingTop(5).LineHorizontal(1);
            });

            page.Content().PaddingVertical(20).Column(col =>
            {
                col.Spacing(10);
                col.Item().Text($"Admission Number: {_student.AdmissionNumber}");
                col.Item().Text($"Student Name: {_student.FullName}");
                col.Item().Text($"Date of Birth: {_student.DateOfBirth:d MMMM yyyy}");
                col.Item().Text($"Class / Section at time of leaving: {_student.ClassId} / {_student.SectionId}");
                col.Item().Text($"Reason for leaving: {_certificate.Reason}");
                col.Item().Text($"Date of leaving: {_certificate.RequestedLeavingDateUtc:d MMMM yyyy}");
                col.Item().PaddingTop(15).Text($"Approved on: {_certificate.ApprovedAtUtc:d MMMM yyyy}");
                col.Item().PaddingTop(20).Text($"Verification Code: {_certificate.VerificationCode}").Bold();
                col.Item().Text("Verify this certificate at: /api/transfer-certificates/verify/{code} (no login required).").FontSize(9).Italic();
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("This is a system-generated document. ");
                x.Span($"Generated {DateTime.UtcNow:u}").FontSize(8);
            });
        });
    }
}
