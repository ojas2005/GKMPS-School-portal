namespace SchoolERP.Reporting.Clients;

public interface IStudentServiceClient
{
    Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default);
}
