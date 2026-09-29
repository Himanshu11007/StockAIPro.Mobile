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

    public Task<string?> GetAccessTokenAsync() => SecureStorage.Default.GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(RefreshTokenKey);

    public Task<string?> GetSessionIdAsync() => SecureStorage.Default.GetAsync(SessionIdKey);

    public async Task SaveTokensAsync(string accessToken, string refreshToken, bool isNewSession = false)
    {
        if (isNewSession)
            await SecureStorage.Default.SetAsync(SessionIdKey, Guid.NewGuid().ToString("N"));

        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(SessionIdKey);
        return Task.CompletedTask;
    }
}
