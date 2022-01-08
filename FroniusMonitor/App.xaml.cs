//using System.Globalization;
using System.Windows;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Services;
using Unity;

namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    public const double ZoomFactor = 1.025;

    public static readonly IUnityContainer Container = new UnityContainer();

    public static Settings Settings { get; set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        //Thread.CurrentThread.CurrentUICulture= new CultureInfo("de-CH");
        base.OnStartup(e);

        Container
            .RegisterType<IWebClientService, WebClientService>()
            .RegisterSingleton<ISolarSystemService, SolarSystemService>()
            .RegisterSingleton<Settings>()
            ;
    }
}