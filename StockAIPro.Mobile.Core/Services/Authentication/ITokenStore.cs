namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// Persists the access/refresh token pair. The only implementation shipped
/// (SecureTokenStore) uses MAUI's platform secure storage (Android
/// Keystore-backed, etc.) - never plain files, preferences, or app state.
/// Abstracted as an interface so Services/Authentication's actual logic
/// (AuthService, AuthenticatedHttpMessageHandler) can be unit tested with
/// an in-memory fake instead of a real platform-backed store.
/// </summary>
public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task ClearAsync();
}
