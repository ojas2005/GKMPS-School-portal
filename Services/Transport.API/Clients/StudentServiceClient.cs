using System.Text.Json;
using SchoolERP.Shared.Common;

namespace SchoolERP.Transport.Clients;

/// <summary>
/// Typed HttpClient calling Student.API's own endpoint -- never Student.API's database
/// directly. Retry/circuit-breaker are configured on the underlying HttpClient via
/// Microsoft.Extensions.Http.Polly in Program.cs, so this class stays a thin HTTP call.
/// </summary>
public class StudentServiceClient : IStudentServiceClient
{
    private readonly HttpClient _httpClient;

    // ASP.NET Core's default controller JSON output is camelCase; our own DTOs are
    // PascalCase, so deserialization must be case-insensitive or every field silently
    // comes back null/default.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public StudentServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<StudentLookupResult?> GetByIdAsync(Guid studentId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/students/{studentId}", ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<StudentSummaryDto>>(JsonOptions, ct);
        var student = envelope?.Data;
        return student is null ? null : new StudentLookupResult(student.Id, student.FullName, student.AdmissionNumber);
    }

    // Only the fields this client needs, not the full Student.API shape.
    private record StudentSummaryDto(Guid Id, string FullName, string AdmissionNumber);
}
