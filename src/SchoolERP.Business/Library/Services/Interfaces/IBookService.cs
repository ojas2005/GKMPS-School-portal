using SchoolERP.Business.Library.DTOs;

namespace SchoolERP.Business.Library.Services.Interfaces;

public interface IBookService
{
    Task<BookSummary> AddAsync(AddBookRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BookSummary>> SearchAsync(string? keyword, string? category, int page, int pageSize, CancellationToken ct = default);
}
