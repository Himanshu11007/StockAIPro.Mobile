using System.Net;
using System.Net.Http.Json;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class ApiConfigurationResolveTests
{
    private const string Dev = "http://127.0.0.1:8000";

    [Fact]
    public void Debug_without_configuration_uses_the_local_development_backend()
    {
        var (url, env) = ApiConfiguration.Resolve(null, isReleaseBuild: false, Dev);
        Assert.Equal(Dev, url);
        Assert.Equal(ApiEnvironment.Development, env);
    }

    [Fact]
    public void Release_without_configuration_is_refused_so_a_developer_ip_never_ships()
    {
        Assert.Throws<InvalidOperationException>(() => ApiConfiguration.Resolve("", isReleaseBuild: true, Dev));
    }

    [Theory]
    [InlineData("http://192.168.1.20:8000", false, "http://192.168.1.20:8000")]
    [InlineData("https://api.example.com/", true, "https://api.example.com")]
    [InlineData("  https://api.example.com:8443  ", true, "https://api.example.com:8443")]
    public void Configured_url_is_normalised(string configured, bool release, string expected)
    {
        var (url, env) = ApiConfiguration.Resolve(configured, release, Dev);
        Assert.Equal(expected, url);
        Assert.Equal(ApiEnvironment.Configured, env);
    }

    [Theory]
    [InlineData("http://api.example.com", true)]    // release must be HTTPS
    [InlineData("ftp://api.example.com", false)]
    [InlineData("api.example.com", false)]
    [InlineData("https://api.example.com/api/v1", false)] // prefix is added by the app
    [InlineData("https://api.example.com/?x=1", false)]
    public void Invalid_configuration_is_rejected(string configured, bool release)
    {
        Assert.Throws<ArgumentException>(() => ApiConfiguration.Resolve(configured, release, Dev));
    }
}

public class AppConfigServiceTests
{
    private static (AppConfigService Service, FakeHttpMessageHandler Raw) Create()
    {
        var raw = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.RawClientName, raw);
        return (new AppConfigService(new ProductApiClient(factory)), raw);
    }

    [Fact]
    public async Task Loads_backend_feature_flags_and_disclaimer()
    {
        var (service, raw) = Create();
        raw.Enqueue(HttpStatusCode.OK, JsonContent.Create(new
        {
            success = true, message = "ok",
            data = new { features = new Dictionary<string, bool> { ["intelligence"] = false }, disclaimer = "Backend text.",
                         announcement = "Maintenance tonight", top_picks_limit = 10 },
        }));

        await service.LoadAsync();

        Assert.False(service.IsFallback);
        Assert.False(service.IsEnabled("intelligence"));
        Assert.True(service.IsEnabled("watchlist"));          // not mentioned -> enabled
        Assert.Equal("Backend text.", service.Disclaimer);
        Assert.Equal("Maintenance tonight", service.Announcement);
    }

    [Fact]
    public async Task Unreachable_backend_never_throws_and_keeps_features_visible()
    {
        var (service, raw) = Create();
        raw.EnqueueNetworkFailure();

        await service.LoadAsync();

        Assert.True(service.IsFallback);
        Assert.NotNull(service.LastError);
        Assert.True(service.IsEnabled("top_picks"));
        Assert.Equal(AppConfigService.FallbackDisclaimer, service.Disclaimer);
        Assert.Null(service.Announcement);
    }

    [Fact]
    public async Task Malformed_payload_is_handled_as_fallback()
    {
        var (service, raw) = Create();
        raw.Enqueue(HttpStatusCode.OK, new StringContent("not json"));

        await service.LoadAsync();

        Assert.True(service.IsFallback);
        Assert.NotNull(service.LastError);
    }

    [Fact]
    public async Task Loads_once_unless_forced()
    {
        var (service, raw) = Create();
        object Payload() => new { success = true, message = "ok", data = new { features = new Dictionary<string, bool>(), disclaimer = "x", top_picks_limit = 5 } };
        raw.Enqueue(HttpStatusCode.OK, JsonContent.Create(Payload()));
        raw.Enqueue(HttpStatusCode.OK, JsonContent.Create(Payload()));

        await service.LoadAsync();
        await service.LoadAsync();
        Assert.Single(raw.Requests);
        await service.LoadAsync(force: true);
        Assert.Equal(2, raw.Requests.Count);
    }
}
