namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class InverterDetailsView : IInverterScoped
{

    private readonly IServiceProvider provider;
    
    public static readonly DependencyProperty AlwaysUseRunningBackgroundProperty = DependencyProperty.RegisterAttached
    (
        nameof(GetAlwaysUseRunningBackground)[3..], typeof(bool), typeof(InverterDetailsView),
        new FrameworkPropertyMetadata(false)
    );

    public static bool GetAlwaysUseRunningBackground(DependencyObject d)
    {
        return (bool)d.GetValue(AlwaysUseRunningBackgroundProperty);
    }

    public static void SetAlwaysUseRunningBackground(DependencyObject d, bool value)
    {
        d.SetValue(AlwaysUseRunningBackgroundProperty, value);
    }

    public InverterDetailsView(InverterDetailsViewModel viewModel, IGen24Service gen24Service, IServiceProvider provider)
    {
        this.viewModel = viewModel;
        this.provider=provider;
        InitializeComponent();
        DataContext = viewModel;
        Gen24Service = gen24Service;

        Loaded += (s, e) =>
        {
            _ = viewModel.OnInitialize();
        };

        Closed += (s, e) =>
        {
            _ = viewModel.CleanUp();
        };
    }

    protected override ScaleTransform Scaler => WrapScaler;

    private readonly InverterDetailsViewModel viewModel;


    public IGen24Service Gen24Service { get; }

    private void CheckAllViews(object sender, RoutedEventArgs e)
    {
        var all = GetAllViewMenuItems();
        all[1..].Apply(item => item.IsChecked = all[0].IsChecked);
    }

    private void CheckShowAll(object sender, RoutedEventArgs e)
    {
        var all = GetAllViewMenuItems();
        all[0].IsChecked = all[1..].All(i => i.IsChecked);
        viewModel.IsNoneSelected = all[1..].All(i => !i.IsChecked);
    }

    private MenuItem[] GetAllViewMenuItems() => View.Items.OfType<MenuItem>().ToArray();
    
    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        IoC.TryGetRegistered<MainWindow>()?.GetView<InverterSettingsView>(provider).Focus();
    }

    private void OnEnergyFlowClicked(object sender, RoutedEventArgs e)
    {
        IoC.TryGetRegistered<MainWindow>()?.GetView<SelfConsumptionOptimizationView>(provider).Focus();
    }

    private void OnModbusClicked(object sender, RoutedEventArgs e)
    {
        IoC.TryGetRegistered<MainWindow>()?.GetView<ModbusView>(provider).Focus();
    }

    private void OnEventLogClicked(object sender, RoutedEventArgs e)
    {
        IoC.TryGetRegistered<MainWindow>()?.GetView<EventLogView>(provider).Focus();
    }
}