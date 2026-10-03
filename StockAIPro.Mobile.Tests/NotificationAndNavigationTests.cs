using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StockAIPro.Mobile.Models.Common;
using StockAIPro.Mobile.Models.Notifications;
using StockAIPro.Mobile.Models.Product;
using StockAIPro.Mobile.Models.Watchlist;
using StockAIPro.Mobile.Services.Api;
using StockAIPro.Mobile.Services.Configuration;
using StockAIPro.Mobile.Services.Navigation;
using StockAIPro.Mobile.Services.Notifications;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

/// <summary>Deep links, pending navigation, push registration, the
/// notification API client, contract tests against real backend responses
/// (Fixtures/, captured by the backend's export_mobile_contract_fixtures.py),
/// and API failure handling for the new endpoints.</summary>
public class NotificationAndNavigationTests
{
    // ── deep links ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/stock/TCS.NS", "/stock/TCS.NS")]
    [InlineData("stock/tcs.ns", "/stock/TCS.NS")]
    [InlineData("stockaipro://stock/TCS.NS", "/stock/TCS.NS")]
    [InlineData("stockaipro://top-picks", "/top-picks")]
    [InlineData("/top-picks/", "/top-picks")]
    [InlineData("/notifications?x=1", "/notifications")]
    [InlineData("/", "/")]
    [InlineData("/stock/M&M.NS", "/stock/M%26M.NS")]
    [InlineData("/settings/notifications", "/settings/notifications")]
    public void Allowed_routes_are_normalised(string input, string expected) =>
        Assert.Equal(expected, DeepLinkRouter.ToRoute(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://evil.example/stock/TCS.NS")]
    [InlineData("javascript:alert(1)")]
    [InlineData("otherapp://stock/TCS.NS")]
    [InlineData("/stock/../account")]
    [InlineData("/admin")]
    [InlineData("/stock/TCS.NS/extra")]
    [InlineData("/stock/<script>")]
    [InlineData("/stock/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Unknown_or_unsafe_destinations_are_rejected(string? input) =>
        Assert.Null(DeepLinkRouter.ToRoute(input));

    [Fact]
    public void Push_payload_maps_to_route_and_notification_id()
    {
        var data = new Dictionary<string, string?> { ["route"] = "/stock/INFY.NS", ["notification_id"] = "42" };
        Assert.Equal("/stock/INFY.NS", DeepLinkRouter.FromPushData(data));
        Assert.Equal(42, DeepLinkRouter.NotificationId(data));
        Assert.Equal("/stock/TCS.NS", DeepLinkRouter.FromPushData(new Dictionary<string, string?> { ["symbol"] = "TCS.NS" }));
        Assert.Equal("/notifications", DeepLinkRouter.FromPushData(new Dictionary<string, string?> { ["route"] = "https://x" }));
    }

    [Fact]
    public void Pending_destination_survives_sign_in()
    {
        var nav = new PendingNavigationService();
        var raised = 0;
        nav.DestinationRequested += () => raised++;
        Assert.True(nav.RequestFromPush(new Dictionary<string, string?> { ["route"] = "/stock/TCS.NS", ["notification_id"] = "7" }));
        Assert.Equal(1, raised);
        Assert.Null(nav.TryConsume(isAuthenticated: false));            // signed out: kept, not shown
        var d = nav.TryConsume(isAuthenticated: true);                    // after sign-in: opened once
        Assert.Equal(new PendingDestination("/stock/TCS.NS", 7), d);
        Assert.Null(nav.TryConsume(isAuthenticated: true));
        Assert.False(nav.Request("https://evil.example"));
        Assert.Null(nav.Peek());
    }

    // ── push registration ────────────────────────────────────────────────────

    private sealed class FakePush : IPushTokenProvider
    {
        public string? Platform { get; set; } = "android";
        public bool IsConfigured { get; set; } = true;
        public string? AppVersion => "1.2";
        public PushPermission Permission { get; set; } = PushPermission.Granted;
        public string? Token { get; set; } = "fcm-token";
        public int PermissionRequests { get; private set; }
        public Task<PushPermission> GetPermissionAsync() => Task.FromResult(Permission);
        public Task<PushPermission> RequestPermissionAsync() { PermissionRequests++; return Task.FromResult(Permission); }
        public Task<string?> GetTokenAsync() => Task.FromResult(Token);
        public event Action<string>? TokenRefreshed;
        public void Rotate(string t) { Token = t; TokenRefreshed?.Invoke(t); }
    }

    private static (PushRegistrationService Service, FakeHttpMessageHandler Http, FakePush Push, FakeAuthService Auth) Push()
    {
        var http = new FakeHttpMessageHandler();
        var api = new NotificationApiClient(new FakeHttpClientFactory(http));
        var push = new FakePush();
        var auth = new FakeAuthService();
        return (new PushRegistrationService(api, new InMemoryDeviceIdentityService("dev-1"), push, auth), http, push, auth);
    }

    private static StringContent DeviceOk() => new(
        """{"success":true,"data":{"device_id":"dev-1","platform":"android","token":"...-token","active":true},"message":"ok"}""",
        System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task Registers_device_token_with_backend_after_sign_in()
    {
        var (svc, http, push, auth) = Push();
        http.Enqueue(HttpStatusCode.OK, DeviceOk());                         // triggered by sign-in
        auth.SetAuthenticated(true);
        await Task.Delay(50);
        Assert.Single(http.Requests);
        Assert.Equal("/api/v1/devices", http.Requests[0].RequestUri!.AbsolutePath);
        var body = JsonDocument.Parse(http.RequestBodies[0]!).RootElement;
        Assert.Equal("dev-1", body.GetProperty("device_id").GetString());
        Assert.Equal("android", body.GetProperty("platform").GetString());
        Assert.Equal("fcm-token", body.GetProperty("push_token").GetString());
        Assert.Equal("granted", body.GetProperty("permission").GetString());
        Assert.Equal(PushRegistrationStatus.Registered, svc.LastResult.Status);
        Assert.Equal(0, push.PermissionRequests);                            // never prompts silently

        http.Enqueue(HttpStatusCode.OK, DeviceOk());                         // token rotation re-registers
        push.Rotate("fcm-token-2");
        await Task.Delay(50);
        Assert.Contains("fcm-token-2", http.RequestBodies[1]);
    }

    [Fact]
    public async Task Denied_permission_and_unconfigured_builds_are_reported_honestly()
    {
        var (svc, http, push, auth) = Push();
        http.Enqueue(HttpStatusCode.OK, DeviceOk());
        auth.SetAuthenticated(true);
        await Task.Delay(50);

        push.Permission = PushPermission.Denied;
        http.Enqueue(HttpStatusCode.OK, DeviceOk());
        var denied = await svc.RegisterAsync(requestPermission: true);
        Assert.Equal(PushRegistrationStatus.PermissionDenied, denied.Status);
        Assert.Equal(1, push.PermissionRequests);
        Assert.Contains("\"push_token\":null", http.RequestBodies[^1]);
        Assert.Contains("\"permission\":\"denied\"", http.RequestBodies[^1]);

        push.Permission = PushPermission.Granted;
        push.IsConfigured = false;
        http.Enqueue(HttpStatusCode.OK, DeviceOk());
        var nc = await svc.RegisterAsync(requestPermission: false);
        Assert.Equal(PushRegistrationStatus.NotConfigured, nc.Status);
        Assert.Contains("Notification Center", nc.Message);

        push.Platform = null;
        Assert.Equal(PushRegistrationStatus.Unsupported, (await svc.RegisterAsync(false)).Status);
    }

    [Fact]
    public async Task Registration_failure_never_throws()
    {
        var (svc, http, _, auth) = Push();
        http.EnqueueNetworkFailure();
        auth.SetAuthenticated(true);
        await Task.Delay(50);
        Assert.Equal(PushRegistrationStatus.Failed, svc.LastResult.Status);
        Assert.DoesNotContain("simulated", svc.LastResult.Message);
        auth.SetAuthenticated(false);
        Assert.Equal(PushRegistrationStatus.NotAttempted, svc.LastResult.Status);
        Assert.Equal(PushRegistrationStatus.NotAttempted, (await svc.RegisterAsync(true)).Status);
    }

    // ── notification client + unread badge ───────────────────────────────────

    private static StringContent Json(string s) => new(s, System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task Notification_client_calls_expected_endpoints()
    {
        var http = new FakeHttpMessageHandler();
        var api = new NotificationApiClient(new FakeHttpClientFactory(http));
        http.Enqueue(HttpStatusCode.OK, Json(File.ReadAllText("Fixtures/notifications.json")));
        var list = await api.GetNotificationsAsync(unreadOnly: true, limit: 20);
        Assert.Equal("/api/v1/notifications?unread_only=true&limit=20&offset=0", http.Requests[0].RequestUri!.PathAndQuery);
        Assert.NotEmpty(list.Items);

        http.Enqueue(HttpStatusCode.OK, Json("""{"success":true,"data":{"unread":3},"message":""}"""));
        Assert.Equal(3, await api.GetUnreadCountAsync());
        http.Enqueue(HttpStatusCode.OK, Json("""{"success":true,"data":{"marked":3},"message":""}"""));
        Assert.Equal(3, await api.MarkAllReadAsync());
        Assert.Equal(HttpMethod.Post, http.Requests[^1].Method);
        http.Enqueue(HttpStatusCode.NotFound, Json("""{"detail":"Notification not found"}"""));
        var ex = await Assert.ThrowsAsync<ApiException>(() => api.MarkReadAsync(999));
        Assert.Equal(ApiErrorKind.NotFound, ex.Kind);
        http.Enqueue(HttpStatusCode.NotFound, Json("""{"detail":"Device not found"}"""));
        Assert.False(await api.RemoveDeviceAsync("x"));

        http.Enqueue(HttpStatusCode.OK, Json(File.ReadAllText("Fixtures/notification_preferences.json")));
        var prefs = await api.GetPreferencesAsync();
        prefs.DailySummary = true;
        http.Enqueue(HttpStatusCode.OK, Json(File.ReadAllText("Fixtures/notification_preferences.json")));
        await api.SavePreferencesAsync(prefs);
        Assert.Equal(HttpMethod.Put, http.Requests[^1].Method);
        Assert.Contains("\"daily_summary\":true", http.RequestBodies[^1]);
        Assert.DoesNotContain("updated_at", http.RequestBodies[^1]);
    }

    [Fact]
    public async Task Feedback_receipt_carries_backend_message()
    {
        var http = new FakeHttpMessageHandler();
        var api = new NotificationApiClient(new FakeHttpClientFactory(http));
        http.Enqueue(HttpStatusCode.Created, Json(File.ReadAllText("Fixtures/feedback_created.json")));
        var receipt = await api.SubmitFeedbackAsync(new FeedbackRequest
            { Category = FeedbackCategories.StaleData, Message = "Price looks old", Symbol = "S03.NS" });
        Assert.Contains("recorded", receipt.Message);
        Assert.Equal("STALE_DATA", receipt.Item.Category);
        http.Enqueue(HttpStatusCode.TooManyRequests, Json("""{"detail":"You have reached the limit of reports for today. Please try again tomorrow."}"""));
        var ex = await Assert.ThrowsAsync<ApiException>(() => api.SubmitFeedbackAsync(
            new FeedbackRequest { Category = FeedbackCategories.AppBug, Message = "broken" }));
        Assert.Equal(ApiErrorKind.TooManyRequests, ex.Kind);
        Assert.StartsWith("You have reached the limit", ex.Message);
    }

    [Fact]
    public async Task Unread_badge_tracks_backend_and_resets_on_sign_out()
    {
        var http = new FakeHttpMessageHandler();
        var auth = new FakeAuthService();
        var state = new NotificationStateService(new NotificationApiClient(new FakeHttpClientFactory(http)), auth);
        http.Enqueue(HttpStatusCode.OK, Json("""{"success":true,"data":{"unread":4},"message":""}"""));
        auth.SetAuthenticated(true);
        await Task.Delay(50);
        Assert.Equal(4, state.UnreadCount);
        http.Enqueue(HttpStatusCode.InternalServerError, Json("""{"success":false,"error":"Internal server error","details":"Unexpected server error (reference abc)"}"""));
        await state.RefreshAsync();
        Assert.Equal(4, state.UnreadCount);                                  // failure keeps last value
        auth.SetAuthenticated(false);
        await Task.Delay(50);
        Assert.Equal(0, state.UnreadCount);
    }

    // ── contract tests: real backend responses ───────────────────────────────

    private static T Data<T>(string fixture) =>
        JsonSerializer.Deserialize<ApiEnvelope<T>>(File.ReadAllText($"Fixtures/{fixture}.json"))!.Data;

    [Fact]
    public void Contract_stock_analysis_has_explanation_labels_freshness_and_regime()
    {
        var a = Data<StockAnalysis>("stock_analysis");
        Assert.Equal("S03.NS", a.Symbol);
        Assert.StartsWith("Ranked ", a.Explanation!.Summary);
        Assert.Contains(a.Labels, l => l.Label == "Top Candidate" && l.Tone == "positive");
        Assert.Contains(a.Labels, l => l.Label == "High Risk" && l.Tone == "negative");
        Assert.DoesNotContain(a.Labels, l => l.Label.Contains("buy", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("OK", a.FreshnessStatus!.Status);
        Assert.NotNull(a.MarketRegime!.Regime);
        Assert.Equal(1234.5, a.ReferencePrice!.Close);
        Assert.NotNull(a.Market!.Technical!.TrendDaily);
        Assert.Equal("UP", a.Market.Technical.TrendDaily!.Trend);
        Assert.NotEmpty(a.Ranking.Components);
    }

    [Fact]
    public void Contract_top_picks_watchlist_and_notifications()
    {
        var tp = Data<TopCandidatesResponse>("top_picks");
        Assert.NotEmpty(tp.Items);
        var first = tp.Items[0];
        Assert.Equal(6, first.Components.Count);
        Assert.NotNull(first.FreshnessStatus);
        Assert.Contains(first.Labels, l => l.Key == "top_candidate");

        var wl = Data<WatchlistOverview>("watchlist_overview");
        var w = Assert.Single(wl.Items);
        Assert.Equal("S03.NS", w.Symbol);
        Assert.Null(w.BuyPrice);
        Assert.NotNull(w.StockAiScore);
        Assert.Equal(-12.0, w.ScoreChange);
        Assert.True(w.Alerts.ScoreChanges);
        Assert.True(w.AlertsActive);

        var n = Data<NotificationList>("notifications");
        Assert.True(n.Unread > 0);
        Assert.All(n.Items, i => Assert.NotNull(DeepLinkRouter.ToRoute(i.Route)));
        Assert.Contains(n.Items, i => i.Type == "WATCHLIST_ALERT" && i.Route == "/stock/S03.NS");

        var prefs = Data<NotificationPreferences>("notification_preferences");
        Assert.True(prefs.ScoreChanges);
        Assert.Equal("Asia/Kolkata", prefs.Timezone);

        var devices = Data<List<DeviceInfo>>("devices");
        Assert.DoesNotContain("fixture-token", devices[0].MaskedToken);
    }

    [Fact]
    public void Contract_market_performance_intelligence_and_config()
    {
        var m = Data<MarketStatusInfo>("market_status");
        Assert.False(string.IsNullOrEmpty(m.Label));
        Assert.StartsWith("Asia/Kolkata", m.Timezone);

        var p = Data<PerformanceOverview>("performance_overview");
        Assert.Equal("COLLECTING", p.Sections.Prospective!.Status);
        Assert.Equal("Performance tracking will appear after enough observations are available.", p.Sections.Prospective.Message);
        Assert.Contains("not establish", p.Sections.Validation!.Conclusion);
        Assert.NotNull(p.Sections.LegacySignals!.Caveat);

        var i = Data<IntelligenceOverview>("intelligence_overview");
        Assert.Equal(["market", "fundamental", "technical", "ml_signal", "news", "legacy"], i.Sections.Select(s => s.Key));
        Assert.StartsWith("Informational only", i.Sections.Single(s => s.Key == "ml_signal").Status);

        var cfg = Data<AppConfig>("app_config");
        Assert.True(cfg.Onboarding.Count >= 5);
        Assert.False(string.IsNullOrEmpty(cfg.Legal!.PrivacySummary));
        Assert.True(cfg.Features["notifications"]);
    }

    // ── formatting ───────────────────────────────────────────────────────────

    [Fact]
    public void Ist_timestamps_relative_ages_and_freshness_lines()
    {
        Assert.Equal("03 Oct 2026 09:35 IST", ProductFormat.DateTimeIst("2026-10-03T04:05:00+00:00"));
        Assert.Equal("03 Oct 2026", ProductFormat.DateTimeIst("2026-10-03"));
        var now = new DateTimeOffset(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal("5 minutes ago", ProductFormat.Relative("2026-10-03T09:55:00Z", now));
        Assert.Equal("1 hour ago", ProductFormat.Relative("2026-10-03T08:30:00Z", now));
        Assert.Equal("2 days ago", ProductFormat.Relative("2026-10-01T09:00:00Z", now));
        Assert.Equal("-", ProductFormat.Relative(null, now));
        var stale = new FreshnessStatus { Status = "STALE", MarketDataAsOf = "2026-09-20" };
        Assert.StartsWith("Data may be stale.", ProductFormat.FreshnessLine(stale, now));
        Assert.Equal("Data unavailable", ProductFormat.FreshnessLine(new FreshnessStatus { Status = "UNAVAILABLE" }, now));
        Assert.Equal("+12.5", ProductFormat.Change(12.5));
        Assert.Equal("-", ProductFormat.Change(null));
    }

    // ── API failure handling for the new endpoints ───────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, ApiErrorKind.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests, ApiErrorKind.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError, ApiErrorKind.ServerError)]
    [InlineData(HttpStatusCode.BadGateway, ApiErrorKind.ServerError)]
    public async Task Failures_map_to_kinds_with_user_safe_messages(HttpStatusCode code, ApiErrorKind kind)
    {
        var http = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactory();
        factory.Configure(ApiConfiguration.AuthenticatedClientName, http);
        var product = new ProductApiClient(factory);
        http.Enqueue(code, new StringContent("<html>proxy error</html>"));
        var ex = await Assert.ThrowsAsync<ApiException>(() => product.GetPerformanceOverviewAsync());
        Assert.Equal(kind, ex.Kind);
        Assert.DoesNotContain("<html>", ex.Message);
    }

    [Fact]
    public async Task Network_loss_and_timeout_are_reported_as_network_unavailable()
    {
        var http = new FakeHttpMessageHandler();
        var api = new NotificationApiClient(new FakeHttpClientFactory(http));
        http.EnqueueNetworkFailure();
        Assert.Equal(ApiErrorKind.NetworkUnavailable, (await Assert.ThrowsAsync<ApiException>(() => api.GetUnreadCountAsync())).Kind);
        http.EnqueueTimeout();
        var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetNotificationsAsync());
        Assert.Equal(ApiErrorKind.NetworkUnavailable, ex.Kind);
        Assert.DoesNotContain("simulated", ex.Message);
    }

    [Fact]
    public async Task Watchlist_overview_and_alert_update()
    {
        var http = new FakeHttpMessageHandler();
        var api = new WatchlistApiClient(new FakeHttpClientFactory(http));
        http.Enqueue(HttpStatusCode.OK, Json(File.ReadAllText("Fixtures/watchlist_overview.json")));
        var ov = await api.GetOverviewAsync();
        Assert.Single(ov.Items);
        http.Enqueue(HttpStatusCode.OK, Json("""{"success":true,"data":{"symbol":"S03.NS","score_changes":false,"rank_changes":true,"fqvf_changes":true,"status_changes":true,"muted":false},"message":""}"""));
        var saved = await api.UpdateAlertsAsync(ov.Items[0].Id, new WatchlistAlerts { ScoreChanges = false });
        Assert.False(saved.ScoreChanges);
        Assert.Equal(HttpMethod.Put, http.Requests[^1].Method);
        Assert.EndsWith($"/watchlist/{ov.Items[0].Id}/alerts", http.Requests[^1].RequestUri!.AbsolutePath);
        http.Enqueue(HttpStatusCode.Created, Json("""{"id":9,"symbol":"TCS.NS","stock_name":"TCS","buy_price":null,"buy_date":null,"quantity":1,"created_at":"2026-10-03T00:00:00Z"}"""));
        var added = await api.AddAsync(new WatchlistAddRequest { Symbol = "TCS.NS" });
        Assert.Null(added.BuyPrice);
        Assert.DoesNotContain("buy_price", http.RequestBodies[^1]);
    }
}
