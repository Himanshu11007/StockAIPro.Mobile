namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// IDeviceIdentityService backed by Microsoft.Maui.Storage.SecureStorage -
/// same mechanism (and same reinstall-wipes-it guarantee) as SecureTokenStore.
/// </summary>
public sealed class DeviceIdentityService : IDeviceIdentityService
{
    private const string DeviceIdKey = "stockaipro_device_id";

    // Guards the read-check-then-write below so two concurrent first-ever
    // callers (e.g. app startup racing a background refresh) can't each
    // generate and persist a different id.
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetDeviceIdAsync()
    {
        var existing = await SecureStorage.Default.GetAsync(DeviceIdKey);
        if (!string.IsNullOrEmpty(existing))
            return existing;

        await _lock.WaitAsync();
        try
        {
            existing = await SecureStorage.Default.GetAsync(DeviceIdKey);
            if (!string.IsNullOrEmpty(existing))
                return existing;

            var newId = Guid.NewGuid().ToString("N");
            await SecureStorage.Default.SetAsync(DeviceIdKey, newId);
            return newId;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<string?> GetDeviceNameAsync()
    {
        var label = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();
        return Task.FromResult<string?>(string.IsNullOrWhiteSpace(label) ? null : label);
    }
}
