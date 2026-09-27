namespace StockAIPro.Mobile.Services.Configuration;

/// <summary>
/// Single source of truth for the backend API base URL. Nothing else in the
/// app should hardcode a URL - every HTTP client is configured from here.
///
/// Development only: the FastAPI backend (stock-prediction-app) runs on the
/// developer PC via `uvicorn api.main:app --reload`, listening on
/// http://127.0.0.1:8000. The Android emulator cannot reach the host
/// machine via 127.0.0.1 (that resolves to the emulator itself) - Android's
/// special-purpose alias for the host loopback is 10.0.2.2. Other platforms
/// (Windows/iOS simulator/MacCatalyst) reach the dev machine directly via
/// 127.0.0.1.
///
/// Uses a RUNTIME check (OperatingSystem.IsAndroid()), not a compile-time
/// "#if ANDROID" symbol: this class lives in StockAIPro.Mobile.Core, a
/// plain net10.0 library (see that project's own comment for why -
/// testability), so it is never compiled with the ANDROID target framework
/// and a "#if ANDROID" here would always evaluate false, even when actually
/// running on Android. This was caught during Phase 7A's own Android
/// emulator test: registration failed with a network error because the
/// client was silently pointed at 127.0.0.1 (itself) instead of 10.0.2.2.
/// </summary>
public static class ApiConfiguration
{
    private const string DevPort = "8000";

    private static string DevHost => OperatingSystem.IsAndroid() ? "10.0.2.2" : "127.0.0.1";

    /// <summary>
    /// Active base URL. Only a Development value exists yet - Staging/
    /// Production are not configured because no such infrastructure exists
    /// yet (see Phase 7A scope: do not hardcode production infrastructure).
    /// </summary>
    public static string BaseUrl { get; } = $"http://{DevHost}:{DevPort}";

    public const string ApiPrefix = "/api/v1";

    /// <summary>Named HttpClient with no auth handler attached - used only
    /// for the token-issuing calls (register/login/refresh/logout), which
    /// must never carry a stale/expired Authorization header.</summary>
    public const string RawClientName = "AuthRawClient";

    /// <summary>Named HttpClient with AuthenticatedHttpMessageHandler
    /// attached - attaches the current access token automatically and
    /// refreshes-and-retries once on a 401. Used for /auth/me and, in later
    /// phases, every other authenticated endpoint.</summary>
    public const string AuthenticatedClientName = "AuthenticatedClient";
}
