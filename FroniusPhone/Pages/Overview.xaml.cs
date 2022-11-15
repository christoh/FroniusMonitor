namespace FroniusPhone.Pages;

public partial class Overview
{
    public Overview(OverviewViewModel viewModel)
    {
        BindingContext = ViewModel = viewModel;
        InitializeComponent();
    }

    public OverviewViewModel ViewModel { get; set; }
}