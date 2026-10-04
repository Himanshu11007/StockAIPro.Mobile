using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StockAIPro.Mobile.Models.Common;
using StockAIPro.Mobile.Models.Product;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

/// <summary>Top Picks freshness: the list is fetched from the API on every
/// open (no local cache), and each candidate's current price (latest
/// available, delayed) is kept separate from the reference price and ranking
/// date of the run that produced the ranking.</summary>
public class CurrentPriceTests
{
    private static (ProductApiClient Client, FakeHttpMessageHandler Auth) Create()
    {
        var auth = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, auth);
        factory.Configure(ApiConfiguration.RawClientName, new FakeHttpMessageHandler());
        factory.Configure(ApiConfiguration.AuthenticatedLongRunningClientName, new FakeHttpMessageHandler());
        return (new ProductApiClient(factory), auth);
    }

    private static object Envelope(object data) => new { success = true, data, message = "ok" };

    private static object TopPicks(string rankingDate, double? currentPrice, double referencePrice) => new
    {
        items = new object[]
        {
            new
            {
                rank = 3, symbol = "RELIANCE.NS", name = "Reliance Industries", stockai_score = 93.5,
                stocklens_score = 93.5, ranking_date = rankingDate, reference_price = referencePrice,
                reference_price_as_of = rankingDate, current_price = currentPrice,
                current_price_as_of = currentPrice is null ? null : "2026-10-06T10:00:00+00:00",
                current_price_status = currentPrice is null ? "NOT_AVAILABLE" : "LAST_CLOSE",
                current_price_source = currentPrice is null ? null : "yfinance",
                current_price_date = currentPrice is null ? null : "2026-10-06", market_status = "CLOSED",
            },
        },
        total_eligible = 157, limit = 20, ranking_date = rankingDate,
        run = new { run_id = "RANKING-" + rankingDate, status = "COMPLETED", stocks_analysed = 299, ranking_date = rankingDate },
        market_status = new { status = "CLOSED", label = "Closed", is_open = false },
        disclaimer = "x",
    };

    private static T Data<T>(string fixture) =>
        JsonSerializer.Deserialize<ApiEnvelope<T>>(File.ReadAllText($"Fixtures/{fixture}.json"))!.Data;

    private static string RepoFile(params string[] parts)
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "StockAIPro.Mobile.slnx")))
            d = d.Parent;
        return Path.Combine([d!.FullName, .. parts]);
    }

    // ── every open calls the API ─────────────────────────────────────────────

    [Fact]
    public async Task Every_call_fetches_from_the_api_and_returns_the_newest_ranking()
    {
        var (client, auth) = Create();
        auth.Enqueue(HttpStatusCode.OK, JsonContent.Create(Envelope(TopPicks("2026-10-05", 1200.0, 1200.0))));
        auth.Enqueue(HttpStatusCode.OK, JsonContent.Create(Envelope(TopPicks("2026-10-05", 1400.0, 1200.0))));
        auth.Enqueue(HttpStatusCode.OK, JsonContent.Create(Envelope(TopPicks("2026-10-06", 1400.0, 1400.0))));

        var day1 = await client.GetTopCandidatesAsync();
        var day2BeforeRun = await client.GetTopCandidatesAsync();
        var day2AfterRun = await client.GetTopCandidatesAsync();

        Assert.Equal(3, auth.Requests.Count);
        Assert.All(auth.Requests, r => Assert.EndsWith("/api/v1/top-picks", r.RequestUri!.AbsolutePath));
        Assert.Equal(1200.0, day1.Items[0].CurrentPrice);
        // Day 2 before the run: new current price, same ranking and reference price.
        Assert.Equal(1400.0, day2BeforeRun.Items[0].CurrentPrice);
        Assert.Equal(1200.0, day2BeforeRun.Items[0].ReferencePrice);
        Assert.Equal("2026-10-05", day2BeforeRun.RankingDate);
        // After the Day-2 run: the Day-2 ranking and reference price.
        Assert.Equal(1400.0, day2AfterRun.Items[0].ReferencePrice);
        Assert.Equal("2026-10-06", day2AfterRun.Items[0].RankingDate);
    }

    [Fact]
    public void Top_picks_page_loads_on_every_open_and_keeps_no_local_cache()
    {
        var page = File.ReadAllText(RepoFile("StockAIPro.Mobile", "Components", "Pages", "TopPicks.razor"));
        Assert.Contains("OnInitializedAsync() => LoadAsync()", page);
        Assert.Contains("ProductApi.GetTopCandidatesAsync", page);
        foreach (var store in new[] { "Preferences.", "SecureStorage", "FileSystem.", "IMemoryCache", "static TopCandidatesResponse" })
            Assert.DoesNotContain(store, page);
        var client = File.ReadAllText(RepoFile("StockAIPro.Mobile.Core", "Services", "Api", "ProductApiClient.cs"));
        Assert.DoesNotContain("cache", client, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Top_picks_page_shows_current_and_reference_prices_separately()
    {
        var page = File.ReadAllText(RepoFile("StockAIPro.Mobile", "Components", "Pages", "TopPicks.razor"));
        Assert.Contains("Current Price", page);
        Assert.Contains("Price as of", page);
        Assert.Contains("Reference Price", page);
        Assert.Contains("Ranking Date", page);
        Assert.Contains("item.CurrentPrice", page);
        Assert.Contains("item.ReferencePrice", page);
        Assert.DoesNotContain("live", page.Replace("delivered", ""), StringComparison.OrdinalIgnoreCase);
    }

    // ── formatting ───────────────────────────────────────────────────────────

    [Fact]
    public void Missing_price_is_not_available_never_zero()
    {
        Assert.Equal("Not available", ProductFormat.Rupees(null));
        Assert.Equal("₹1,400.00", ProductFormat.Rupees(1400));
        Assert.Equal("Not available", ProductFormat.PriceStatusLabel("NOT_AVAILABLE"));
        Assert.Equal("Not available", ProductFormat.PriceStatusLabel(null));
    }

    [Theory]
    [InlineData("LAST_CLOSE", "Last close")]
    [InlineData("DELAYED_INTRADAY", "Delayed")]
    [InlineData("STALE", "Stale")]
    public void Price_status_is_never_called_live(string status, string label)
    {
        Assert.Equal(label, ProductFormat.PriceStatusLabel(status));
        Assert.DoesNotContain("live", ProductFormat.PriceStatusLabel(status), StringComparison.OrdinalIgnoreCase);
    }

    // ── contract: real backend responses ─────────────────────────────────────

    [Fact]
    public void Contract_top_picks_has_ranking_date_reference_and_current_prices()
    {
        var tp = Data<TopCandidatesResponse>("top_picks");
        Assert.NotNull(tp.RankingDate);
        Assert.Equal(tp.RankingDate, tp.Run!.RankingDate);
        Assert.NotNull(tp.MarketStatus);
        Assert.False(string.IsNullOrEmpty(tp.MarketStatus!.Status));
        Assert.All(tp.Items, i =>
        {
            Assert.Equal(tp.RankingDate, i.RankingDate);
            Assert.Equal(i.StockAiScore, i.StockLensScore);
            Assert.NotNull(i.ReferencePrice);
            Assert.NotNull(i.ReferencePriceAsOf);
            Assert.Equal(tp.MarketStatus.Status, i.MarketStatus);
        });
        var priced = tp.Items.Single(i => i.Symbol == "S00.NS");
        Assert.Equal(1012.25, priced.CurrentPrice);
        Assert.Equal("DELAYED_INTRADAY", priced.CurrentPriceStatus);
        Assert.NotNull(priced.CurrentPriceAsOf);
        Assert.NotEqual(priced.ReferencePrice, priced.CurrentPrice);
        var missing = tp.Items.Single(i => i.Symbol == "S01.NS");
        Assert.Null(missing.CurrentPrice);
        Assert.Null(missing.CurrentPriceAsOf);
        Assert.Equal("NOT_AVAILABLE", missing.CurrentPriceStatus);
        Assert.NotNull(missing.ReferencePrice);           // the reference price is never used as current
    }

    [Fact]
    public void Contract_stock_analysis_has_current_price_and_ranking_date()
    {
        var a = Data<StockAnalysis>("stock_analysis");
        Assert.Equal(1250.0, a.CurrentPrice!.Price);
        Assert.Equal("DELAYED_INTRADAY", a.CurrentPrice.Status);
        Assert.NotNull(a.CurrentPrice.AsOf);
        Assert.NotNull(a.RankingDate);
    }
}
