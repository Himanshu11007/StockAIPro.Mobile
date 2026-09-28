using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Analysis;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

public sealed class AnalysisApiClient : IAnalysisApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AnalysisApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AnalyzeStockResult> AnalyzeAsync(string symbol, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.PostAsJsonAsync(
                $"{ApiConfiguration.ApiPrefix}/analyze-stock",
                new AnalyzeStockRequest { Symbol = symbol }, ct),
            ct);

        return await BusinessApiSend.ReadDataOrThrowAsync<AnalyzeStockResult>(response, ct);
    }
}
