using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class StockApiClientTests
{
    [Fact]
    public async Task SearchAsync_sends_search_limit_and_offset_as_query_params()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new[]
        {
            new { symbol = "RELIANCE.NS", name = "Reliance Industries", sector = (string?)null, industry = (string?)null, exchange = "NSE", analysis_enabled = true },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new StockApiClient(factory);

        var results = await client.SearchAsync("reliance", limit: 10, offset: 5);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("limit=10", request.RequestUri!.ToString());
        Assert.Contains("offset=5", request.RequestUri!.ToString());
        Assert.Contains("search=reliance", request.RequestUri!.ToString());
        Assert.Single(results);
        Assert.Equal("RELIANCE.NS", results[0].Symbol);
    }

    [Fact]
    public async Task SearchAsync_omits_search_param_when_not_provided()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(Array.Empty<object>()));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new StockApiClient(factory);

        var results = await client.SearchAsync();

        Assert.DoesNotContain("search=", handler.Requests[0].RequestUri!.ToString());
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAsync_404_returns_null_not_an_exception()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, JsonContent.Create(new { detail = "Stock not found" }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new StockApiClient(factory);

        var result = await client.GetAsync("UNKNOWN.NS");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_401_throws_Unauthorized_ApiException()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized, JsonContent.Create(new { detail = "Not authenticated" }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new StockApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync("RELIANCE.NS"));

        Assert.Equal(ApiErrorKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task SearchAsync_network_failure_throws_NetworkUnavailable()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueNetworkFailure();
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new StockApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.SearchAsync());

        Assert.Equal(ApiErrorKind.NetworkUnavailable, ex.Kind);
    }
}
