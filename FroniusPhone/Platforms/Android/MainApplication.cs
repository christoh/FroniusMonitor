using Android.App;
using Android.Runtime;

namespace FroniusPhone;

[Application(UsesCleartextTraffic = true)]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}