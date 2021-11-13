using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Services;
using System.Windows;
using Unity;

namespace FroniusMonitor
{

    public partial class App : Application
    {
        public static readonly IUnityContainer Container = new UnityContainer();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Container
                .RegisterType<IWebClientService, WebClientService>()
                ;
        }
    }
}
