using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class PerformanceApiClientTests
{
    [Fact]
    public async Task GetSummaryAsync_unwraps_envelope_and_hits_the_correct_path()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true, message = "Performance summary retrieved",
            data = new { total = 100, successful = 60, failed = 40, success_rate = 60.0, avg_return = 1.2, best_return = 15.0, worst_return = -8.0 },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new PerformanceApiClient(factory);

        var summary = await client.GetSummaryAsync();

        Assert.EndsWith("/performance/summary", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(100, summary.Total);
        Assert.Equal(60.0, summary.SuccessRate);
    }

    [Fact]
    public async Task GetBySignalAsync_maps_the_space_and_percent_JSON_keys()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true, message = "Signal performance retrieved",
            data = new[]
            {
                new Dictionary<string, object>
                {
                    ["Signal"] = "BUY",
                    ["Count"] = 42,
                    ["Success Rate %"] = 66.7,
                    ["Avg Return %"] = 2.3,
                },
            },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new PerformanceApiClient(factory);

        var rows = await client.GetBySignalAsync();

        var row = Assert.Single(rows);
        Assert.Equal("BUY", row.Signal);
        Assert.Equal(42, row.Count);
        Assert.Equal(66.7, row.SuccessRatePercent);
        Assert.Equal(2.3, row.AvgReturnPercent);
    }

    [Fact]
    public async Task GetByConfidenceAsync_unauthorized_throws()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized, JsonContent.Create(new { detail = "Not authenticated" }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new PerformanceApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetByConfidenceAsync());

        Assert.Equal(ApiErrorKind.Unauthorized, ex.Kind);
    }
}
