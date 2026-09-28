using StockAIPro.Mobile.Models.Performance;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

public sealed class PerformanceApiClient : IPerformanceApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PerformanceApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PerformanceSummary> GetSummaryAsync(CancellationToken ct = default) =>
        await GetAsync<PerformanceSummary>("/performance/summary", ct);

    public async Task<List<SignalPerformance>> GetBySignalAsync(CancellationToken ct = default) =>
        await GetAsync<List<SignalPerformance>>("/performance/by-signal", ct);

    public async Task<List<ConfidencePerformance>> GetByConfidenceAsync(CancellationToken ct = default) =>
        await GetAsync<List<ConfidencePerformance>>("/performance/by-confidence", ct);

    public async Task<List<ConfluencePerformance>> GetByConfluenceAsync(CancellationToken ct = default) =>
        await GetAsync<List<ConfluencePerformance>>("/performance/by-confluence", ct);

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync($"{ApiConfiguration.ApiPrefix}{path}", ct), ct);

        return await BusinessApiSend.ReadDataOrThrowAsync<T>(response, ct);
    }
}
