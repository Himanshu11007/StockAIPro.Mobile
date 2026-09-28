using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Watchlist;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class WatchlistApiClientTests
{
    [Fact]
    public async Task GetMyWatchlistAsync_returns_the_raw_list_with_no_envelope()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new[]
        {
            new { id = 1, symbol = "TCS.NS", stock_name = "Tata Consultancy Services", buy_price = 3500.0, buy_date = "2026-01-01", quantity = 2.0, created_at = "2026-01-01T00:00:00Z" },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new WatchlistApiClient(factory);

        var items = await client.GetMyWatchlistAsync();

        Assert.Single(items);
        Assert.Equal("TCS.NS", items[0].Symbol);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
    }

    [Fact]
    public async Task AddAsync_request_body_never_contains_a_user_id()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Created, JsonContent.Create(new
        {
            id = 1, symbol = "TCS.NS", stock_name = "Tata Consultancy Services",
            buy_price = 3500.0, buy_date = "2026-01-01", quantity = 2.0,
            created_at = "2026-01-01T00:00:00Z",
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new WatchlistApiClient(factory);

        await client.AddAsync(new WatchlistAddRequest { Symbol = "TCS.NS", BuyPrice = 3500, BuyDate = "2026-01-01", Quantity = 2 });

        var body = handler.RequestBodies[0]!;
        Assert.DoesNotContain("user_id", body);
        Assert.Contains("\"symbol\":\"TCS.NS\"", body);
    }

    [Fact]
    public async Task AddAsync_400_unknown_symbol_throws_ValidationFailed()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, JsonContent.Create(new { detail = "Stock symbol 'FAKE.NS' is not available" }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new WatchlistApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.AddAsync(
            new WatchlistAddRequest { Symbol = "FAKE.NS", BuyPrice = 1, BuyDate = "2026-01-01" }));

        Assert.Equal(ApiErrorKind.ValidationFailed, ex.Kind);
        Assert.Contains("not available", ex.Message);
    }

    [Fact]
    public async Task AddAsync_409_duplicate_throws_Conflict()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Conflict, JsonContent.Create(new { detail = "TCS.NS is already in your watchlist" }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new WatchlistApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.AddAsync(
            new WatchlistAddRequest { Symbol = "TCS.NS", BuyPrice = 1, BuyDate = "2026-01-01" }));

        Assert.Equal(ApiErrorKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task RemoveAsync_204_returns_true()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NoContent);
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new WatchlistApiClient(factory);

        var removed = await client.RemoveAsync(42);

        Assert.True(removed);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.EndsWith("/watchlist/42", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task RemoveAsync_404_returns_false_not_an_exception()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, JsonContent.Create(new { detail = "Watchlist item not found" }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new WatchlistApiClient(factory);

        var removed = await client.RemoveAsync(999);

        Assert.False(removed);
    }
}
