using StockAIPro.Mobile.Models.Performance;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Read-only performance analytics over validated recommendations (see
/// api/routes/performance.py). All figures are historical/observed, never a
/// prediction guarantee - preserve that distinction in the UI.
/// </summary>
public interface IPerformanceApiClient
{
    Task<PerformanceSummary> GetSummaryAsync(CancellationToken ct = default);

    Task<List<SignalPerformance>> GetBySignalAsync(CancellationToken ct = default);

    Task<List<ConfidencePerformance>> GetByConfidenceAsync(CancellationToken ct = default);

    Task<List<ConfluencePerformance>> GetByConfluenceAsync(CancellationToken ct = default);
}
