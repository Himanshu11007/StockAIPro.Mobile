using System.Net.Http.Json;
using StockAIPro.Mobile.Models.Notifications;
using StockAIPro.Mobile.Services.Configuration;

namespace StockAIPro.Mobile.Services.Api;

/// <summary>
/// Notification Center, notification preferences, push-device registration
/// and user reports (api/routes/notifications.py). Everything is scoped to
/// the signed-in user server-side; no user id is ever sent.
/// </summary>
public interface INotificationApiClient
{
    Task<NotificationList> GetNotificationsAsync(bool unreadOnly = false, int limit = 50, int offset = 0,
                                                 CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    Task<NotificationItem> MarkReadAsync(int notificationId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(CancellationToken ct = default);

    Task<NotificationPreferences> GetPreferencesAsync(CancellationToken ct = default);
    Task<NotificationPreferences> SavePreferencesAsync(NotificationPreferences preferences, CancellationToken ct = default);

    Task<DeviceInfo> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken ct = default);
    Task<List<DeviceInfo>> GetDevicesAsync(CancellationToken ct = default);
    /// <summary>False when the backend does not know the device (404).</summary>
    Task<bool> RemoveDeviceAsync(string deviceId, CancellationToken ct = default);

    Task<FeedbackReceipt> SubmitFeedbackAsync(FeedbackRequest request, CancellationToken ct = default);
    Task<List<FeedbackItem>> GetMyReportsAsync(CancellationToken ct = default);
    Task<FeedbackReceipt> RequestAccountDeletionAsync(string? reason, CancellationToken ct = default);
}

public sealed class NotificationApiClient : INotificationApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient Client() => _httpClientFactory.CreateClient(ApiConfiguration.AuthenticatedClientName);

    private static string Path(string relative) => $"{ApiConfiguration.ApiPrefix}{relative}";

    public async Task<NotificationList> GetNotificationsAsync(bool unreadOnly = false, int limit = 50, int offset = 0,
                                                              CancellationToken ct = default)
    {
        var client = Client();
        var url = Path($"/notifications?unread_only={(unreadOnly ? "true" : "false")}&limit={limit}&offset={offset}");
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(url, ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<NotificationList>(response, ct);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(Path("/notifications/unread-count"), ct), ct);
        return (await BusinessApiSend.ReadDataOrThrowAsync<UnreadCountResponse>(response, ct)).Unread;
    }

    public async Task<NotificationItem> MarkReadAsync(int notificationId, CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(
            () => client.PostAsync(Path($"/notifications/{notificationId}/read"), content: null, ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<NotificationItem>(response, ct);
    }

    public async Task<int> MarkAllReadAsync(CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(
            () => client.PostAsync(Path("/notifications/read-all"), content: null, ct), ct);
        return (await BusinessApiSend.ReadDataOrThrowAsync<MarkedResponse>(response, ct)).Marked;
    }

    public async Task<NotificationPreferences> GetPreferencesAsync(CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(Path("/notifications/preferences"), ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<NotificationPreferences>(response, ct);
    }

    public async Task<NotificationPreferences> SavePreferencesAsync(NotificationPreferences preferences,
                                                                    CancellationToken ct = default)
    {
        var client = Client();
        var body = new NotificationPreferences
        {
            PushEnabled = preferences.PushEnabled, NewTopCandidate = preferences.NewTopCandidate,
            TopCandidateRemoved = preferences.TopCandidateRemoved, ScoreChanges = preferences.ScoreChanges,
            FqvfChanges = preferences.FqvfChanges, WatchlistAlerts = preferences.WatchlistAlerts,
            DailySummary = preferences.DailySummary, MarketRegime = preferences.MarketRegime,
            QuietHoursEnabled = preferences.QuietHoursEnabled, QuietHoursStart = preferences.QuietHoursStart,
            QuietHoursEnd = preferences.QuietHoursEnd, DailySummaryTime = preferences.DailySummaryTime,
        };
        using var response = await BusinessApiSend.SendAsync(
            () => client.PutAsJsonAsync(Path("/notifications/preferences"), body, ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<NotificationPreferences>(response, ct);
    }

    public async Task<DeviceInfo> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(() => client.PostAsJsonAsync(Path("/devices"), request, ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<DeviceInfo>(response, ct);
    }

    public async Task<List<DeviceInfo>> GetDevicesAsync(CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(Path("/devices"), ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<List<DeviceInfo>>(response, ct);
    }

    public async Task<bool> RemoveDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(
            () => client.DeleteAsync(Path($"/devices/{Uri.EscapeDataString(deviceId)}"), ct), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        await BusinessApiSend.EnsureSuccessAsync(response, ct);
        return true;
    }

    public async Task<FeedbackReceipt> SubmitFeedbackAsync(FeedbackRequest request, CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(() => client.PostAsJsonAsync(Path("/feedback"), request, ct), ct);
        var (item, message) = await BusinessApiSend.ReadEnvelopeOrThrowAsync<FeedbackItem>(response, ct);
        return new FeedbackReceipt(item, message);
    }

    public async Task<List<FeedbackItem>> GetMyReportsAsync(CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(() => client.GetAsync(Path("/feedback"), ct), ct);
        return await BusinessApiSend.ReadDataOrThrowAsync<List<FeedbackItem>>(response, ct);
    }

    public async Task<FeedbackReceipt> RequestAccountDeletionAsync(string? reason, CancellationToken ct = default)
    {
        var client = Client();
        using var response = await BusinessApiSend.SendAsync(
            () => client.PostAsJsonAsync(Path("/account/deletion-request"), new { reason }, ct), ct);
        var (item, message) = await BusinessApiSend.ReadEnvelopeOrThrowAsync<FeedbackItem>(response, ct);
        return new FeedbackReceipt(item, message);
    }
}
