namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// A stable identifier for this app installation - NOT a credential, NOT
/// derived from sensitive hardware identifiers (IMEI, serial number, etc.),
/// and never used as a password. Exists purely so the backend can tell
/// sessions on different devices/installations apart for device/session
/// management ("list my devices", "sign out this device", "sign out all
/// devices" - see auth/device_service.py on the backend) and so the mobile
/// client can scope its local PIN to "this specific installation."
///
/// The one implementation shipped (DeviceIdentityService) persists the id
/// in the same platform secure storage SecureTokenStore uses for tokens -
/// it is regenerated automatically after an app data reset/reinstall for
/// exactly the same reason stored tokens are: that storage is wiped too.
/// </summary>
public interface IDeviceIdentityService
{
    Task<string> GetDeviceIdAsync();

    /// <summary>Best-effort human-readable label (e.g. "Google Pixel 7") for
    /// display in a device/session list - never relied on for identity or
    /// security, purely cosmetic. May return null if unavailable.</summary>
    Task<string?> GetDeviceNameAsync();
}
