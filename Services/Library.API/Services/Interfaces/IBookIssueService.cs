using SchoolERP.Library.DTOs;

namespace SchoolERP.Library.Services.Interfaces;

public interface IBookIssueService
{
    Task<BookIssueSummary> IssueAsync(IssueBookRequest request, CancellationToken ct = default);
    Task<BookIssueSummary> ReturnAsync(ReturnBookRequest request, CancellationToken ct = default);
}
