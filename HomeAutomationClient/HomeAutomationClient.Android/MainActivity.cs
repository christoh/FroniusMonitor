using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationClient;
using De.Hochstaetter.HomeAutomationClient.Misc;

namespace HomeAutomationClient.Android;

[Activity(
    Label = "HomeAutomationClient.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}

public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        PlatformStartup.AccentColor = GetOsAccentColor();

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    /// <summary>
    /// Android 12 and later derive an accent color from the wallpaper (Material You), which is the counterpart of
    /// the accent color of Windows. Older versions have none, so the accent of the app theme applies. Everything
    /// is fully qualified because the namespace of this head shadows the Android SDK ones.
    /// </summary>
    private HaColor? GetOsAccentColor()
    {
        try
        {
            if (Resources is not { } resources || Theme is not { } theme)
            {
                return null;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var systemAccent = resources.GetColor(global::Android.Resource.Color.SystemAccent1500, theme);
                return HaColor.FromArgb(systemAccent.A, systemAccent.R, systemAccent.G, systemAccent.B);
            }

            using var themeAccent = new global::Android.Util.TypedValue();
            return theme.ResolveAttribute(global::Android.Resource.Attribute.ColorAccent, themeAccent, true) ? unchecked((uint)themeAccent.Data) : null;
        }
        catch (Exception)
        {
            // No accent color: the app falls back to the SystemAccentColor of the Fluent theme.
            return null;
        }
    }
}
