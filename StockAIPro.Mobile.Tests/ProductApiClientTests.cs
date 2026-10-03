using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StockAIPro.Mobile.Models.Product;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class ProductApiClientTests
{
    private static (ProductApiClient Client, FakeHttpMessageHandler Auth, FakeHttpMessageHandler Raw, FakeHttpMessageHandler Long) Create()
    {
        var auth = new FakeHttpMessageHandler();
        var raw = new FakeHttpMessageHandler();
        var longRunning = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, auth);
        factory.Configure(ApiConfiguration.RawClientName, raw);
        factory.Configure(ApiConfiguration.AuthenticatedLongRunningClientName, longRunning);
        return (new ProductApiClient(factory), auth, raw, longRunning);
    }

    private static object Envelope(object data) => new { success = true, data, message = "ok" };

    private static object AnalysisPayload() => new
    {
        symbol = "RELIANCE.NS",
        name = "Reliance Industries",
        sector = "Energy",
        ranking = new
        {
            stockai_score = 44.7,
            score_coverage = 0.95,
            rank = (int?)null,
            eligible_for_top_picks = false,
            ineligible_reasons = new[] { "20-day average volume below 500,000 (liquidity)" },
            components = new object[]
            {
                new { key = "quality", label = "Business quality", score = 50.0, weight = 25.0, contribution = 13.2, basis = "FQVF" },
                new { key = "sector_outlook", label = "Sector outlook", score = (double?)null, weight = 5.0, contribution = (double?)null, basis = "no FQVF check in this group could be evaluated" },
            },
            positives = new[] { "D/E 0.46: strong." },
            risks = new[] { "Return on Equity (ROE): ROE 8.9%" },
            engine_version = "ranking-v1.0",
        },
        fqvf = new
        {
            version = "fqvf-v1.0",
            summary = "7 pass, 4 warning, 4 fail, 3 not available",
            score = 53.8,
            coverage = 0.812,
            counts = new Dictionary<string, int> { ["PASS"] = 7, ["NOT_AVAILABLE"] = 3 },
            checks = new object[]
            {
                new { id = 6, name = "PE Ratio", status = "PASS", value = 21.14, threshold = "PE <= 25", explanation = "Trailing PE 21.14.", scored = true, data_available = true },
                new { id = 8, name = "Historical Average PE (5-7 years)", status = "NOT_AVAILABLE", value = (object?)null, threshold = "x", explanation = "4 year(s) of EPS available.", scored = true, data_available = false },
                new { id = 9, name = "Intrinsic Value", status = "FAIL", value = new { intrinsic_value = 911.13, price = 1167.7 }, threshold = "x", explanation = "above", scored = true, data_available = true },
                new { id = 3, name = "Industry Classification", status = "PASS", value = "Energy / Oil", threshold = "x", explanation = "ok", scored = false, data_available = true },
            },
        },
        market = (object?)null,
        ml_signal = new { available = true, direction = "UP", probability_up = 0.59, disclaimer = "Informational only." },
        freshness = new Dictionary<string, string?> { ["market_data_as_of"] = "2026-10-01", ["sector_outlook_updated_at"] = null },
        engine = new { ranking = "ranking-v1.0", fqvf = "fqvf-v1.0", run_id = "RANKING-1", run_kind = "RANKING" },
    };

    [Fact]
    public async Task GetAppConfig_uses_the_unauthenticated_client()
    {
        var (client, auth, raw, _) = Create();
        raw.Enqueue(HttpStatusCode.OK, JsonContent.Create(Envelope(new
        {
            features = new Dictionary<string, bool> { ["top_picks"] = false },
            disclaimer = "Not advice.",
            announcement = (string?)null,
            top_picks_limit = 20,
        })));

        var config = await client.GetAppConfigAsync();

        Assert.Empty(auth.Requests);
        Assert.EndsWith("/api/v1/app/config", raw.Requests[0].RequestUri!.ToString());
        Assert.False(config.Features["top_picks"]);
        Assert.Equal(20, config.TopPicksLimit);
    }

    [Fact]
    public async Task GetTopCandidates_parses_items_run_and_regime()
    {
        var (client, auth, _, _) = Create();
        auth.Enqueue(HttpStatusCode.OK, JsonContent.Create(Envelope(new
        {
            items = new object[]
            {
                new { rank = 1, symbol = "CANBK.NS", name = "Canara Bank", stockai_score = 77.0, score_coverage = 0.95,
                      fqvf_score = 80.0, fqvf_summary = "14 pass", positives = new[] { "p" }, risks = new[] { "r" },
                      freshness = new Dictionary<string, string?> { ["market_data_as_of"] = "2026-10-01" }, engine_version = "ranking-v1.0" },
            },
            total_eligible = 157,
            limit = 20,
            run = new { run_id = "RANKING-1", status = "COMPLETED", stocks_analysed = 299 },
            market_regime = new { index = "^NSEI", regime = "Bearish", as_of_date = "2026-10-01" },
            disclaimer = "Not advice.",
        })));

        var result = await client.GetTopCandidatesAsync(limit: 10);

        Assert.Contains("/api/v1/top-picks?limit=10", auth.Requests[0].RequestUri!.ToString());
        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.Rank);
        Assert.Equal(77.0, item.StockAiScore);
        Assert.Equal(157, result.TotalEligible);
        Assert.Equal("Bearish", result.MarketRegime!.Regime);
        Assert.Equal(299, result.Run!.StocksAnalysed);
    }

    [Fact]
    public async Task GetTopCandidates_empty_before_first_run_is_not_an_error()
    {
        var (client, auth, _, _) = Create();
        auth.Enqueue(HttpStatusCode.OK, JsonContent.Create(Envelope(new
        {
            items = Array.Empty<object>(), total_eligible = 0, limit = 20, run = (object?)null,
            market_regime = (object?)null, disclaimer = "x",
        })));

        var result = await client.GetTopCandidatesAsync();

        Assert.Empty(result.Items);
        Assert.Null(result.Run);
        Assert.Null(result.MarketRegime);
    }

    [Fact]
    public async Task GetStockAnalysis_parses_fqvf_values_of_every_shape_without_inventing_any()
    {
        var (client, auth, _, _) = Create();
        auth.Enqueue(HttpStatusCode.OK, JsonContent.Create(Envelope(AnalysisPayload())));

        var a = await client.GetStockAnalysisAsync("reliance.ns");

        Assert.EndsWith("/api/v1/stocks/RELIANCE.NS/analysis", auth.Requests[0].RequestUri!.ToString());
        Assert.Equal(44.7, a.Ranking.StockAiScore);
        Assert.Null(a.Ranking.Components[1].Score);               // missing component stays missing
        Assert.Equal("21.14", a.Fqvf.Checks[0].DisplayValue);
        Assert.Equal("-", a.Fqvf.Checks[1].DisplayValue);         // NOT_AVAILABLE: no fabricated value
        Assert.Equal("NOT_AVAILABLE", a.Fqvf.Checks[1].Status);
        Assert.Contains("intrinsic value: 911.13", a.Fqvf.Checks[2].DisplayValue);
        Assert.Equal("Energy / Oil", a.Fqvf.Checks[3].DisplayValue);
        Assert.Null(a.Market);
        Assert.True(a.MlSignal!.Available);
        Assert.Null(a.Freshness["sector_outlook_updated_at"]);
    }

    [Fact]
    public async Task GetStockAnalysis_not_analysed_maps_to_NotFound_with_backend_message()
    {
        var (client, auth, _, _) = Create();
        auth.Enqueue(HttpStatusCode.NotFound, JsonContent.Create(new { detail = "NEW.NS has not been analysed yet." }));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetStockAnalysisAsync("NEW.NS"));

        Assert.Equal(ApiErrorKind.NotFound, ex.Kind);
        Assert.Contains("not been analysed", ex.Message);
    }

    [Fact]
    public async Task RefreshStockAnalysis_uses_the_long_running_client_and_maps_busy_to_Conflict()
    {
        var (client, auth, _, longRunning) = Create();
        longRunning.Enqueue(HttpStatusCode.Conflict, JsonContent.Create(new { detail = "busy" }));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.RefreshStockAnalysisAsync("RELIANCE.NS"));

        Assert.Empty(auth.Requests);
        Assert.Equal(HttpMethod.Post, longRunning.Requests[0].Method);
        Assert.EndsWith("/api/v1/stocks/RELIANCE.NS/analysis/refresh", longRunning.Requests[0].RequestUri!.ToString());
        Assert.Equal(ApiErrorKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task Network_failure_maps_to_NetworkUnavailable()
    {
        var (client, auth, _, _) = Create();
        auth.EnqueueNetworkFailure();

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetTopCandidatesAsync());

        Assert.Equal(ApiErrorKind.NetworkUnavailable, ex.Kind);
        Assert.DoesNotContain("simulated", ex.Message);
    }

    [Fact]
    public async Task Server_error_maps_to_ServerError()
    {
        var (client, auth, _, _) = Create();
        auth.Enqueue(HttpStatusCode.InternalServerError,
            JsonContent.Create(new { success = false, error = "Internal server error", details = "Unexpected server error (reference abc)" }));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetTopCandidatesAsync());

        Assert.Equal(ApiErrorKind.ServerError, ex.Kind);
    }

    [Fact]
    public async Task Market_regime_null_is_allowed()
    {
        var (client, auth, _, _) = Create();
        auth.Enqueue(HttpStatusCode.OK, JsonContent.Create(new { success = true, data = (object?)null, message = "none" }));

        Assert.Null(await client.GetMarketRegimeAsync());
    }

    [Fact]
    public async Task Blank_symbol_is_rejected_before_any_request()
    {
        var (client, auth, _, _) = Create();
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetStockAnalysisAsync("  "));
        Assert.Empty(auth.Requests);
    }

    [Theory]
    [InlineData("null", "-")]
    [InlineData("0.4632", "0.4632")]
    [InlineData("true", "yes")]
    [InlineData("{}", "-")]
    [InlineData("{\"pe\": 21.1, \"peers\": 5}", "pe: 21.1, peers: 5")]
    public void FqvfCheck_FormatValue(string json, string expected)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, FqvfCheck.FormatValue(doc.RootElement.Clone()));
    }
}
