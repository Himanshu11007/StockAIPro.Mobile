using Android.App;
using Android.Runtime;
using StockAIPro.Mobile.Platforms.Android.Push;

namespace StockAIPro.Mobile
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            FirebaseSetup.CreateChannel(this);
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
