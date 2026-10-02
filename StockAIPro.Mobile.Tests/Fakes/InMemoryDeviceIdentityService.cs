using StockAIPro.Mobile.Services.Authentication;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>In-memory IDeviceIdentityService for tests - the real
/// DeviceIdentityService wraps MAUI SecureStorage/DeviceInfo, which need a
/// platform context unavailable in a plain unit test.</summary>
public sealed class InMemoryDeviceIdentityService : IDeviceIdentityService
{
    private readonly string _deviceId;
    private readonly string? _deviceName;

    public InMemoryDeviceIdentityService(string deviceId = "test-device", string? deviceName = "Test Device")
    {
        _deviceId = deviceId;
        _deviceName = deviceName;
    }

    public Task<string> GetDeviceIdAsync() => Task.FromResult(_deviceId);

    public Task<string?> GetDeviceNameAsync() => Task.FromResult(_deviceName);
}
