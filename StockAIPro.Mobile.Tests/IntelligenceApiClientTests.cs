using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class IntelligenceApiClientTests
{
    [Fact]
    public async Task GetReportAsync_maps_a_full_successful_report()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true,
            message = "Intelligence report generated (120 records analysed)",
            data = new
            {
                summary = new
                {
                    total = 120, successful = 70, failed = 50, success_rate = 58.3,
                    avg_return = 1.1, best_return = 20.0, worst_return = -12.0,
                    median_return = 0.9, data_quality = "120 validated records.",
                },
                threshold_analysis = new[]
                {
                    new { threshold = 0.50, trades = 120, successful = 70, failed = 50, success_rate = 58.3, avg_return = 1.1, median_return = 0.9, win_loss_ratio = (double?)1.4 },
                },
                confidence_analysis = Array.Empty<object>(),
                pillar_analysis = new[]
                {
                    new { pillar = "ML Direction", avg_score = (double?)0.6, corr_with_success = (double?)0.42, corr_with_return = (double?)0.3, interpretation = "ML Direction has a moderate positive influence on success." },
                },
                sector_analysis = Array.Empty<object>(),
                regime_analysis = Array.Empty<object>(),
                signal_analysis = Array.Empty<object>(),
                recommendations = new[]
                {
                    new { title = "Confluence Threshold Observation", priority = "High", confidence = 85, recommendation = "...", evidence = "..." },
                },
                meta = new { execution_time_ms = 12.3, records_analyzed = 120, engine_version = "v1.0" },
            },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new IntelligenceApiClient(factory);

        var report = await client.GetReportAsync();

        Assert.Equal(120, report.Summary.Total);
        Assert.Single(report.ThresholdAnalysis);
        Assert.Equal(0.42, report.PillarAnalysis[0].CorrWithSuccess);
        Assert.Equal("High", report.Recommendations[0].Priority);
        Assert.Null(report.Meta.Error);
    }

    [Fact]
    public async Task GetReportAsync_engine_failure_fallback_with_empty_summary_object_does_not_throw()
    {
        // Mirrors generate_engine_report()'s except branch exactly:
        // "summary": {} is a literal empty object, not a full IntelligenceSummary.
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true,
            message = "Intelligence report generated (0 records analysed)",
            data = new
            {
                summary = new { },
                threshold_analysis = Array.Empty<object>(),
                confidence_analysis = Array.Empty<object>(),
                pillar_analysis = Array.Empty<object>(),
                sector_analysis = Array.Empty<object>(),
                regime_analysis = Array.Empty<object>(),
                signal_analysis = Array.Empty<object>(),
                recommendations = Array.Empty<object>(),
                meta = new { execution_time_ms = 4.0, records_analyzed = 0, engine_version = "v1.0", error = "Intelligence engine failed — see logs." },
            },
        }));
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, handler);
        var client = new IntelligenceApiClient(factory);

        var report = await client.GetReportAsync();

        Assert.Equal(0, report.Summary.Total);
        Assert.Equal("", report.Summary.DataQuality);
        Assert.NotNull(report.Meta.Error);
    }
}
