using StockAIPro.Mobile.Models.Product;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// The backend's product API: Top Investment Candidates, per-stock
/// investment analysis (StockAI Score + FQVF), market regime and the
/// backend-controlled app configuration. Thin by design: every value is
/// computed by the backend and displayed as delivered.
/// </summary>
public interface IProductApiClient
{
    /// <summary>Public endpoint (no token needed).</summary>
    Task<AppConfig> GetAppConfigAsync(CancellationToken ct = default);

    /// <summary>Empty Items (not an error) when no engine run has completed.</summary>
    Task<TopCandidatesResponse> GetTopCandidatesAsync(int? limit = null, CancellationToken ct = default);

    /// <summary>Throws ApiException Kind NotFound when the stock is unknown,
    /// inactive, or has not been analysed yet (Message says which).</summary>
    Task<StockAnalysis> GetStockAnalysisAsync(string symbol, CancellationToken ct = default);

    /// <summary>Synchronous on-demand analysis (10-30 s). Throws Conflict
    /// when the engine is busy.</summary>
    Task<StockAnalysis> RefreshStockAnalysisAsync(string symbol, CancellationToken ct = default);

    /// <summary>Null when the backend has not computed a regime yet.</summary>
    Task<MarketRegimeInfo?> GetMarketRegimeAsync(CancellationToken ct = default);
}

public sealed class ProductApiClient : IProductApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private static string Path(string relative) => $"{ApiConfiguration.ApiPrefix}{relative}";

    private static string Sym(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("A stock symbol is required.", nameof(symbol));
        return Uri.EscapeDataString(symbol.Trim().ToUpperInvariant());
    }

    public async Task<AppConfig> GetAppConfigAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.RawClientName);
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(Path("/app/config"), ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<AppConfig>(response, ct);
    }

    public async Task<TopCandidatesResponse> GetTopCandidatesAsync(int? limit = null, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        var url = limit is { } l ? Path($"/top-picks?limit={l}") : Path("/top-picks");
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(url, ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<TopCandidatesResponse>(response, ct);
    }

    public async Task<StockAnalysis> GetStockAnalysisAsync(string symbol, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync(Path($"/stocks/{Sym(symbol)}/analysis"), ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<StockAnalysis>(response, ct);
    }

    public async Task<StockAnalysis> RefreshStockAnalysisAsync(string symbol, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedLongRunningClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.PostAsync(Path($"/stocks/{Sym(symbol)}/analysis/refresh"), content: null, ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<StockAnalysis>(response, ct);
    }

    public async Task<MarketRegimeInfo?> GetMarketRegimeAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(Path("/market/regime"), ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<MarketRegimeInfo?>(response, ct);
    }
}
