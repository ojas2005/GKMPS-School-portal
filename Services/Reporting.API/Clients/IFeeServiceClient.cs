namespace SchoolERP.Reporting.Clients;

public interface IFeeServiceClient
{
    Task<decimal> GetTotalCollectedAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
}
