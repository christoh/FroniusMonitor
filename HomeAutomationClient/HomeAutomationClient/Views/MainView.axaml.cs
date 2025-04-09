using De.Hochstaetter.Fronius;
using De.Hochstaetter.HomeAutomationClient.ViewModels;

namespace De.Hochstaetter.HomeAutomationClient.Views
{
    public partial class MainView : UserControl
    {
        private MainViewModel viewModel;

        public MainView()
        {
            InitializeComponent();
            DataContext = viewModel = IoC.GetRegistered<MainViewModel>();
        }
    }
}