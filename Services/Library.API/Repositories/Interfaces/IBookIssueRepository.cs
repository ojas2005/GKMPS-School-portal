using SchoolERP.Library.Entities;

namespace SchoolERP.Library.Repositories.Interfaces;

public interface IBookIssueRepository
{
    Task<BookIssue?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BookIssue>> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Duplicate-prevention guard: this student already has this exact book issued and not yet returned.</summary>
    Task<bool> HasActiveIssueAsync(Guid studentId, Guid bookId, CancellationToken ct = default);

    /// <summary>Most recent issues first (with Book loaded); optionally only not-yet-returned ones and/or one student's.</summary>
    Task<IReadOnlyList<BookIssue>> ListAsync(bool activeOnly, Guid? studentId, int take, CancellationToken ct = default);

    Task AddAsync(BookIssue issue, CancellationToken ct = default);

    /// <summary>Atomic return + fine recording in one round trip.</summary>
    Task<int> MarkReturnedAsync(Guid issueId, decimal fineAmount, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
