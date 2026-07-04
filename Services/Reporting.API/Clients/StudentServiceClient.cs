using System.Text.Json;
using SchoolERP.Shared.Common;

namespace SchoolERP.Reporting.Clients;

/// <summary>
/// Typed HttpClient calling Student.API's own endpoint -- never Student.API's database
/// directly. Retry/circuit-breaker are configured on the underlying HttpClient via
/// Microsoft.Extensions.Http.Polly in Program.cs, so this class stays a thin HTTP call.
/// </summary>
public class StudentServiceClient : IStudentServiceClient
{
    private readonly HttpClient _httpClient;

    // ASP.NET Core's default controller JSON output is camelCase; the property names on
    // our own DTOs are PascalCase, so deserialization must be case-insensitive or every
    // field silently comes back null/default.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public StudentServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("/api/students/stats/active-by-class", ct);
        if (!response.IsSuccessStatusCode)
            return new Dictionary<string, int>();

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, int>>>(JsonOptions, ct);
        return envelope?.Data ?? new Dictionary<string, int>();
    }
}
