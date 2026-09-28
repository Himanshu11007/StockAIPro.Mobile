using StockAIPro.Mobile.Models.TopPicks;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// The Top Picks background scan flow (see api/routes/top_picks.py): start
/// a scan, poll its status, then fetch the cached result. This client is
/// deliberately thin - it mirrors the three endpoints exactly and performs
/// no polling loop itself; the caller (a ViewModel/page) owns the poll
/// interval/timeout/cancellation policy, since the backend does not dictate
/// one and inventing a fixed interval here would be a UI decision made in
/// the wrong layer.
/// </summary>
public interface ITopPicksApiClient
{
    /// <summary>Throws ApiException with Kind == ValidationFailed (400) for
    /// a category outside StockCategory.All (see
    /// api/services.py:start_top_picks_scan). If a scan is already running
    /// (for any category - the scanner has no per-category concurrency),
    /// the backend still returns a fresh scan_id bound to this category and
    /// reports status "started".</summary>
    Task<StartScanResult> StartScanAsync(string category, CancellationToken ct = default);

    /// <summary>Throws nothing for an unknown scan_id - the backend reports
    /// status "not_found" instead (see api/services.py:get_scan_status()).</summary>
    Task<ScanStatus> GetStatusAsync(string scanId, CancellationToken ct = default);

    /// <summary>Throws ApiException with Kind == ValidationFailed (400) for
    /// an unknown scan_id (services.py:get_scan_result raises ValueError).
    /// Results may be empty (e.g. the scan is still running, or nothing
    /// passed the quality filters) - callers should check ScanResult.Status
    /// before treating an empty list as "no picks found".</summary>
    Task<ScanResult> GetResultAsync(string scanId, CancellationToken ct = default);
}
