namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// ITokenStore backed by Microsoft.Maui.Storage.SecureStorage - on Android
/// this is backed by the Android Keystore; equivalent platform-secure
/// mechanisms are used on iOS/Windows/MacCatalyst. Never store tokens any
/// other way (no Preferences, no plain files, no app/component state).
/// </summary>
public sealed class SecureTokenStore : ITokenStore
{
    private const string AccessTokenKey = "stockaipro_access_token";
    private const string RefreshTokenKey = "stockaipro_refresh_token";
    private const string SessionIdKey = "stockaipro_session_id";

    // Guards SaveTokensAsync/ClearAsync/GetSnapshotAsync so a snapshot read
    // can never observe a torn combination of fields mid-write. Registered
    // as a singleton, so one lock instance covers the whole app.
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Task<string?> GetAccessTokenAsync() => SecureStorage.Default.GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(RefreshTokenKey);

    public Task<string?> GetSessionIdAsync() => SecureStorage.Default.GetAsync(SessionIdKey);

    public async Task<TokenSessionSnapshot> GetSnapshotAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            var sessionId = await SecureStorage.Default.GetAsync(SessionIdKey);
            return new TokenSessionSnapshot(accessToken, refreshToken, sessionId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken, bool isNewSession = false)
    {
        await _lock.WaitAsync();
        try
        {
            if (isNewSession)
                await SecureStorage.Default.SetAsync(SessionIdKey, Guid.NewGuid().ToString("N"));

            await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAsync()
    {
        await _lock.WaitAsync();
        try
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(SessionIdKey);
        }
        finally
        {
            _lock.Release();
        }
    }
}
