using System.Globalization;

namespace StockAIPro.Mobile.Models.Product;

/// <summary>
/// Display formatting for product values. A missing value always renders as
/// "-" (never 0 or a guess) so the UI cannot imply data that does not exist.
/// </summary>
public static class ProductFormat
{
    public const string Missing = "-";

    public static string Score(double? v) => v is { } x ? x.ToString("0.0", CultureInfo.InvariantCulture) : Missing;

    /// <summary>A 0-1 fraction as a percentage ("0.953" -> "95.3%").</summary>
    public static string Percent(double? fraction, int decimals = 1) =>
        fraction is { } x ? (x * 100).ToString("F" + decimals, CultureInfo.InvariantCulture) + "%" : Missing;

    public static string Price(double? v) => v is { } x ? x.ToString("#,##0.00", CultureInfo.InvariantCulture) : Missing;

    /// <summary>ISO date/timestamp -> "yyyy-MM-dd"; anything else unchanged.</summary>
    public static string Date(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return Missing;
        return iso.Length >= 10 && DateTime.TryParse(iso[..10], CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? iso[..10]
            : iso;
    }

    /// <summary>CSS modifier for an FQVF status.</summary>
    public static string StatusCss(string? status) => (status ?? "").ToUpperInvariant() switch
    {
        "PASS" => "sai-badge-positive",
        "FAIL" => "sai-badge-negative",
        "WARNING" => "sai-badge-warning",
        _ => "sai-badge-neutral",
    };

    /// <summary>Human label for an FQVF status (NOT_AVAILABLE is not a failure).</summary>
    public static string StatusLabel(string? status) => (status ?? "").ToUpperInvariant() switch
    {
        "PASS" => "Pass",
        "FAIL" => "Fail",
        "WARNING" => "Warning",
        "NOT_AVAILABLE" => "Not available",
        _ => status ?? Missing,
    };

    /// <summary>Freshness keys -> readable labels.</summary>
    public static string FreshnessLabel(string key) => key switch
    {
        "fundamentals_fetched_at" => "Fundamentals fetched",
        "fiscal_period_end" => "Latest fiscal year",
        "market_data_as_of" => "Market data as of",
        "technical_computed_at" => "Technicals computed",
        "sector_outlook_updated_at" => "Sector outlook set",
        _ => key.Replace('_', ' '),
    };
}
