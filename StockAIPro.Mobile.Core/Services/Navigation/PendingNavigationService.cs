namespace StockAIPro.Mobile.Services.Navigation;

/// <summary>
/// Holds a destination requested from outside the normal UI flow (tapping a
/// push notification, opening a stockaipro:// link) until the app can show
/// it. If the user is signed out the destination is kept and opened right
/// after a successful sign-in, so a notification tap is never lost.
/// </summary>
public sealed class PendingNavigationService
{
    private readonly object _lock = new();
    private PendingDestination? _pending;

    /// <summary>Raised when a new destination is requested (UI thread
    /// marshalling is the subscriber's job).</summary>
    public event Action? DestinationRequested;

    public PendingDestination? Peek()
    {
        lock (_lock) return _pending;
    }

    /// <summary>Request navigation to an untrusted route string; ignored
    /// (returns false) unless DeepLinkRouter accepts it.</summary>
    public bool Request(string? route, int? notificationId = null)
    {
        var safe = DeepLinkRouter.ToRoute(route);
        if (safe is null) return false;
        lock (_lock) _pending = new PendingDestination(safe, notificationId);
        DestinationRequested?.Invoke();
        return true;
    }

    public bool RequestFromPush(IReadOnlyDictionary<string, string?> data) =>
        Request(DeepLinkRouter.FromPushData(data), DeepLinkRouter.NotificationId(data));

    /// <summary>Returns and clears the pending destination if it can be shown
    /// now (signed in, or a public route); otherwise keeps it and returns null.</summary>
    public PendingDestination? TryConsume(bool isAuthenticated)
    {
        lock (_lock)
        {
            if (_pending is null) return null;
            if (!isAuthenticated && !DeepLinkRouter.IsPublic(_pending.Route)) return null;
            var p = _pending;
            _pending = null;
            return p;
        }
    }

    public void Clear()
    {
        lock (_lock) _pending = null;
    }
}

public sealed record PendingDestination(string Route, int? NotificationId);
