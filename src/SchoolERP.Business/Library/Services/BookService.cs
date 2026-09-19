using SchoolERP.Business.Library.DTOs;
using SchoolERP.DataAccess.Library.Entities;
using SchoolERP.DataAccess.Library.Repositories.Interfaces;
using SchoolERP.Business.Library.Services.Interfaces;

namespace SchoolERP.Business.Library.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _books;

    public BookService(IBookRepository books) => _books = books;

    public async Task<BookSummary> AddAsync(AddBookRequest request, CancellationToken ct = default)
    {
        if (await _books.ExistsByIsbnAsync(request.Isbn, ct))
            throw new InvalidOperationException($"A book with ISBN '{request.Isbn}' already exists.");

        var book = new Book
        {
            Isbn = request.Isbn,
            Title = request.Title,
            Author = request.Author,
            Category = request.Category,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.TotalCopies
        };

        await _books.AddAsync(book, ct);
        await _books.SaveChangesAsync(ct);

        return ToSummary(book);
    }

    public async Task<IReadOnlyList<BookSummary>> SearchAsync(string? keyword, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var items = await _books.SearchAsync(keyword, category, page, pageSize, ct);
        return items.Select(ToSummary).ToList();
    }

    private static BookSummary ToSummary(Book b) => new(b.Id, b.Isbn, b.Title, b.Author, b.Category, b.TotalCopies, b.AvailableCopies);
}
