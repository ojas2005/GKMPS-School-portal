using SchoolERP.Library.DTOs;

namespace SchoolERP.Library.Services.Interfaces;

public interface IBookService
{
    Task<BookSummary> AddAsync(AddBookRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BookSummary>> SearchAsync(string? keyword, string? category, int page, int pageSize, CancellationToken ct = default);
}
