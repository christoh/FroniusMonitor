namespace FroniusPhone.Pages;

public partial class MainPage
{
    public MainPage(MainViewModel viewModel)
    {
        BindingContext = ViewModel = viewModel;
        InitializeComponent();
    }

    public MainViewModel ViewModel { get; set; }
}