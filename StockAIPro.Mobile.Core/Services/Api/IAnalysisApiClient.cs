using StockAIPro.Mobile.Models.Analysis;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// POST /api/v1/analyze-stock - runs the full StockAI Pro analysis pipeline
/// for one symbol (see api/routes/analysis.py). This is the ONLY source of
/// analysis results; the mobile app must never compute its own signal,
/// score, or confidence.
/// </summary>
public interface IAnalysisApiClient
{
    /// <summary>Throws ApiException with Kind == ValidationFailed (400) for
    /// an invalid/unknown symbol or insufficient data (services.py raises
    /// ValueError for both, mapped to 400 by api/main.py's centralized
    /// handler).</summary>
    Task<AnalyzeStockResult> AnalyzeAsync(string symbol, CancellationToken ct = default);
}
