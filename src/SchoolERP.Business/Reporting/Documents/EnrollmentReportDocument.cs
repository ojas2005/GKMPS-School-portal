using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SchoolERP.Business.Reporting.Documents;

public class EnrollmentReportDocument : IDocument
{
    private readonly IReadOnlyDictionary<string, int> _countByClass;
    private readonly string _schoolName;

    public EnrollmentReportDocument(IReadOnlyDictionary<string, int> countByClass, string schoolName)
    {
        _countByClass = countByClass;
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
                col.Item().AlignCenter().Text("Enrollment Report -- Active Students by Class").FontSize(13).SemiBold();
                col.Item().PaddingTop(5).LineHorizontal(1);
            });

            page.Content().PaddingVertical(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Class").Bold();
                    header.Cell().Text("Active Students").Bold();
                });

                foreach (var (classId, count) in _countByClass)
                {
                    table.Cell().Text(classId);
                    table.Cell().Text(count.ToString());
                }
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("System-generated report. ");
                x.Span($"Generated {DateTime.UtcNow:u}").FontSize(8);
            });
        });
    }
}
