using StockAIPro.Mobile.Models.Auth;
using StockAIPro.Mobile.Services.Api;

namespace StockAIPro.Mobile.Tests.Fakes;

/// <summary>Configurable IAuthApiClient test double - records calls and lets
/// each method's behavior be swapped per test via a delegate.</summary>
public sealed class FakeAuthApiClient : IAuthApiClient
{
    public int RegisterCallCount { get; private set; }
    public int LoginCallCount { get; private set; }
    public int RefreshCallCount { get; private set; }
    public int LogoutCallCount { get; private set; }
    public int GetCurrentUserCallCount { get; private set; }

    public List<string> RefreshTokensUsed { get; } = [];
    public List<string> LogoutTokensUsed { get; } = [];
    public (string Email, string Password)? LastLoginArgs { get; private set; }
    public (string Email, string Password)? LastRegisterArgs { get; private set; }
    public (string? DeviceId, string? DeviceName)? LastDeviceInfo { get; private set; }

    public Func<TokenResponse> RegisterResult { get; set; } = () => Canned.Tokens();
    public Func<TokenResponse> LoginResult { get; set; } = () => Canned.Tokens();
    public Func<TokenResponse> RefreshResult { get; set; } = () => Canned.Tokens();
    public Func<UserProfileResponse> CurrentUserResult { get; set; } = () => Canned.Profile();
    public Func<TokenResponse> GoogleResult { get; set; } = () => Canned.Tokens();
    public Func<TokenResponse> AppleResult { get; set; } = () => Canned.Tokens();
    public Func<TokenResponse> OtpVerifyResult { get; set; } = () => Canned.Tokens();
    public Func<LinkedIdentityResponse> LinkResult { get; set; } = () => new LinkedIdentityResponse { Provider = "google", CreatedAt = DateTimeOffset.UtcNow };
    public Func<List<LinkedIdentityResponse>> IdentitiesResult { get; set; } = () => [];
    public Func<List<SessionResponse>> SessionsResult { get; set; } = () => [];
    public Func<RevokeAllSessionsResponse> RevokeAllResult { get; set; } = () => new RevokeAllSessionsResponse { RevokedCount = 0 };

    public int LoginWithGoogleCallCount { get; private set; }
    public int LoginWithAppleCallCount { get; private set; }
    public int RequestOtpCallCount { get; private set; }
    public int VerifyOtpCallCount { get; private set; }
    public int RevokeAllSessionsCallCount { get; private set; }
    public (bool ExceptCurrent, string? CurrentDeviceId)? LastRevokeAllArgs { get; private set; }
    public (string DeviceId, bool Enabled)? LastSetPinEnabledArgs { get; private set; }

    public Task<TokenResponse> RegisterAsync(
        string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default)
    {
        RegisterCallCount++;
        LastRegisterArgs = (email, password);
        LastDeviceInfo = (deviceId, deviceName);
        return Task.FromResult(RegisterResult());
    }

    public Task<TokenResponse> LoginAsync(
        string email, string password, string? deviceId = null, string? deviceName = null, CancellationToken ct = default)
    {
        LoginCallCount++;
        LastLoginArgs = (email, password);
        LastDeviceInfo = (deviceId, deviceName);
        return Task.FromResult(LoginResult());
    }

    public Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        RefreshCallCount++;
        RefreshTokensUsed.Add(refreshToken);
        return Task.FromResult(RefreshResult());
    }

    public Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        LogoutCallCount++;
        LogoutTokensUsed.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken ct = default)
    {
        GetCurrentUserCallCount++;
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(CurrentUserResult());
    }

    public Task<TokenResponse> LoginWithGoogleAsync(string idToken, string? deviceId, string? deviceName, CancellationToken ct = default)
    {
        LoginWithGoogleCallCount++;
        LastDeviceInfo = (deviceId, deviceName);
        return Task.FromResult(GoogleResult());
    }

    public Task<TokenResponse> LoginWithAppleAsync(string identityToken, string? deviceId, string? deviceName, CancellationToken ct = default)
    {
        LoginWithAppleCallCount++;
        LastDeviceInfo = (deviceId, deviceName);
        return Task.FromResult(AppleResult());
    }

    public Task RequestOtpAsync(string destination, CancellationToken ct = default)
    {
        RequestOtpCallCount++;
        return Task.CompletedTask;
    }

    public Task<TokenResponse> VerifyOtpAsync(string destination, string code, string? deviceId, string? deviceName, CancellationToken ct = default)
    {
        VerifyOtpCallCount++;
        LastDeviceInfo = (deviceId, deviceName);
        return Task.FromResult(OtpVerifyResult());
    }

    public Task<LinkedIdentityResponse> LinkGoogleAsync(string idToken, CancellationToken ct = default) =>
        Task.FromResult(LinkResult());

    public Task<LinkedIdentityResponse> LinkAppleAsync(string identityToken, CancellationToken ct = default) =>
        Task.FromResult(LinkResult());

    public Task<List<LinkedIdentityResponse>> GetLinkedIdentitiesAsync(CancellationToken ct = default) =>
        Task.FromResult(IdentitiesResult());

    public Task UnlinkIdentityAsync(string provider, CancellationToken ct = default) => Task.CompletedTask;

    public Task<List<SessionResponse>> GetSessionsAsync(CancellationToken ct = default) =>
        Task.FromResult(SessionsResult());

    public Task RevokeSessionAsync(int sessionId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<RevokeAllSessionsResponse> RevokeAllSessionsAsync(bool exceptCurrent, string? currentDeviceId, CancellationToken ct = default)
    {
        RevokeAllSessionsCallCount++;
        LastRevokeAllArgs = (exceptCurrent, currentDeviceId);
        return Task.FromResult(RevokeAllResult());
    }

    public Task SetPinEnabledAsync(string deviceId, bool enabled, CancellationToken ct = default)
    {
        LastSetPinEnabledArgs = (deviceId, enabled);
        return Task.CompletedTask;
    }
}

public static class Canned
{
    public static TokenResponse Tokens(string accessToken = "access-token", string refreshToken = "refresh-token") =>
        new() { AccessToken = accessToken, RefreshToken = refreshToken, TokenType = "bearer" };

    public static UserProfileResponse Profile(string email = "user@example.com") => new()
    {
        Id = 1,
        Email = email,
        IsActive = true,
        Roles = ["USER"],
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
