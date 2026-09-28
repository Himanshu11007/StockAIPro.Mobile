using System.Net.Http.Json;
using StockAIPro.Mobile.Models.TopPicks;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

public sealed class TopPicksApiClient : ITopPicksApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TopPicksApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<StartScanResult> StartScanAsync(string category, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/top-picks/start",
                new StartScanRequest { Category = category }, ct),
            ct);

        return await BusinessApiSend.ReadDataOrThrowAsync<StartScanResult>(response, ct);
    }

    public async Task<ScanStatus> GetStatusAsync(string scanId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/top-picks/status/{Uri.EscapeDataString(scanId)}", ct), ct);

        return await BusinessApiSend.ReadDataOrThrowAsync<ScanStatus>(response, ct);
    }

    public async Task<ScanResult> GetResultAsync(string scanId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/top-picks/result/{Uri.EscapeDataString(scanId)}", ct), ct);

        return await BusinessApiSend.ReadDataOrThrowAsync<ScanResult>(response, ct);
    }
}
