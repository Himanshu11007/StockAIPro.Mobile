namespace StockAIPro.Mobile.Models.TopPicks;

/// <summary>
/// Mirrors config.py:CATEGORIES exactly - the backend rejects any other
/// value with a 400 (see api/services.py:start_top_picks_scan). There is no
/// endpoint to fetch this list dynamically, so it is duplicated here
/// deliberately; if the backend's CATEGORIES ever changes, this must be
/// updated to match.
/// </summary>
public static class StockCategory
{
    public const string LargeCap = "Large Cap";
    public const string MidCap = "Mid Cap";
    public const string SmallCap = "Small Cap";

    public static readonly IReadOnlyList<string> All = [LargeCap, MidCap, SmallCap];
}
