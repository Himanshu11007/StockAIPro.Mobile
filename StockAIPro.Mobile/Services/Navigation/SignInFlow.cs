using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Navigation;
using StockAIPro.Mobile.Services.Notifications;

namespace StockAIPro.Mobile.Services.Navigation;

/// <summary>
/// Where to go after signing in (password, OTP, registration, PIN unlock)
/// or after the session is restored at startup: a destination the user
/// asked for while signed out (a tapped notification / stockaipro:// link)
/// wins; otherwise Home, or the optional PIN set-up after a first sign-in.
/// Opening a notification's destination marks that notification as read.
/// </summary>
public sealed class SignInFlow
{
    private readonly PendingNavigationService _pending;
    private readonly INotificationApiClient _notifications;
    private readonly NotificationStateService _state;

    public SignInFlow(PendingNavigationService pending, INotificationApiClient notifications,
                      NotificationStateService state)
    {
        _pending = pending;
        _notifications = notifications;
        _state = state;
    }

    public string NextRoute(bool offerPinSetup)
    {
        if (TakePending(isAuthenticated: true) is { } route) return route;
        return offerPinSetup ? "/pin/create" : "/";
    }

    /// <summary>The pending destination if it can be shown now, else null.</summary>
    public string? TakePending(bool isAuthenticated)
    {
        var d = _pending.TryConsume(isAuthenticated);
        if (d is null) return null;
        if (d.NotificationId is { } id)
            _ = MarkReadAsync(id);
        return d.Route;
    }

    private async Task MarkReadAsync(int id)
    {
        try
        {
            await _notifications.MarkReadAsync(id);
            await _state.RefreshAsync();
        }
        catch (Exception)
        {
            // Read state is cosmetic; never block navigation on it.
        }
    }
}
