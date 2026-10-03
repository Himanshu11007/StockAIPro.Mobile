using System.Reflection;
using Microsoft.Extensions.Logging;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Authentication;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            ConfigureApiBaseUrl();
            RegisterApiAndAuthServices(builder.Services);

            return builder.Build();
        }

        /// <summary>Applies the build-time API URL (MSBuild property
        /// StockAIProApiBaseUrl, emitted as assembly metadata by the csproj)
        /// - see ApiConfiguration for the resolution rules.</summary>
        private static void ConfigureApiBaseUrl()
        {
            var configured = typeof(MauiProgram).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "StockAIProApiBaseUrl")?.Value;
#if DEBUG
            const bool isRelease = false;
#else
            const bool isRelease = true;
#endif
            ApiConfiguration.Initialize(configured, isRelease);
        }

        private static void RegisterApiAndAuthServices(IServiceCollection services)
        {
            services.AddSingleton<ITokenStore, SecureTokenStore>();
            services.AddSingleton<IAuthApiClient, AuthApiClient>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IDeviceIdentityService, DeviceIdentityService>();
            // IPinStore is a singleton for the same reason ITokenStore is -
            // see SecureTokenStore's own comment: its internal lock only
            // protects against concurrent writes if exactly one instance
            // exists app-wide.
            services.AddSingleton<IPinStore, SecurePinStore>();
            services.AddSingleton<IPinService, PinService>();
            // Both fail closed (see GoogleSignInNotConfiguredException /
            // AppleSignInNotConfiguredException) until real native SDK
            // wiring + provider credentials are added - see
            // docs/AUTHENTICATION.md "Google setup" / "Apple setup". Safe to
            // register regardless: nothing calls SignInAsync() except a user
            // tapping the corresponding button.
            services.AddSingleton<IGoogleSignInService, GoogleSignInService>();
            services.AddSingleton<IAppleSignInService, AppleSignInService>();
            services.AddSingleton<IStockApiClient, StockApiClient>();
            services.AddSingleton<IWatchlistApiClient, WatchlistApiClient>();
            services.AddSingleton<IAnalysisApiClient, AnalysisApiClient>();
            services.AddSingleton<ITopPicksApiClient, TopPicksApiClient>();
            services.AddSingleton<IPerformanceApiClient, PerformanceApiClient>();
            services.AddSingleton<IIntelligenceApiClient, IntelligenceApiClient>();
            services.AddSingleton<IProductApiClient, ProductApiClient>();
            services.AddSingleton<AppConfigService>();

            // Transient: IHttpClientFactory constructs a fresh handler
            // instance per HttpClient it builds, per its own lifecycle
            // rules - it must not be registered as a singleton.
            services.AddTransient<AuthenticatedHttpMessageHandler>();

            // No message handler attached - used only for the token-issuing
            // calls (register/login/refresh/logout), which must never carry
            // a stale/expired Authorization header.
            services.AddHttpClient(ApiConfiguration.RawClientName, client =>
            {
                client.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Attaches the current access token automatically and
            // refreshes-and-retries once on a 401. Used for /auth/me and,
            // in later phases, every other authenticated endpoint.
            services.AddHttpClient(ApiConfiguration.AuthenticatedClientName, client =>
            {
                client.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            // Same auth pipeline, longer timeout: on-demand stock analysis
            // (POST /stocks/{symbol}/analysis/refresh) fetches provider data
            // synchronously and can take 10-30 seconds.
            services.AddHttpClient(ApiConfiguration.AuthenticatedLongRunningClientName, client =>
            {
                client.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(120);
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();
        }
    }
}
