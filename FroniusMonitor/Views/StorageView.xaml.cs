using System.Windows;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.ViewModels;

namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class StorageView
    {
        public StorageView()
        {
            InitializeComponent();
            DataContext = IoC.Get<StorageViewModel>();

            Loaded += async (_, _) =>
            {
                Vm.Storage = (Storage)((MainWindow)Window.GetWindow(this)!).Vm.DeviceInfo!;
                await Vm.OnInitialize().ConfigureAwait(false);
            };
        }

        public StorageViewModel Vm => (StorageViewModel)DataContext;
    }
}
