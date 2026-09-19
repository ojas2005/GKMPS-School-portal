using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SchoolERP.Business.Examination.Documents;

public class ReportCardDocument : IDocument
{
    private readonly Guid _studentId;
    private readonly string _examName;
    private readonly IReadOnlyList<(string Subject, decimal Marks, int MaxMarks, string? Grade)> _rows;
    private readonly string _schoolName;

    public ReportCardDocument(Guid studentId, string examName, IReadOnlyList<(string, decimal, int, string?)> rows, string schoolName)
    {
        _studentId = studentId;
        _examName = examName;
        _rows = rows;
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
                col.Item().AlignCenter().Text($"Report Card -- {_examName}").FontSize(14).SemiBold();
                col.Item().PaddingTop(5).LineHorizontal(1);
            });

            page.Content().PaddingVertical(20).Column(col =>
            {
                col.Item().Text($"Student ID: {_studentId}");
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Subject").Bold();
                        header.Cell().Text("Marks").Bold();
                        header.Cell().Text("Max").Bold();
                        header.Cell().Text("Grade").Bold();
                    });

                    foreach (var row in _rows)
                    {
                        table.Cell().Text(row.Subject);
                        table.Cell().Text(row.Marks.ToString("0.##"));
                        table.Cell().Text(row.MaxMarks.ToString());
                        table.Cell().Text(row.Grade ?? "-");
                    }
                });
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("System-generated document. ");
                x.Span($"Generated {DateTime.UtcNow:u}").FontSize(8);
            });
        });
    }
}
