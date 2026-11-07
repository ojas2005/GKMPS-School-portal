using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolERP.Common;

namespace SchoolERP.Business.Fee.Documents;

/// <summary>Who and what a receipt is for. Names are looked up when the PDF is made.</summary>
public sealed record ReceiptDetails(
    string? StudentName, string? AdmissionNumber, string? ClassName,
    string FeeLabel, string Category, decimal FeeAmount, decimal FeeOutstandingNow);

public class ReceiptDocument : IDocument
{
    private readonly string _receiptNumber;
    private readonly decimal _amount;
    private readonly string _paymentMethod;
    private readonly DateTime _paidAtUtc;
    private readonly Guid _studentId;
    private readonly string _schoolName;
    // Everything the student still owes when the receipt is produced, across every due.
    private readonly decimal _pendingAmount;
    private readonly ReceiptDetails? _details;

    public ReceiptDocument(string receiptNumber, decimal amount, string paymentMethod, DateTime paidAtUtc, Guid studentId, string schoolName, decimal pendingAmount,
        ReceiptDetails? details = null)
    {
        _receiptNumber = receiptNumber;
        _amount = amount;
        _paymentMethod = paymentMethod;
        _paidAtUtc = paidAtUtc;
        _studentId = studentId;
        _schoolName = schoolName;
        _pendingAmount = pendingAmount;
        _details = details;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    // "Rs." rather than the rupee sign: the PDF's built-in font may not have the glyph.
    private static string Inr(decimal amount) => $"Rs. {amount:#,##0.##}";

    public void Compose(IDocumentContainer container)
    {
        var paidLocal = DateTime.SpecifyKind(_paidAtUtc, DateTimeKind.Utc) + SchoolClock.IndiaOffset;
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Column(col =>
            {
                col.Item().AlignCenter().Text(_schoolName).FontSize(18).Bold();
                col.Item().AlignCenter().Text("Fee Payment Receipt").FontSize(13).SemiBold();
                col.Item().PaddingTop(5).LineHorizontal(1);
            });

            page.Content().PaddingVertical(15).Column(col =>
            {
                col.Spacing(6);
                col.Item().Text($"Receipt No: {_receiptNumber}").Bold();
                col.Item().Text($"Date: {paidLocal:d MMMM yyyy, h:mm tt} IST");
                col.Item().PaddingTop(4).LineHorizontal(0.5f);
                if (_details?.StudentName is { } name)
                {
                    col.Item().Text($"Student: {name}");
                    if (_details.AdmissionNumber is { } adm) col.Item().Text($"Admission No: {adm}");
                    if (_details.ClassName is { } cls) col.Item().Text($"Class: {cls}");
                }
                else
                {
                    col.Item().Text($"Student ID: {_studentId}");
                }
                col.Item().PaddingTop(4).LineHorizontal(0.5f);
                if (_details is not null)
                {
                    col.Item().Text($"Towards: {_details.FeeLabel} ({_details.Category})");
                    col.Item().Text($"Fee amount: {Inr(_details.FeeAmount)}");
                }
                col.Item().Text($"Amount Paid: {Inr(_amount)}").Bold();
                col.Item().Text($"Payment Method: {_paymentMethod}");
                if (_details is not null)
                    col.Item().Text($"Still due on this fee: {Inr(_details.FeeOutstandingNow)}");
                col.Item().Text($"Total pending on the account: {Inr(_pendingAmount)}").SemiBold();
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("System-generated receipt. ");
                x.Span($"Generated {DateTime.UtcNow:u}").FontSize(8);
            });
        });
    }
}
