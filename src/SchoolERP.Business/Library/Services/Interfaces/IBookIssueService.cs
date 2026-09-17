using SchoolERP.Business.Library.DTOs;

namespace SchoolERP.Business.Library.Services.Interfaces;

public interface IBookIssueService
{
    Task<BookIssueSummary> IssueAsync(IssueBookRequest request, CancellationToken ct = default);
    Task<BookIssueSummary> ReturnAsync(ReturnBookRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BookIssueDetail>> ListAsync(bool activeOnly, Guid? studentId, CancellationToken ct = default);
}
