using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Watchlist;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

public sealed class WatchlistApiClient : IWatchlistApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WatchlistApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<WatchlistItem>> GetMyWatchlistAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.GetAsync($"{ApiConfiguration.ApiPrefix}/watchlist", ct), ct);

        return await BusinessApiSend.ReadRawOrThrowAsync<List<WatchlistItem>>(response, ct);
    }

    public async Task<WatchlistItem> AddAsync(WatchlistAddRequest request, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.PostAsJsonAsync($"{ApiConfiguration.ApiPrefix}/watchlist", request, ct), ct);

        return await BusinessApiSend.ReadRawOrThrowAsync<WatchlistItem>(response, ct);
    }

    public async Task<bool> RemoveAsync(int itemId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);
        using var response = await BusinessApiSend.SendAsync(
            () => client.DeleteAsync($"{ApiConfiguration.ApiPrefix}/watchlist/{itemId}", ct), ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        if (!response.IsSuccessStatusCode)
            throw await BackendErrorParser.FromResponseAsync(response, ct);

        return true; // 204 No Content on success
    }
}
