namespace FroniusPhone.Pages;

public partial class Overview
{
    public Overview(OverviewViewModel viewModel)
    {
        BindingContext = ViewModel = viewModel;
        InitializeComponent();
        HandlerChanged += (_, _) => viewModel.Dispatcher = Dispatcher;
    }

    public OverviewViewModel ViewModel { get; set; }
}