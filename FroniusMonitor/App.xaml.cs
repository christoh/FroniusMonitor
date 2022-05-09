//using System.Globalization;

using System.IO;
using System.Windows;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.FroniusMonitor.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.ViewModels;
using De.Hochstaetter.FroniusMonitor.Views;
using Unity;

namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    public const double ZoomFactor = 1.025;
    public static string AppName => "FroniusMonitor";
    public static string PerUserDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hochstätter", AppName);
    public static string SettingsFileName => Path.Combine(PerUserDataDir, "Settings.fms");

    public static readonly IUnityContainer Container = new UnityContainer();

    public static Settings Settings { get; set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        //Thread.CurrentThread.CurrentUICulture= new CultureInfo("de-CH");
        AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
        base.OnStartup(e);

        Container
            .RegisterSingleton<IWebClientService, WebClientService>()
            .RegisterSingleton<ISolarSystemService, SolarSystemService>()
            .RegisterSingleton<MainWindow>()
            .RegisterSingleton<MainViewModel>()
            .RegisterType<EventLogView>()
            .RegisterType<EventLogViewModel>()
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

        IoC.Get<MainWindow>().Show();
    }
}
