namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class InverterSettingsView : IInverterScoped
{
    public InverterSettingsView(InverterSettingsViewModel viewModel, IGen24Service gen24Service)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.View = this;
        Gen24Service = gen24Service;

        Loaded += async (_, _) =>
        {
            viewModel.Dispatcher = Dispatcher;
            await viewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public IGen24Service Gen24Service { get; }
}
