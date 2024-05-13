namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class NewConnectedInverterView
{
    public NewConnectedInverterViewModel ViewModel { get; set; }

    public NewConnectedInverterView(NewConnectedInverterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = ViewModel = viewModel;

        Loaded += (_, _) => _ = viewModel.OnInitialize();
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.OnOkClicked();
        DialogResult = true;
    }
}