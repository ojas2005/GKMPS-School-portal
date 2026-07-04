using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Library.DTOs;

public record AddBookRequest([Required] string Isbn, [Required] string Title, [Required] string Author, [Required] string Category, [Required] int TotalCopies);
public record BookSummary(Guid Id, string Isbn, string Title, string Author, string Category, int TotalCopies, int AvailableCopies);

public record IssueBookRequest([Required] Guid BookId, [Required] Guid StudentId, [Required] DateTime DueDateUtc);
public record ReturnBookRequest([Required] Guid IssueId);
public record BookIssueSummary(Guid Id, Guid BookId, Guid StudentId, DateTime IssuedAtUtc, DateTime DueDateUtc, DateTime? ReturnedAtUtc, decimal FineAmount);
