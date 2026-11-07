using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Common;

namespace SchoolERP.Business.Fee.Services.Interfaces;

/// <summary>Read side of fees: the dashboard, lists, reports and statements.</summary>
public interface IFeeLedgerService
{
    Task<PagedResult<FeeEntryDto>> SearchEntriesAsync(FeeEntryQuery query, CancellationToken ct = default);
    Task<PagedResult<StudentFeeBalanceDto>> StudentBalancesAsync(FeeEntryQuery query, CancellationToken ct = default);
    Task<FeeOverviewDto> OverviewAsync(string? academicYear, CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyCollectionDto>> MonthlyCollectionsAsync(string? academicYear, CancellationToken ct = default);
    Task<FeeStatementDto?> StatementAsync(Guid studentId, CancellationToken ct = default);
}
