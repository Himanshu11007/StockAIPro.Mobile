using StockAIPro.Mobile.Models.Intelligence;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>GET /api/v1/intelligence/report - the Recommendation
/// Intelligence Engine's read-only analytics report (see
/// api/routes/intelligence.py). Never re-derive these insights client-side;
/// this is the sole source of them.</summary>
public interface IIntelligenceApiClient
{
    Task<IntelligenceReport> GetReportAsync(CancellationToken ct = default);
}
