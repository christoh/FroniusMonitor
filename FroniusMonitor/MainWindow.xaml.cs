using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace De.Hochstaetter.FroniusMonitor
{
    public partial class MainWindow : Window
    {
        private MainViewModel Vm => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = IoC.Get<MainViewModel>();

            Loaded += async (s, e) =>
            {
                await Vm.OnInitialize().ConfigureAwait(false);
            };
        }
    }
}
