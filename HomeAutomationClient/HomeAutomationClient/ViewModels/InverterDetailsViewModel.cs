namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class InverterDetailsViewModel(DashboardViewModel dashboardViewModel) : ViewModelBase
{
    [ObservableProperty]
    public partial Gen24System Gen24System { get; set; } = null!;

    public bool ColorAllTicks=>dashboardViewModel.ColorAllTicks; //BUG: Move to MainViewModel or settings
}
