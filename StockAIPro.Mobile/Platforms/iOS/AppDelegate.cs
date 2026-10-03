using Foundation;
using StockAIPro.Mobile.Platforms.iOS.Push;
using StockAIPro.Mobile.Services.Navigation;
using UIKit;
using UserNotifications;

namespace StockAIPro.Mobile
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        {
            var ok = base.FinishedLaunching(application, launchOptions);
            UNUserNotificationCenter.Current.Delegate = new NotificationCenterDelegate();
            return ok;
        }

        [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
        public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) =>
            IosPushTokenProvider.Instance?.OnToken(deviceToken);

        [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
        public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) =>
            IosPushTokenProvider.Instance?.OnTokenFailed();

        // stockaipro:// links
        public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
        {
            var nav = IPlatformApplication.Current?.Services.GetService<PendingNavigationService>();
            if (nav is not null && url.AbsoluteString is { } link && nav.Request(link)) return true;
            return base.OpenUrl(application, url, options);
        }
    }
}
