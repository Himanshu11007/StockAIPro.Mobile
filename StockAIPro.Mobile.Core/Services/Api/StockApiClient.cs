using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Stocks;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

public sealed class StockApiClient : IStockApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public StockApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<StockSearchResult>> SearchAsync(
        string? search = null, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        var query = $"limit={limit}&offset={offset}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/stocks?{query}", ct), ct);

        // GET /stocks returns the list directly (no success envelope) - see
        // api/routes/stocks.py, which uses `response_model=list[...]` rather
        // than success_envelope(...) like the other business routes.
        return await BusinessApiSend.ReadRawOrThrowAsync<List<StockSearchResult>>(response, ct);
    }

    public async Task<StockSearchResult?> GetAsync(string symbol, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/stocks/{Uri.EscapeDataString(symbol)}", ct), ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        return await BusinessApiSend.ReadRawOrThrowAsync<StockSearchResult>(response, ct);
    }
}
