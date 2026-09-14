using Microsoft.EntityFrameworkCore;
using SchoolERP.Library.Data;
using SchoolERP.Library.Entities;
using SchoolERP.Library.Repositories.Interfaces;

namespace SchoolERP.Library.Repositories;

public class BookIssueRepository : IBookIssueRepository
{
    private readonly LibraryDbContext _db;

    public BookIssueRepository(LibraryDbContext db) => _db = db;

    public Task<BookIssue?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.BookIssues.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<IReadOnlyList<BookIssue>> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default) =>
        _db.BookIssues.Where(i => i.StudentId == studentId).OrderByDescending(i => i.IssuedAtUtc).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<BookIssue>)t.Result, ct);

    public Task<bool> HasActiveIssueAsync(Guid studentId, Guid bookId, CancellationToken ct = default) =>
        _db.BookIssues.AnyAsync(i => i.StudentId == studentId && i.BookId == bookId && i.ReturnedAtUtc == null, ct);

    public async Task<IReadOnlyList<BookIssue>> ListAsync(bool activeOnly, Guid? studentId, int take, CancellationToken ct = default) =>
        await _db.BookIssues
            .Include(i => i.Book)
            .Where(i => (!activeOnly || i.ReturnedAtUtc == null) && (studentId == null || i.StudentId == studentId))
            .OrderByDescending(i => i.IssuedAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(BookIssue issue, CancellationToken ct = default) =>
        await _db.BookIssues.AddAsync(issue, ct);

    public Task<int> MarkReturnedAsync(Guid issueId, decimal fineAmount, CancellationToken ct = default) =>
        _db.BookIssues.Where(i => i.Id == issueId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.ReturnedAtUtc, DateTime.UtcNow)
                .SetProperty(i => i.FineAmount, fineAmount)
                .SetProperty(i => i.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
