using System.Reflection;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase;
using Firebase.Messaging;
using StockAIPro.Mobile.Services.Navigation;
using StockAIPro.Mobile.Services.Notifications;

namespace StockAIPro.Mobile.Platforms.Android.Push;

/// <summary>
/// Firebase Cloud Messaging set-up. Firebase is initialised either from
/// google-services.json (Platforms/Android/google-services.json, gitignored,
/// picked up by the build when present) or from build properties
/// (-p:FirebaseApplicationId=... -p:FirebaseApiKey=... -p:FirebaseProjectId=...
/// -p:FirebaseSenderId=...). With neither, push is reported as not
/// configured and the app keeps working with the in-app Notification Center.
/// </summary>
public static class FirebaseSetup
{
    public const string ChannelId = "stockai_alerts";
    private static bool? _initialized;

    public static bool EnsureInitialized(Context context)
    {
        if (_initialized is { } done) return done;
        try
        {
            if (FirebaseApp.GetApps(context).Count > 0)
                return (_initialized = true).Value;
            if (FirebaseApp.InitializeApp(context) is not null)
                return (_initialized = true).Value;
            var meta = typeof(FirebaseSetup).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .ToDictionary(a => a.Key, a => a.Value);
            if (meta.TryGetValue("FirebaseApplicationId", out var appId) && !string.IsNullOrEmpty(appId) &&
                meta.TryGetValue("FirebaseApiKey", out var apiKey) && !string.IsNullOrEmpty(apiKey) &&
                meta.TryGetValue("FirebaseProjectId", out var projectId) && !string.IsNullOrEmpty(projectId))
            {
                var options = new FirebaseOptions.Builder()
                    .SetApplicationId(appId).SetApiKey(apiKey).SetProjectId(projectId)
                    .SetGcmSenderId(meta.GetValueOrDefault("FirebaseSenderId")).Build();
                FirebaseApp.InitializeApp(context, options);
                return (_initialized = true).Value;
            }
        }
        catch (Exception)
        {
            // Misconfigured Firebase must never crash the app.
        }
        return (_initialized = false).Value;
    }

    public static void CreateChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, "StockAI alerts", NotificationImportance.Default)
        {
            Description = "Top Candidate changes, watchlist alerts, daily summaries and market regime updates",
        };
        (context.GetSystemService(Context.NotificationService) as NotificationManager)?.CreateNotificationChannel(channel);
    }

    /// <summary>Route a launch/new intent (notification tap or stockaipro://
    /// link) into the app's pending navigation.</summary>
    public static void HandleIntent(Intent? intent)
    {
        if (intent is null) return;
        var nav = IPlatformApplication.Current?.Services.GetService<PendingNavigationService>();
        if (nav is null) return;
        if (intent.Data is { } uri && string.Equals(uri.Scheme, DeepLinkRouter.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            nav.Request(uri.ToString());
            return;
        }
        var extras = intent.Extras;
        if (extras is null || !extras.ContainsKey("route")) return;
        var data = new Dictionary<string, string?>
        {
            ["route"] = extras.GetString("route"),
            ["notification_id"] = extras.GetString("notification_id"),
            ["symbol"] = extras.GetString("symbol"),
        };
        nav.RequestFromPush(data);
        intent.RemoveExtra("route");   // handle a tap once, not again on rotation
    }
}

public sealed class AndroidPushTokenProvider : IPushTokenProvider
{
    public static AndroidPushTokenProvider? Instance { get; private set; }

    public AndroidPushTokenProvider() => Instance = this;

    public string? Platform => "android";

    public bool IsConfigured => FirebaseSetup.EnsureInitialized(global::Android.App.Application.Context);

    public string? AppVersion => AppInfo.Current.VersionString;

    public event Action<string>? TokenRefreshed;

    internal void RaiseTokenRefreshed(string token) => TokenRefreshed?.Invoke(token);

    public async Task<PushPermission> GetPermissionAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return Map(await Permissions.CheckStatusAsync<Permissions.PostNotifications>());
        return NotificationManagerCompat.From(global::Android.App.Application.Context).AreNotificationsEnabled()
            ? PushPermission.Granted : PushPermission.Denied;
    }

    public async Task<PushPermission> RequestPermissionAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return Map(await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.PostNotifications>));
        return await GetPermissionAsync();
    }

    private static PushPermission Map(PermissionStatus s) => s switch
    {
        PermissionStatus.Granted => PushPermission.Granted,
        PermissionStatus.Denied => PushPermission.Denied,
        _ => PushPermission.Unknown,
    };

    public async Task<string?> GetTokenAsync()
    {
        if (!IsConfigured) return null;
        try
        {
            var tcs = new TaskCompletionSource<string?>();
            FirebaseMessaging.Instance.GetToken().AddOnCompleteListener(new TokenListener(tcs));
            var done = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            return done == tcs.Task ? tcs.Task.Result : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed class TokenListener(TaskCompletionSource<string?> tcs)
        : Java.Lang.Object, global::Android.Gms.Tasks.IOnCompleteListener
    {
        public void OnComplete(global::Android.Gms.Tasks.Task task) =>
            tcs.TrySetResult(task.IsSuccessful ? task.Result?.ToString() : null);
    }
}

/// <summary>Receives FCM tokens and messages. Background notification
/// messages are shown by the system; a message arriving while the app is
/// in the foreground is shown here, and the unread badge is refreshed.</summary>
[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class StockAiFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token) => AndroidPushTokenProvider.Instance?.RaiseTokenRefreshed(token);

    public override void OnMessageReceived(RemoteMessage message)
    {
        var state = IPlatformApplication.Current?.Services.GetService<NotificationStateService>();
        _ = state?.RefreshAsync();
        var n = message.GetNotification();
        if (n is null) return;
        var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        foreach (var kv in message.Data)
            intent.PutExtra(kv.Key, kv.Value);
        var id = message.Data.TryGetValue("notification_id", out var nid) && int.TryParse(nid, out var parsed)
            ? parsed : System.Environment.TickCount;
        var pending = PendingIntent.GetActivity(this, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        var builder = new NotificationCompat.Builder(this, FirebaseSetup.ChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(n.Title)
            .SetContentText(n.Body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(n.Body))
            .SetAutoCancel(true)
            .SetContentIntent(pending);
        NotificationManagerCompat.From(this).Notify(id, builder.Build());
    }
}
