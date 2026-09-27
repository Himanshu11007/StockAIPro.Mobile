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

    public Task<string?> GetAccessTokenAsync() => SecureStorage.Default.GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(RefreshTokenKey);

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }
}
