using StockAIPro.Mobile.Models.Stocks;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// GET /api/v1/stocks and GET /api/v1/stocks/{symbol} - the authoritative
/// stock master for search/selection. Never maintain a local stock list;
/// this is the only source of truth for symbols, names, and which stocks
/// are currently active (see api/routes/stocks.py).
/// </summary>
public interface IStockApiClient
{
    /// <summary>
    /// Search active stocks by symbol or company name (backend matches
    /// either - see stocks/service.py:search_stocks). Returns an empty list
    /// for no matches, never null.
    /// </summary>
    Task<List<StockSearchResult>> SearchAsync(
        string? search = null, int limit = 50, int offset = 0, CancellationToken ct = default);

    /// <summary>Returns null if the symbol doesn't exist or is inactive
    /// (backend returns 404 for both - see stocks/service.py:get_stock).</summary>
    Task<StockSearchResult?> GetAsync(string symbol, CancellationToken ct = default);
}
