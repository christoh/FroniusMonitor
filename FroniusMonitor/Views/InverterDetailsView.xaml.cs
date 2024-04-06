namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class InverterDetailsView : IInverterScoped
{
    public static DependencyProperty AlwaysUseRunningBackgroundProperty = DependencyProperty.RegisterAttached
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

    public InverterDetailsView(InverterDetailsViewModel viewModel, IGen24Service gen24Service)
    {
        InitializeComponent();
        DataContext = viewModel;
        Gen24Service = gen24Service;

        Loaded += (s, e) =>
        {
            _ = viewModel.OnInitialize();
        };
    }

    public IGen24Service Gen24Service { get; }
}