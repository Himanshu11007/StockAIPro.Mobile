using StockAIPro.Mobile.Models.Notifications;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Authentication;

namespace StockAIPro.Mobile.Services.Notifications;

public enum PushPermission { Unknown, Granted, Denied }

/// <summary>
/// Platform push integration (Android: Firebase Cloud Messaging; iOS: APNs),
/// implemented in the MAUI project. Platform is null where push is not
/// supported (e.g. Windows). IsConfigured is false when the build carries no
/// push configuration (no Firebase config on Android).
/// </summary>
public interface IPushTokenProvider
{
    string? Platform { get; }
    bool IsConfigured { get; }
    string? AppVersion { get; }
    Task<PushPermission> GetPermissionAsync();
    Task<PushPermission> RequestPermissionAsync();
    /// <summary>Current push token, or null if unavailable.</summary>
    Task<string?> GetTokenAsync();
    /// <summary>Raised when the platform issues a new token.</summary>
    event Action<string>? TokenRefreshed;
}

public enum PushRegistrationStatus { NotAttempted, Registered, PermissionDenied, NotConfigured, Unsupported, Failed }

public sealed record PushRegistrationResult(PushRegistrationStatus Status, string Message);

/// <summary>
/// Registers this installation with the backend (POST /devices) after
/// sign-in / session restore and whenever the platform rotates the token.
/// The device is always registered (with permission state), even without a
/// token, so the backend knows why no push can be delivered. Sign-out
/// removes the device on the backend (POST /auth/logout does it from the
/// session's device id). Never throws.
/// </summary>
public sealed class PushRegistrationService : IDisposable
{
    private readonly INotificationApiClient _api;
    private readonly IDeviceIdentityService _device;
    private readonly IPushTokenProvider _push;
    private readonly IAuthService _auth;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public PushRegistrationService(INotificationApiClient api, IDeviceIdentityService device, IPushTokenProvider push,
                                   IAuthService auth)
    {
        _api = api;
        _device = device;
        _push = push;
        _auth = auth;
        _push.TokenRefreshed += OnTokenRefreshed;
        _auth.AuthStateChanged += OnAuthStateChanged;
    }

    public PushRegistrationResult LastResult { get; private set; } =
        new(PushRegistrationStatus.NotAttempted, "Push notifications have not been set up on this device yet.");

    public event Action? Changed;

    /// <summary>Register with the backend. Asks the OS for notification
    /// permission only when requestPermission is true (an explicit user
    /// action), never silently on startup.</summary>
    public async Task<PushRegistrationResult> RegisterAsync(bool requestPermission, CancellationToken ct = default)
    {
        if (!_auth.IsAuthenticated)
            return Set(new(PushRegistrationStatus.NotAttempted, "Sign in to receive notifications."));
        if (_push.Platform is null)
            return Set(new(PushRegistrationStatus.Unsupported,
                "Push notifications are not available on this platform. Your notifications are always in the Notification Center."));

        await _gate.WaitAsync(ct);
        try
        {
            var permission = requestPermission ? await _push.RequestPermissionAsync() : await _push.GetPermissionAsync();
            string? token = null;
            if (_push.IsConfigured && permission != PushPermission.Denied)
                token = await _push.GetTokenAsync();

            await _api.RegisterDeviceAsync(new DeviceRegistrationRequest
            {
                DeviceId = await _device.GetDeviceIdAsync(),
                Platform = _push.Platform,
                PushToken = token,
                AppVersion = _push.AppVersion,
                Permission = permission switch
                {
                    PushPermission.Granted => "granted",
                    PushPermission.Denied => "denied",
                    _ => "unknown",
                },
            }, ct);

            if (!_push.IsConfigured)
                return Set(new(PushRegistrationStatus.NotConfigured,
                    "Push delivery is not configured in this app build. Notifications still appear in the Notification Center."));
            if (permission == PushPermission.Denied)
                return Set(new(PushRegistrationStatus.PermissionDenied,
                    "Notifications are turned off for StockAI Pro in your device settings. You can still see them in the Notification Center."));
            if (token is null)
                return Set(new(PushRegistrationStatus.Failed,
                    "This device did not provide a push token yet. Notifications still appear in the Notification Center."));
            return Set(new(PushRegistrationStatus.Registered, "This device will receive push notifications."));
        }
        catch (ApiException ex)
        {
            return Set(new(PushRegistrationStatus.Failed, ex.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Set(new(PushRegistrationStatus.Failed, "Push registration could not be completed. Please try again later."));
        }
        finally
        {
            _gate.Release();
        }
    }

    private PushRegistrationResult Set(PushRegistrationResult result)
    {
        LastResult = result;
        Changed?.Invoke();
        return result;
    }

    private void OnTokenRefreshed(string token)
    {
        if (_auth.IsAuthenticated)
            _ = RegisterAsync(requestPermission: false);
    }

    private void OnAuthStateChanged()
    {
        if (_auth.IsAuthenticated)
            _ = RegisterAsync(requestPermission: false);
        else
            Set(new(PushRegistrationStatus.NotAttempted, "Sign in to receive notifications."));
    }

    public void Dispose()
    {
        _push.TokenRefreshed -= OnTokenRefreshed;
        _auth.AuthStateChanged -= OnAuthStateChanged;
    }
}
