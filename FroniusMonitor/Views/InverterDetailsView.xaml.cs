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
        this.viewModel = viewModel;
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

        PreviewMouseWheel += OnMouseWheel;
        PreviewKeyDown += OnKeyDown;
    }

    private readonly InverterDetailsViewModel viewModel;

    private void OnMouseWheel(object _, MouseWheelEventArgs e)
    {
        if (e.Handled || Keyboard.IsKeyUp(Key.LeftCtrl) && Keyboard.IsKeyUp(Key.RightCtrl))
        {
            return;
        }

        e.Handled = true;

        if (e.Delta > 0)
        {
            ZoomIn();
        }
        else
        {
            ZoomOut();
        }
    }

    protected void OnKeyDown(object _, KeyEventArgs e)
    {
        if (e.Handled || Keyboard.IsKeyUp(Key.LeftCtrl) && Keyboard.IsKeyUp(Key.RightCtrl)) return;

        e.Handled = true;

        switch (e.Key)
        {
            case Key.Add:
            case Key.OemPlus:
                ZoomIn();
                break;

            case Key.Subtract:
            case Key.OemMinus:
                ZoomOut();
                break;

            case Key.NumPad0:
            case Key.D0:
                Zoom0();
                break;

            default:
                e.Handled = false;
                break;
        }
    }

    private void ZoomIn()
    {
        Scaler.ScaleX *= App.ZoomFactor;
        //Scaler.ScaleY = Scaler.ScaleX;
    }

    private void ZoomOut()
    {
        Scaler.ScaleX /= App.ZoomFactor;
        //ConsumerScaler.ScaleY = ConsumerScaler.ScaleX;
    }

    private void Zoom0()
    {
        Scaler.ScaleX = 1;
    }

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
}