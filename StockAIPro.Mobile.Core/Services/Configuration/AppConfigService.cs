using StockAIPro.Mobile.Models.Product;
using StockAIPro.Mobile.Services.Api;

namespace StockAIPro.Mobile.Services.Configuration;

/// <summary>
/// Backend-controlled client configuration (feature flags, disclaimer,
/// announcement), loaded from GET /api/v1/app/config and cached for the
/// session. Never throws: if the backend is unreachable the app keeps
/// working with every feature visible and the built-in disclaimer, and
/// IsFallback / LastError say so. Feature decisions are the backend's; this
/// class only reads them.
/// </summary>
public sealed class AppConfigService
{
    public const string FallbackDisclaimer =
        "StockLens provides research and analysis, not investment advice. Scores rank stocks on " +
        "available data; they are not predictions or guarantees of returns.";

    private readonly IProductApiClient _api;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AppConfigService(IProductApiClient api)
    {
        _api = api;
    }

    public AppConfig? Current { get; private set; }
    public bool IsFallback => Current is null;
    public string? LastError { get; private set; }

    public event Action? Changed;

    public string Disclaimer => string.IsNullOrWhiteSpace(Current?.Disclaimer) ? FallbackDisclaimer : Current!.Disclaimer!;

    public string? Announcement => string.IsNullOrWhiteSpace(Current?.Announcement) ? null : Current!.Announcement;

    /// <summary>Unknown features and an unloaded configuration default to
    /// enabled: hiding the app's own screens because the config endpoint
    /// was unreachable would be worse than showing them.</summary>
    public bool IsEnabled(string feature) =>
        Current?.Features is not { } f || !f.TryGetValue(feature, out var enabled) || enabled;

    public async Task LoadAsync(bool force = false, CancellationToken ct = default)
    {
        if (Current is not null && !force) return;
        await _gate.WaitAsync(ct);
        try
        {
            if (Current is not null && !force) return;
            Current = await _api.GetAppConfigAsync(ct);
            LastError = null;
        }
        catch (ApiException ex)
        {
            LastError = ex.Message;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Malformed payload etc. - keep running on defaults.
            LastError = "The server configuration could not be read.";
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke();
    }
}
