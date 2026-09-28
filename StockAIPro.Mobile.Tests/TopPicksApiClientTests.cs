using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Models.TopPicks;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class TopPicksApiClientTests
{
    [Fact]
    public async Task StartScanAsync_posts_category_and_unwraps_the_envelope()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true,
            message = "Scan started",
            data = new { scan_id = "abc123", status = "started", category = "Large Cap" },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new TopPicksApiClient(factory);

        var result = await client.StartScanAsync(StockCategory.LargeCap);

        Assert.Contains("\"category\":\"Large Cap\"", handler.RequestBodies[0]);
        Assert.Equal("abc123", result.ScanId);
        Assert.Equal("started", result.Status);
    }

    [Fact]
    public async Task GetStatusAsync_maps_running_progress_fields()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true, message = "Scan status retrieved",
            data = new { scan_id = "abc123", status = "running", progress = 4, total = 10, message = "Scanning Large Cap" },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new TopPicksApiClient(factory);

        var status = await client.GetStatusAsync("abc123");

        Assert.Equal("running", status.Status);
        Assert.Equal(4, status.Progress);
        Assert.Equal(10, status.Total);
        Assert.Contains("status/abc123", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetResultAsync_maps_result_items_including_pillar_scores()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true, message = "Scan result retrieved",
            data = new
            {
                scan_id = "abc123",
                status = "completed",
                results = new[]
                {
                    new
                    {
                        stock = "Reliance Industries", symbol = "RELIANCE.NS", signal = "BUY",
                        score = 0.81, confidence = 88.0, accuracy = 79.5, reason = "Strong confluence",
                        factors = new[] { "Momentum" }, close = 2900.0, stop_loss = (double?)2750.0,
                        target = (double?)3100.0, rr_ratio = (double?)1.5, regime = "Bullish",
                        weekly_trend = "Up", daily_trend = "Up", timeframe_score = 0.6,
                        model = "Ensemble", news_score = 0.2,
                        pillar_scores = new Dictionary<string, double> { ["ML Direction"] = 0.7 },
                        weighted_score = 0.65, sector = "IT", engine_version = "v1.0",
                    },
                },
            },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new TopPicksApiClient(factory);

        var result = await client.GetResultAsync("abc123");

        Assert.Equal("completed", result.Status);
        var item = Assert.Single(result.Results);
        Assert.Equal("RELIANCE.NS", item.Symbol);
        Assert.Equal(0.7, item.PillarScores["ML Direction"]);
        Assert.Equal("IT", item.Sector);
    }

    [Fact]
    public async Task GetResultAsync_unknown_scan_id_throws_ValidationFailed()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, JsonContent.Create(new
        {
            success = false, error = "Invalid request", details = "Unknown scan_id 'nope'",
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new TopPicksApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetResultAsync("nope"));

        Assert.Equal(ApiErrorKind.ValidationFailed, ex.Kind);
        Assert.Contains("Unknown scan_id", ex.Message);
    }
}
