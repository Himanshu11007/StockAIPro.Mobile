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

            RegisterApiAndAuthServices(builder.Services);

            return builder.Build();
        }

        private static void RegisterApiAndAuthServices(IServiceCollection services)
        {
            services.AddSingleton<ITokenStore, SecureTokenStore>();
            services.AddSingleton<IAuthApiClient, AuthApiClient>();
            services.AddSingleton<IAuthService, AuthService>();

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
        }
    }
}
