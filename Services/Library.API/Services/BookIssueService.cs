using SchoolERP.Library.DTOs;
using SchoolERP.Library.Entities;
using SchoolERP.Library.Repositories.Interfaces;
using SchoolERP.Library.Services.Interfaces;

namespace SchoolERP.Library.Services;

/// <summary>
/// Issuing/returning a book keeps Book.AvailableCopies correct via atomic
/// increment/decrement (ExecuteUpdateAsync) rather than loading the book, adjusting
/// the count in C#, and saving the whole row back -- avoids lost updates under
/// concurrent issue/return requests.
/// </summary>
public class BookIssueService : IBookIssueService
{
    private readonly IBookIssueRepository _issues;
    private readonly IBookRepository _books;
    private readonly ILogger<BookIssueService> _logger;

    private const decimal FinePerDayLate = 5m;

    public BookIssueService(IBookIssueRepository issues, IBookRepository books, ILogger<BookIssueService> logger)
    {
        _issues = issues;
        _books = books;
        _logger = logger;
    }

    public async Task<BookIssueSummary> IssueAsync(IssueBookRequest request, CancellationToken ct = default)
    {
        var book = await _books.FindByIdAsync(request.BookId, ct) ?? throw new KeyNotFoundException("Book not found.");

        if (book.AvailableCopies <= 0)
            throw new InvalidOperationException("No copies of this book are currently available.");

        // Duplicate-prevention guard: no double-issue of the same book to the same student.
        if (await _issues.HasActiveIssueAsync(request.StudentId, request.BookId, ct))
            throw new InvalidOperationException("This student already has this book issued and not yet returned.");

        var decremented = await _books.DecrementAvailableCopiesAsync(request.BookId, ct);
        if (decremented == 0)
            throw new InvalidOperationException("No copies of this book are currently available.");

        var issue = new BookIssue
        {
            BookId = request.BookId,
            StudentId = request.StudentId,
            DueDateUtc = request.DueDateUtc
        };

        await _issues.AddAsync(issue, ct);
        await _issues.SaveChangesAsync(ct);

        _logger.LogInformation("Book issued: {BookId} to student {StudentId}", request.BookId, request.StudentId);

        return ToSummary(issue);
    }

    public async Task<BookIssueSummary> ReturnAsync(ReturnBookRequest request, CancellationToken ct = default)
    {
        var issue = await _issues.FindByIdAsync(request.IssueId, ct) ?? throw new KeyNotFoundException("Issue record not found.");

        if (issue.IsReturned)
            throw new InvalidOperationException("This book has already been returned.");

        var daysLate = (DateTime.UtcNow.Date - issue.DueDateUtc.Date).Days;
        var fine = daysLate > 0 ? daysLate * FinePerDayLate : 0m;

        await _issues.MarkReturnedAsync(issue.Id, fine, ct);
        await _books.IncrementAvailableCopiesAsync(issue.BookId, ct);

        issue.ReturnedAtUtc = DateTime.UtcNow;
        issue.FineAmount = fine;

        return ToSummary(issue);
    }

    public async Task<IReadOnlyList<BookIssueDetail>> ListAsync(bool activeOnly, Guid? studentId, CancellationToken ct = default)
    {
        var issues = await _issues.ListAsync(activeOnly, studentId, take: 200, ct);
        var now = DateTime.UtcNow;
        return issues.Select(i => new BookIssueDetail(
            i.Id, i.BookId, i.Book?.Title ?? "(removed book)", i.StudentId, i.IssuedAtUtc, i.DueDateUtc,
            i.ReturnedAtUtc, i.FineAmount, i.ReturnedAtUtc == null && i.DueDateUtc < now)).ToList();
    }

    private static BookIssueSummary ToSummary(BookIssue i) =>
        new(i.Id, i.BookId, i.StudentId, i.IssuedAtUtc, i.DueDateUtc, i.ReturnedAtUtc, i.FineAmount);
}
