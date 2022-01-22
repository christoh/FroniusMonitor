//using System.Globalization;

using System.IO;
using System.Windows;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.FroniusMonitor.Models;
using Unity;

namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    public const double ZoomFactor = 1.025;
    public static string AppName => "FroniusMonitor";
    public static string PerUserDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hochstätter", AppName);
    public static string SettingsFileName => Path.Combine(PerUserDataDir, "Settings.xml");

    public static readonly IUnityContainer Container = new UnityContainer();

    public static Settings Settings { get; set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        //Thread.CurrentThread.CurrentUICulture= new CultureInfo("de-CH");
        base.OnStartup(e);

        Container
            .RegisterType<IWebClientService, WebClientService>()
            .RegisterSingleton<ISolarSystemService, SolarSystemService>()
            ;

        Directory.CreateDirectory(PerUserDataDir);

        try
        {
            Settings.Load().Wait();
        }
        catch
        {
            Settings = new();
            Settings.Save().Wait();
        }
    }
}