using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class AnalysisApiClientTests
{
    [Fact]
    public async Task AnalyzeAsync_posts_symbol_and_unwraps_the_success_envelope()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true,
            message = "Analysis complete for RELIANCE.NS",
            data = new
            {
                symbol = "RELIANCE.NS", stock = "Reliance Industries", signal = "BUY",
                score = 0.72, confidence = 81.5, accuracy = 0.834, news_score = 0.12,
                regime = "Bullish", weekly_trend = "Up", daily_trend = "Up",
                target = (double?)2950.0, stop_loss = (double?)2700.0,
                factors = new[] { "Strong momentum" }, explanation = (object?)null,
            },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new AnalysisApiClient(factory);

        var result = await client.AnalyzeAsync("RELIANCE.NS");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains("\"symbol\":\"RELIANCE.NS\"", handler.RequestBodies[0]);
        Assert.Equal("BUY", result.Signal);
        Assert.Equal(0.72, result.Score);
        Assert.Equal(81.5, result.Confidence);
        Assert.Equal(0.834, result.Accuracy);
        Assert.Null(result.Explanation);
    }

    [Fact]
    public async Task AnalyzeAsync_400_from_centralized_ValueError_handler_surfaces_the_specific_message()
    {
        // api/main.py's ValueError handler (not FastAPI's HTTPException
        // default) - a different JSON shape than {"detail": ...}.
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, JsonContent.Create(new
        {
            success = false,
            error = "Invalid request",
            details = "No price data found for symbol 'FAKE.NS'",
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new AnalysisApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.AnalyzeAsync("FAKE.NS"));

        Assert.Equal(ApiErrorKind.ValidationFailed, ex.Kind);
        Assert.Equal("No price data found for symbol 'FAKE.NS'", ex.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_500_throws_ServerError()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError, JsonContent.Create(new
        {
            success = false, error = "Internal server error", details = "Model training failed: boom",
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new AnalysisApiClient(factory);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.AnalyzeAsync("RELIANCE.NS"));

        Assert.Equal(ApiErrorKind.ServerError, ex.Kind);
    }
}
