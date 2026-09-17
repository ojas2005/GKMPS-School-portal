using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SchoolERP.Business.Fee.Documents;

public class ReceiptDocument : IDocument
{
    private readonly string _receiptNumber;
    private readonly decimal _amount;
    private readonly string _paymentMethod;
    private readonly DateTime _paidAtUtc;
    private readonly Guid _studentId;
    private readonly string _schoolName;
    private readonly decimal _pendingAmount;

    public ReceiptDocument(string receiptNumber, decimal amount, string paymentMethod, DateTime paidAtUtc, Guid studentId, string schoolName, decimal pendingAmount)
    {
        _receiptNumber = receiptNumber;
        _amount = amount;
        _paymentMethod = paymentMethod;
        _paidAtUtc = paidAtUtc;
        _studentId = studentId;
        _schoolName = schoolName;
        _pendingAmount = pendingAmount;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
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
                col.Spacing(8);
                col.Item().Text($"Receipt No: {_receiptNumber}").Bold();
                col.Item().Text($"Student ID: {_studentId}");
                col.Item().Text($"Amount Paid: {_amount:0.##}");
                col.Item().Text($"Payment Method: {_paymentMethod}");
                col.Item().Text($"Paid On: {_paidAtUtc:d MMMM yyyy HH:mm} UTC");
                col.Item().Text($"Pending Fee: {_pendingAmount:0.##}").SemiBold();
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("System-generated receipt. ");
                x.Span($"Generated {DateTime.UtcNow:u}").FontSize(8);
            });
        });
    }
}
