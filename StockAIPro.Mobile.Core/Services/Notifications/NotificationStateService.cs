using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Authentication;

namespace StockAIPro.Mobile.Services.Notifications;

/// <summary>
/// The unread-notification badge. Refreshed on sign-in, when the app
/// returns to the foreground, when a push arrives, and after the user reads
/// notifications. Failures keep the last known count (never throws).
/// </summary>
public sealed class NotificationStateService : IDisposable
{
    private readonly INotificationApiClient _api;
    private readonly IAuthService _auth;

    public NotificationStateService(INotificationApiClient api, IAuthService auth)
    {
        _api = api;
        _auth = auth;
        _auth.AuthStateChanged += OnAuthStateChanged;
    }

    public int UnreadCount { get; private set; }

    public event Action? Changed;

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!_auth.IsAuthenticated)
        {
            SetCount(0);
            return;
        }
        try
        {
            SetCount(await _api.GetUnreadCountAsync(ct));
        }
        catch (ApiException)
        {
            // keep the last known value
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }
    }

    public void SetCount(int count)
    {
        count = Math.Max(0, count);
        if (count == UnreadCount) return;
        UnreadCount = count;
        Changed?.Invoke();
    }

    private void OnAuthStateChanged() => _ = RefreshAsync();

    public void Dispose() => _auth.AuthStateChanged -= OnAuthStateChanged;
}
