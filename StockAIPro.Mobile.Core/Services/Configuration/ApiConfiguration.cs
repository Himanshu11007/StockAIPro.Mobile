namespace StockAIPro.Mobile.Services.Configuration;

public enum ApiEnvironment
{
    /// <summary>No URL configured: local FastAPI on the developer machine
    /// (10.0.2.2 from the Android emulator, 127.0.0.1 elsewhere).</summary>
    Development,

    /// <summary>URL supplied at build time (StockAIProApiBaseUrl), e.g. a
    /// physical device's LAN/tunnel URL or the production HTTPS API.</summary>
    Configured,
}

/// <summary>
/// Single source of truth for the backend API base URL. Nothing else in the
/// app should hardcode a URL - every HTTP client is configured from here.
///
/// Resolution (see docs/MOBILE_SETUP.md in the backend repository):
///   1. A URL configured at build time via the MSBuild property
///      StockAIProApiBaseUrl (emitted as assembly metadata and passed to
///      Initialize by MauiProgram). Release builds REQUIRE it and require
///      HTTPS - the Release build fails without it (csproj target
///      ValidateApiBaseUrl), so a developer IP can never ship.
///   2. Otherwise (Debug only) the local development backend:
///      http://10.0.2.2:8000 on Android (the emulator's alias for the host
///      loopback) and http://127.0.0.1:8000 elsewhere.
///
/// Uses a RUNTIME check (OperatingSystem.IsAndroid()), not a compile-time
/// "#if ANDROID" symbol: this class lives in StockAIPro.Mobile.Core, a plain
/// net10.0 library, so "#if ANDROID" would always be false here even when
/// running on Android (a real bug caught during Phase 7A's emulator test).
/// </summary>
public static class ApiConfiguration
{
    public const string DevPort = "8000";

    public static string DevelopmentBaseUrl =>
        $"http://{(OperatingSystem.IsAndroid() ? "10.0.2.2" : "127.0.0.1")}:{DevPort}";

    /// <summary>Active base URL (scheme://host[:port], no trailing slash).</summary>
    public static string BaseUrl { get; private set; } = DevelopmentBaseUrl;

    public static ApiEnvironment Environment { get; private set; } = ApiEnvironment.Development;

    public const string ApiPrefix = "/api/v1";

    /// <summary>Named HttpClient with no auth handler attached - used only
    /// for the token-issuing calls (register/login/refresh/logout), which
    /// must never carry a stale/expired Authorization header, and for public
    /// endpoints such as /app/config.</summary>
    public const string RawClientName = "AuthRawClient";

    /// <summary>Named HttpClient with AuthenticatedHttpMessageHandler
    /// attached - attaches the current access token automatically and
    /// refreshes-and-retries once on a 401.</summary>
    public const string AuthenticatedClientName = "AuthenticatedClient";

    /// <summary>Same as AuthenticatedClientName but with a longer timeout,
    /// for the one synchronous long-running call (on-demand stock analysis,
    /// which fetches provider data and can take 10-30 seconds).</summary>
    public const string AuthenticatedLongRunningClientName = "AuthenticatedLongRunningClient";

    /// <summary>Apply the build-time configuration once, before any
    /// HttpClient is created.</summary>
    public static void Initialize(string? configuredBaseUrl, bool isReleaseBuild)
    {
        var (url, env) = Resolve(configuredBaseUrl, isReleaseBuild, DevelopmentBaseUrl);
        BaseUrl = url;
        Environment = env;
    }

    /// <summary>Pure resolution logic (unit tested).</summary>
    public static (string BaseUrl, ApiEnvironment Environment) Resolve(
        string? configuredBaseUrl, bool isReleaseBuild, string developmentBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            if (isReleaseBuild)
                throw new InvalidOperationException(
                    "Release builds require an API URL: build with -p:StockAIProApiBaseUrl=https://your-api-host");
            return (developmentBaseUrl, ApiEnvironment.Development);
        }

        var trimmed = configuredBaseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.Query) || (uri.AbsolutePath != "/" && uri.AbsolutePath != ""))
        {
            throw new ArgumentException(
                $"StockAIProApiBaseUrl must be an absolute http(s) URL with no path or query (got '{configuredBaseUrl}').");
        }
        if (isReleaseBuild && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Release builds require an HTTPS API URL.");

        return ($"{uri.Scheme}://{uri.Authority}", ApiEnvironment.Configured);
    }
}
