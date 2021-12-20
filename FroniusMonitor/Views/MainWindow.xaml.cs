using System.Windows;
using System.Windows.Controls;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.ViewModels;

namespace De.Hochstaetter.FroniusMonitor.Views
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = IoC.Get<MainViewModel>();
            Loaded += async (s, e) => { await ViewModel.OnInitialize().ConfigureAwait(false); };
        }
    }
}
