using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using StockAIPro.Mobile.Platforms.Android.Push;

namespace StockAIPro.Mobile
{
    // SingleTop: a notification tap / stockaipro:// link while the app is
    // running arrives in OnNewIntent instead of starting a second activity.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = "stockaipro")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            FirebaseSetup.HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            FirebaseSetup.HandleIntent(intent);
        }
    }
}
