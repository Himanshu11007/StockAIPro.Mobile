using Foundation;
using StockAIPro.Mobile.Services.Navigation;
using StockAIPro.Mobile.Services.Notifications;
using UIKit;
using UserNotifications;

namespace StockAIPro.Mobile.Platforms.iOS.Push;

/// <summary>
/// APNs registration. The backend sends to APNs directly (token-based
/// authentication), so the app only needs the push entitlement
/// (aps-environment, Entitlements.plist) and a provisioning profile with
/// Push Notifications enabled. See docs/NOTIFICATIONS.md.
/// </summary>
public sealed class IosPushTokenProvider : IPushTokenProvider
{
    public static IosPushTokenProvider? Instance { get; private set; }
    private TaskCompletionSource<string?>? _pending;
    private string? _token;

    public IosPushTokenProvider() => Instance = this;

    public string? Platform => "ios";
    public bool IsConfigured => true;
    public string? AppVersion => AppInfo.Current.VersionString;
    public event Action<string>? TokenRefreshed;

    public async Task<PushPermission> GetPermissionAsync()
    {
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
        return settings.AuthorizationStatus switch
        {
            UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional or UNAuthorizationStatus.Ephemeral => PushPermission.Granted,
            UNAuthorizationStatus.Denied => PushPermission.Denied,
            _ => PushPermission.Unknown,
        };
    }

    public async Task<PushPermission> RequestPermissionAsync()
    {
        var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound);
        return granted ? PushPermission.Granted : await GetPermissionAsync();
    }

    public async Task<string?> GetTokenAsync()
    {
        if (await GetPermissionAsync() != PushPermission.Granted) return _token;
        _pending = new TaskCompletionSource<string?>();
        await MainThread.InvokeOnMainThreadAsync(() => UIApplication.SharedApplication.RegisterForRemoteNotifications());
        var done = await Task.WhenAny(_pending.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        return done == _pending.Task ? _pending.Task.Result : _token;
    }

    internal void OnToken(NSData deviceToken)
    {
        var token = Convert.ToHexString(deviceToken.ToArray()).ToLowerInvariant();
        var changed = _token is not null && _token != token;
        _token = token;
        _pending?.TrySetResult(token);
        if (changed) TokenRefreshed?.Invoke(token);
    }

    internal void OnTokenFailed() => _pending?.TrySetResult(null);
}

/// <summary>Shows notifications while the app is open and routes taps to
/// the related screen.</summary>
public sealed class NotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
                                                 Action<UNNotificationPresentationOptions> completionHandler)
    {
        _ = IPlatformApplication.Current?.Services.GetService<NotificationStateService>()?.RefreshAsync();
        completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.List |
                          UNNotificationPresentationOptions.Sound);
    }

    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response,
                                                        Action completionHandler)
    {
        var info = response.Notification.Request.Content.UserInfo;
        string? Get(string key) => info.TryGetValue(new NSString(key), out var v) ? v?.ToString() : null;
        var data = new Dictionary<string, string?>
        {
            ["route"] = Get("route"), ["notification_id"] = Get("notification_id"), ["symbol"] = Get("symbol"),
        };
        IPlatformApplication.Current?.Services.GetService<PendingNavigationService>()?.RequestFromPush(data);
        completionHandler();
    }
}
