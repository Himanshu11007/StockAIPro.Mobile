using StockAIPro.Mobile.Models.Auth;
using StockAIPro.Mobile.Services.Authentication;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>Minimal IAuthService for services that only read the auth state
/// and listen to AuthStateChanged.</summary>
public sealed class FakeAuthService : IAuthService
{
    public bool IsAuthenticated { get; private set; }
    public UserProfileResponse? CurrentUser => null;
    public event Action? AuthStateChanged;

    public void SetAuthenticated(bool value)
    {
        IsAuthenticated = value;
        AuthStateChanged?.Invoke();
    }

    public Task RegisterAsync(string email, string password, CancellationToken ct = default) => throw new NotSupportedException();
    public Task LoginAsync(string email, string password, CancellationToken ct = default) => throw new NotSupportedException();
    public Task LogoutAsync(CancellationToken ct = default) { SetAuthenticated(false); return Task.CompletedTask; }
    public Task<UserProfileResponse> RefreshCurrentUserAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task<bool> TryRestoreSessionAsync(CancellationToken ct = default) => Task.FromResult(IsAuthenticated);
    public Task LoginWithGoogleAsync(string idToken, CancellationToken ct = default) => throw new NotSupportedException();
    public Task LoginWithAppleAsync(string identityToken, CancellationToken ct = default) => throw new NotSupportedException();
    public Task RequestOtpAsync(string destination, CancellationToken ct = default) => throw new NotSupportedException();
    public Task VerifyOtpAsync(string destination, string code, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<LinkedIdentityResponse> LinkGoogleAsync(string idToken, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<LinkedIdentityResponse> LinkAppleAsync(string identityToken, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<List<LinkedIdentityResponse>> GetLinkedIdentitiesAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task UnlinkIdentityAsync(string provider, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<string> GetDeviceIdAsync() => Task.FromResult("test-device");
    public Task<List<SessionResponse>> GetSessionsAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task RevokeSessionAsync(int sessionId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<int> SignOutAllDevicesAsync(bool exceptCurrent, CancellationToken ct = default) => throw new NotSupportedException();
    public Task SetPinEnabledAsync(bool enabled, CancellationToken ct = default) => throw new NotSupportedException();
}
