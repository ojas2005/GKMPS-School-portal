using System.Text.Json;
using SchoolERP.Shared.Common;

namespace SchoolERP.Reporting.Clients;

public class FeeServiceClient : IFeeServiceClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public FeeServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<decimal> GetTotalCollectedAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (fromUtc.HasValue) query.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}");
        if (toUtc.HasValue) query.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}");
        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

        var response = await _httpClient.GetAsync($"/api/fee-payments/collection-totals{queryString}", ct);
        if (!response.IsSuccessStatusCode)
            return 0m;

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CollectionTotalsDto>>(JsonOptions, ct);
        return envelope?.Data?.TotalCollected ?? 0m;
    }

    private record CollectionTotalsDto(DateTime? FromUtc, DateTime? ToUtc, decimal TotalCollected);
}
