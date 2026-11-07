using SchoolERP.Business.Fee.DTOs;

namespace SchoolERP.Business.Fee.Services.Interfaces;

/// <summary>
/// Students' fee accounts and the fees generated on them: opened at admission, billed every
/// month and for each exam by the billing job, closed when the student leaves.
/// </summary>
public interface IFeeAccountService
{
    /// <summary>What admitting into this class (with this route) from this date would charge. Changes nothing.</summary>
    Task<FeeBreakdown> PreviewAsync(string classId, Guid? routeId, DateTime? startDate, decimal? previousDues, CancellationToken ct = default);

    /// <summary>Opens the account and creates its first fees in one database transaction. Throws if it already exists.</summary>
    Task<FeeBreakdown> OpenAccountAsync(OpenFeeAccountCommand command, CancellationToken ct = default);

    /// <summary>Removes an account and its fees again, for undoing an admission that didn't complete. Refuses if anything was paid.</summary>
    Task DiscardAccountAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Opens an account for a student admitted before automatic billing (their class and route as they are now).</summary>
    Task<FeeBreakdown> OpenForExistingStudentAsync(Guid studentId, OpenFeeAccountRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default);

    /// <summary>Opens accounts for every current student who has none.</summary>
    Task<OpenAccountsResult> OpenForAllCurrentStudentsAsync(OpenFeeAccountRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default);

    /// <summary>Stops billing (the student left). No-op if there's no open account.</summary>
    Task CloseAsync(Guid studentId, string reason, CancellationToken ct = default);

    /// <summary>Resumes billing from today (the student came back). No-op if there's no closed account.</summary>
    Task ReopenAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>After a class change: re-prices the student's fees not yet due and untouched to the new class's rates.</summary>
    Task<int> RepriceUpcomingAsync(Guid studentId, string newClassId, CancellationToken ct = default);

    /// <summary>Writes a route change (or transport starting/stopping) into the fee history. No-op without an account.</summary>
    Task RecordTransportChangeAsync(Guid studentId, string? oldRoute, string? newRoute, decimal newMonthlyFee, CancellationToken ct = default);

    /// <summary>Brings accounts up to date: creates every monthly and exam fee that should exist by today. Safe to run any time.</summary>
    Task<FeeGenerationResult> GenerateDueFeesAsync(Guid? studentId = null, CancellationToken ct = default);
}
