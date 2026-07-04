using Microsoft.EntityFrameworkCore;
using SchoolERP.Library.Data;
using SchoolERP.Library.Entities;
using SchoolERP.Library.Repositories.Interfaces;

namespace SchoolERP.Library.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _db;

    public BookRepository(LibraryDbContext db) => _db = db;

    public Task<Book?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Book>> SearchAsync(string? keyword, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(b => EF.Functions.ILike(b.Title, $"%{keyword}%") || EF.Functions.ILike(b.Author, $"%{keyword}%"));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        return await query.OrderBy(b => b.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct = default) =>
        _db.Books.AnyAsync(b => b.Isbn == isbn, ct);

    public async Task AddAsync(Book book, CancellationToken ct = default) =>
        await _db.Books.AddAsync(book, ct);

    public Task<int> DecrementAvailableCopiesAsync(Guid bookId, CancellationToken ct = default) =>
        _db.Books.Where(b => b.Id == bookId && b.AvailableCopies > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.AvailableCopies, b => b.AvailableCopies - 1)
                .SetProperty(b => b.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> IncrementAvailableCopiesAsync(Guid bookId, CancellationToken ct = default) =>
        _db.Books.Where(b => b.Id == bookId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.AvailableCopies, b => b.AvailableCopies + 1)
                .SetProperty(b => b.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
