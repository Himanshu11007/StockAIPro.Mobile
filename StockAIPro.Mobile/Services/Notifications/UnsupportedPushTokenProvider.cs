using StockAIPro.Mobile.Services.Notifications;

namespace StockAIPro.Mobile.Services.Notifications;

/// <summary>Platforms without push support in this app (Windows, Mac
/// Catalyst): notifications remain available in the Notification Center.</summary>
public sealed class UnsupportedPushTokenProvider : IPushTokenProvider
{
    public string? Platform => null;
    public bool IsConfigured => false;
    public string? AppVersion => AppInfo.Current.VersionString;
    public Task<PushPermission> GetPermissionAsync() => Task.FromResult(PushPermission.Unknown);
    public Task<PushPermission> RequestPermissionAsync() => Task.FromResult(PushPermission.Unknown);
    public Task<string?> GetTokenAsync() => Task.FromResult<string?>(null);
#pragma warning disable CS0067 // never raised on unsupported platforms
    public event Action<string>? TokenRefreshed;
#pragma warning restore CS0067
}
