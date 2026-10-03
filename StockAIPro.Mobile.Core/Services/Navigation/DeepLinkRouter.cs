using System.Text.RegularExpressions;

namespace StockAIPro.Mobile.Services.Navigation;

/// <summary>
/// Turns a notification payload or a stockaipro:// link into an in-app
/// route. Only known app routes are accepted; anything else (web URLs, other
/// schemes, path traversal, unknown pages, malformed symbols) is rejected so
/// a crafted payload can never navigate the app somewhere unexpected.
///
///   /stock/TCS.NS, stock/TCS.NS, stockaipro://stock/TCS.NS  -> /stock/TCS.NS
///   /top-picks, stockaipro://top-picks                      -> /top-picks
///   push data {"route": "/stock/TCS.NS"} or {"symbol": "TCS.NS"}
/// </summary>
public static partial class DeepLinkRouter
{
    public const string Scheme = "stockaipro";

    private static readonly HashSet<string> StaticRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/", "/top-picks", "/notifications", "/watchlist", "/performance", "/intelligence", "/account",
        "/analyse", "/settings/notifications",
    };

    [GeneratedRegex(@"^[A-Za-z0-9&._\-^]{1,30}$")]
    private static partial Regex SymbolPattern();

    /// <summary>Normalised in-app route, or null if the input is not an
    /// allowed destination.</summary>
    public static string? ToRoute(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var value = input.Trim();

        if (value.Contains("://", StringComparison.Ordinal))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
                return null;
            // stockaipro://stock/TCS.NS -> host "stock", path "/TCS.NS"
            value = "/" + uri.Host + uri.AbsolutePath;
        }

        var q = value.IndexOfAny(['?', '#']);
        if (q >= 0) value = value[..q];
        if (!value.StartsWith('/')) value = "/" + value;
        if (value.Length > 1) value = value.TrimEnd('/');
        if (value.Contains("..", StringComparison.Ordinal) || value.Contains('\\')) return null;

        if (StaticRoutes.Contains(value)) return value.ToLowerInvariant();

        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && string.Equals(parts[0], "stock", StringComparison.OrdinalIgnoreCase))
        {
            var symbol = Uri.UnescapeDataString(parts[1]);
            return SymbolPattern().IsMatch(symbol) ? $"/stock/{Uri.EscapeDataString(symbol.ToUpperInvariant())}" : null;
        }
        return null;
    }

    /// <summary>Route for a push notification's data payload.</summary>
    public static string FromPushData(IReadOnlyDictionary<string, string?> data)
    {
        if (data.TryGetValue("route", out var route) && ToRoute(route) is { } r) return r;
        if (data.TryGetValue("symbol", out var symbol) && !string.IsNullOrWhiteSpace(symbol) &&
            ToRoute($"/stock/{symbol}") is { } s) return s;
        return "/notifications";
    }

    /// <summary>The notification id carried by a push payload, if any.</summary>
    public static int? NotificationId(IReadOnlyDictionary<string, string?> data) =>
        data.TryGetValue("notification_id", out var v) && int.TryParse(v, out var id) ? id : null;

    /// <summary>Routes that can be shown without signing in.</summary>
    public static bool IsPublic(string route) => route == "/";
}
