using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.FroniusMonitor.Unity;
using System.Windows;

namespace De.Hochstaetter.FroniusMonitor
{
    public partial class MainWindow : Window
    {
        public readonly IWebClientService webClient=IoC.Get<IWebClientService>();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                var result=await webClient.GetDevices().ConfigureAwait(false);
            };
        }
    }
}
