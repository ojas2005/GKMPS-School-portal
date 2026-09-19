using SchoolERP.DataAccess.Library.Entities;

namespace SchoolERP.DataAccess.Library.Repositories.Interfaces;

public interface IBookRepository
{
    Task<Book?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Book>> SearchAsync(string? keyword, string? category, int page, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct = default);
    Task AddAsync(Book book, CancellationToken ct = default);

    /// <summary>Atomic decrement on issue -- never load-then-save.</summary>
    Task<int> DecrementAvailableCopiesAsync(Guid bookId, CancellationToken ct = default);

    /// <summary>Atomic increment on return.</summary>
    Task<int> IncrementAvailableCopiesAsync(Guid bookId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
