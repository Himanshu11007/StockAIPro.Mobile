using StockAIPro.Mobile.Models.Watchlist;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// GET/POST/DELETE /api/v1/watchlist - the authenticated user's own
/// watchlist. Ownership is always derived server-side from the access
/// token; nothing here ever sends a user_id (see api/routes/watchlist.py).
/// </summary>
public interface IWatchlistApiClient
{
    Task<List<WatchlistItem>> GetMyWatchlistAsync(CancellationToken ct = default);

    /// <summary>Throws ApiException with:
    ///   Kind == ValidationFailed (400) for an unknown/inactive symbol,
    ///   Kind == Conflict (409) if the symbol is already on the watchlist.
    /// See watchlist/service.py's UnknownSymbolError/DuplicateWatchlistItemError,
    /// mapped to those status codes in api/routes/watchlist.py.</summary>
    Task<WatchlistItem> AddAsync(WatchlistAddRequest request, CancellationToken ct = default);

    /// <summary>Returns false if the item doesn't exist or belongs to
    /// another user - the backend deliberately doesn't distinguish the two
    /// (see watchlist/service.py:remove_watchlist_item), so a caller can't
    /// probe for another user's item ids.</summary>
    Task<bool> RemoveAsync(int itemId, CancellationToken ct = default);
}
