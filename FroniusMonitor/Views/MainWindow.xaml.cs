using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.ViewModels;

namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class MainWindow : Window
    {
        public MainViewModel Vm => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = IoC.Get<MainViewModel>();

            Loaded += async (s, e) =>
            {
                await Vm.OnInitialize().ConfigureAwait(false);
            };
        }

        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Vm.SelectedItem=((TreeView) sender).SelectedItem;
        }
    }
}
