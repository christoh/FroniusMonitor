using System.Windows;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.ViewModels;

namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class InverterView
    {
        private InverterViewModel Vm => (InverterViewModel) DataContext;

        public InverterView()
        {
            InitializeComponent();
            DataContext = IoC.Get<InverterViewModel>();

            Loaded += async (s, e) =>
            {
                Vm.Inverter = (Inverter)((MainWindow) Window.GetWindow(this)!).Vm.DeviceInfo!;
                await Vm.OnInitialize().ConfigureAwait(false);
            };
        }
    }
}
