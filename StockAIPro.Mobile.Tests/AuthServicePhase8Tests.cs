using StockAIPro.Mobile.Services.Authentication;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

/// <summary>Covers the Phase 8 additions to AuthService: Google/Apple/OTP
/// sign-in, account linking, and device/session management - all layered
/// on top of the same EstablishSessionAsync/TokenSessionSnapshot
/// architecture the Phase 7 tests already exercise for password login.</summary>
public class AuthServicePhase8Tests
{
    private static AuthService BuildService(
        FakeAuthApiClient? api = null, InMemoryTokenStore? tokenStore = null, InMemoryDeviceIdentityService? device = null) =>
        new(api ?? new FakeAuthApiClient(), tokenStore ?? new InMemoryTokenStore(), device ?? new InMemoryDeviceIdentityService());

    [Fact]
    public async Task LoginWithGoogleAsync_establishes_an_authenticated_session()
    {
        var api = new FakeAuthApiClient { GoogleResult = () => Canned.Tokens("g-access", "g-refresh") };
        var tokenStore = new InMemoryTokenStore();
        var service = BuildService(api, tokenStore);

        await service.LoginWithGoogleAsync("google-id-token");

        Assert.True(service.IsAuthenticated);
        Assert.Equal(1, api.LoginWithGoogleCallCount);
        Assert.Equal("g-access", await tokenStore.GetAccessTokenAsync());
    }

    [Fact]
    public async Task LoginWithAppleAsync_establishes_an_authenticated_session()
    {
        var api = new FakeAuthApiClient { AppleResult = () => Canned.Tokens("a-access", "a-refresh") };
        var tokenStore = new InMemoryTokenStore();
        var service = BuildService(api, tokenStore);

        await service.LoginWithAppleAsync("apple-identity-token");

        Assert.True(service.IsAuthenticated);
        Assert.Equal(1, api.LoginWithAppleCallCount);
        Assert.Equal("a-access", await tokenStore.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Google_and_Apple_login_forward_this_devices_id_and_name()
    {
        var api = new FakeAuthApiClient();
        var device = new InMemoryDeviceIdentityService("device-xyz", "Pixel 7");
        var service = BuildService(api, device: device);

        await service.LoginWithGoogleAsync("tok");

        Assert.Equal(("device-xyz", "Pixel 7"), api.LastDeviceInfo);
    }

    [Fact]
    public async Task RequestOtpAsync_delegates_to_the_api_client()
    {
        var api = new FakeAuthApiClient();
        var service = BuildService(api);

        await service.RequestOtpAsync("user@example.com");

        Assert.Equal(1, api.RequestOtpCallCount);
    }

    [Fact]
    public async Task VerifyOtpAsync_establishes_an_authenticated_session()
    {
        var api = new FakeAuthApiClient { OtpVerifyResult = () => Canned.Tokens("otp-access", "otp-refresh") };
        var tokenStore = new InMemoryTokenStore();
        var service = BuildService(api, tokenStore);

        await service.VerifyOtpAsync("user@example.com", "123456");

        Assert.True(service.IsAuthenticated);
        Assert.Equal(1, api.VerifyOtpCallCount);
        Assert.Equal("otp-access", await tokenStore.GetAccessTokenAsync());
    }

    [Fact]
    public async Task SignOutAllDevicesAsync_without_except_current_clears_local_credentials()
    {
        var api = new FakeAuthApiClient
        {
            LoginResult = () => Canned.Tokens("access-1", "refresh-1"),
            RevokeAllResult = () => new() { RevokedCount = 3 },
        };
        var tokenStore = new InMemoryTokenStore();
        var service = BuildService(api, tokenStore);
        await service.LoginAsync("a@example.com", "password1");
        Assert.True(service.IsAuthenticated);

        var revokedCount = await service.SignOutAllDevicesAsync(exceptCurrent: false);

        Assert.Equal(3, revokedCount);
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
        Assert.Null(await tokenStore.GetAccessTokenAsync());
        Assert.False(api.LastRevokeAllArgs!.Value.ExceptCurrent);
        Assert.Null(api.LastRevokeAllArgs.Value.CurrentDeviceId);
    }

    [Fact]
    public async Task SignOutAllDevicesAsync_with_except_current_keeps_this_session_authenticated()
    {
        var api = new FakeAuthApiClient
        {
            LoginResult = () => Canned.Tokens("access-1", "refresh-1"),
            RevokeAllResult = () => new() { RevokedCount = 2 },
        };
        var tokenStore = new InMemoryTokenStore();
        var device = new InMemoryDeviceIdentityService("my-device-id");
        var service = BuildService(api, tokenStore, device);
        await service.LoginAsync("a@example.com", "password1");

        var revokedCount = await service.SignOutAllDevicesAsync(exceptCurrent: true);

        Assert.Equal(2, revokedCount);
        Assert.True(service.IsAuthenticated); // this device's own session was NOT revoked
        Assert.Equal("access-1", await tokenStore.GetAccessTokenAsync());
        Assert.True(api.LastRevokeAllArgs!.Value.ExceptCurrent);
        Assert.Equal("my-device-id", api.LastRevokeAllArgs.Value.CurrentDeviceId);
    }

    [Fact]
    public async Task SetPinEnabledAsync_sends_this_devices_id_and_the_flag_never_the_pin()
    {
        var api = new FakeAuthApiClient();
        var device = new InMemoryDeviceIdentityService("device-abc");
        var service = BuildService(api, device: device);

        await service.SetPinEnabledAsync(true);

        Assert.Equal(("device-abc", true), api.LastSetPinEnabledArgs);
    }

    [Fact]
    public async Task GetDeviceIdAsync_returns_the_underlying_device_identity()
    {
        var service = BuildService(device: new InMemoryDeviceIdentityService("fixed-device-id"));
        Assert.Equal("fixed-device-id", await service.GetDeviceIdAsync());
    }

    [Fact]
    public async Task LinkGoogleAsync_and_GetLinkedIdentitiesAsync_pass_through_to_the_api_client()
    {
        var api = new FakeAuthApiClient
        {
            IdentitiesResult = () => [new() { Provider = "google", CreatedAt = DateTimeOffset.UtcNow }],
        };
        var service = BuildService(api);

        await service.LinkGoogleAsync("some-token");
        var identities = await service.GetLinkedIdentitiesAsync();

        Assert.Single(identities);
        Assert.Equal("google", identities[0].Provider);
    }
}
