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

    private static readonly TimeSpan IstOffset = TimeSpan.FromHours(5.5);

    private static bool TryParseInstant(string? iso, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(iso)) return false;
        return DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
    }

    /// <summary>ISO timestamp -> "03 Oct 2026 09:35 IST" (India time). A
    /// date-only value is shown as "03 Oct 2026".</summary>
    public static string DateTimeIst(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return Missing;
        if (iso.Length == 10 && DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        return TryParseInstant(iso, out var t)
            ? t.ToOffset(IstOffset).ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture) + " IST"
            : iso;
    }

    /// <summary>"just now", "5 minutes ago", "3 hours ago", "2 days ago".</summary>
    public static string Relative(string? iso, DateTimeOffset now)
    {
        if (!TryParseInstant(iso, out var t)) return Missing;
        var age = now - t;
        if (age < TimeSpan.Zero) age = TimeSpan.Zero;
        if (age.TotalMinutes < 1) return "just now";
        if (age.TotalMinutes < 60) return Plural((int)age.TotalMinutes, "minute") + " ago";
        if (age.TotalHours < 24) return Plural((int)age.TotalHours, "hour") + " ago";
        return Plural((int)age.TotalDays, "day") + " ago";
    }

    private static string Plural(int n, string unit) => $"{n} {unit}{(n == 1 ? "" : "s")}";

    /// <summary>One freshness line for a list item or header. Stale data is
    /// never presented as current; unavailable data says so.</summary>
    public static string FreshnessLine(FreshnessStatus? f, DateTimeOffset now)
    {
        if (f is null || f.IsUnavailable) return "Data unavailable";
        var market = f.MarketDataAsOf is null ? "Market data unavailable" : $"Market data {DateTimeIst(f.MarketDataAsOf)}";
        var analysis = f.AnalysisComputedAt is null ? "" : $" - analysis {Relative(f.AnalysisComputedAt, now)}";
        return f.IsStale ? $"Data may be stale. {market}{analysis}" : market + analysis;
    }

    public const string NotAvailable = "Not available";

    /// <summary>Indian rupee amount, e.g. "₹1,234.50"; missing -> "Not available".</summary>
    public static string Rupees(double? v) =>
        v is { } x ? "₹" + x.ToString("#,##0.00", CultureInfo.InvariantCulture) : NotAvailable;

    /// <summary>Current-price status label. Prices are delayed provider data,
    /// so nothing is ever labelled "live".</summary>
    public static string PriceStatusLabel(string? status) => (status ?? "").ToUpperInvariant() switch
    {
        "LAST_CLOSE" => "Last close",
        "DELAYED_INTRADAY" => "Delayed",
        "STALE" => "Stale",
        _ => NotAvailable,
    };

    /// <summary>CSS modifier for a current-price status.</summary>
    public static string PriceStatusCss(string? status) => (status ?? "").ToUpperInvariant() switch
    {
        "LAST_CLOSE" or "DELAYED_INTRADAY" => "sai-badge-neutral",
        _ => "sai-badge-warning",
    };

    /// <summary>Signed change, e.g. "+12.5" / "-3.0"; missing -> "-".</summary>
    public static string Change(double? v) => v is { } x ? x.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) : Missing;

    /// <summary>CSS class for an analytical label tone.</summary>
    public static string LabelCss(string? tone) => tone switch
    {
        "positive" => "sai-badge-positive",
        "negative" => "sai-badge-negative",
        "warning" => "sai-badge-warning",
        _ => "sai-badge-neutral",
    };
}
