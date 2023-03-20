namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class MainWindow
{
    private static readonly DoubleAnimation scaleAnimation = new() { Duration = TimeSpan.FromMilliseconds(200) };
    private static readonly DoubleAnimation rotateAnimation = new() { Duration = TimeSpan.FromMilliseconds(200) };

    public static readonly DependencyProperty PowerFlowProperty = DependencyProperty.Register
    (
        nameof(PowerFlow), typeof(Gen24PowerFlow), typeof(MainWindow),
        new PropertyMetadata((d, _) => ((MainWindow)d).OnPowerFlowChanged())
    );

    public Gen24PowerFlow? PowerFlow
    {
        get => (Gen24PowerFlow?)GetValue(PowerFlowProperty);
        set => SetValue(PowerFlowProperty, value);
    }

    public MainWindow(MainViewModel vm)
    {
        Initialized += (_, _) =>
        {
            if (App.Settings.MainWindowSize is { IsEmpty: false })
            {
                Width = App.Settings.MainWindowSize.Value.Width;
                Height = App.Settings.MainWindowSize.Value.Height;
            }
        };

        InitializeComponent();

        DataContext = vm;

        Loaded += (_, _) =>
        {
            ViewModel.Dispatcher = Dispatcher;
            var binding = new Binding($"{nameof(ViewModel.SolarSystemService)}.{nameof(ViewModel.SolarSystemService.SolarSystem)}.{nameof(ViewModel.SolarSystemService.SolarSystem.Gen24System)}.{nameof(ViewModel.SolarSystemService.SolarSystem.Gen24System.PowerFlow)}");
            SetBinding(PowerFlowProperty, binding);
            ViewModel.View = this;
            _ = ViewModel.OnInitialize();
        };

        SizeChanged += (_, e) =>
        {
            if (IsLoaded)
            {
                App.Settings.MainWindowSize = e.NewSize;
                Settings.Save();
            }
        };
    }

    private void OnControllerGridRowHeightChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded && e.HeightChanged && double.IsInfinity(PowerConsumerRow.MaxHeight))
        {
            App.Settings.ControllerGridRowHeight = PowerConsumerRow.ActualHeight;
            Settings.Save();
        }
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext;

    private EventLogView? eventLogView;
    public EventLogView EventLogView
    {
        get
        {
            if (eventLogView == null)
            {
                eventLogView = IoC.Get<EventLogView>();
                eventLogView.Closed += (_, _) => { eventLogView = null; };
            }

            return eventLogView;
        }
    }

    public ModbusView ModbusView => GetView<ModbusView>();

    public SettingsView SettingsView => GetView<SettingsView>();

    public WattPilotSettingsView WattPilotSettingsView => GetView<WattPilotSettingsView>();

    public T GetView<T>() where T : Window
    {
        var view = OwnedWindows.OfType<T>().SingleOrDefault();

        if (view == null)
        {
            view = IoC.Get<T>();
            view.Owner = this;
            view.Show();
        }

        return view;
    }

    private SelfConsumptionOptimizationView? selfConsumptionOptimizationView;
    public SelfConsumptionOptimizationView SelfConsumptionOptimizationView
    {
        get
        {
            if (selfConsumptionOptimizationView == null)
            {
                selfConsumptionOptimizationView = IoC.Get<SelfConsumptionOptimizationView>();
                selfConsumptionOptimizationView.Closed += (_, _) => { selfConsumptionOptimizationView = null; };
            }

            return selfConsumptionOptimizationView!;
        }
    }


    private void ZoomIn()
    {
        ConsumerScaler.ScaleX *= App.ZoomFactor;
        ConsumerScaler.ScaleY = ConsumerScaler.ScaleX;
    }

    private void ZoomOut()
    {
        ConsumerScaler.ScaleX /= App.ZoomFactor;
        ConsumerScaler.ScaleY = ConsumerScaler.ScaleX;
    }

    private void Zoom0()
    {
        ConsumerScaler.ScaleX = ConsumerScaler.ScaleY = 1;
    }


    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
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

    protected override void OnPreviewKeyDown(KeyEventArgs e)
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


    private void OnPowerFlowChanged()
    {
        if (PowerFlow is null)
        {
            return;
        }

        LoadArrow.Power = PowerFlow.LoadPower - (ViewModel.IncludeInverterPower ? ViewModel.SolarSystemService.PowerLossAvg : 0);

        if (LoadArrow.Power > 0)
        {
            LoadArrow.Fill = Brushes.Salmon;
            return;
        }

        var totalIncomingPower = new[] { PowerFlow.SolarPower, PowerFlow.StoragePower, PowerFlow.GridPower }.Where(ps => ps is > 0).Select(ps => ps!.Value).Sum();

        double r = 0, g = 0, b = 0;

        if (PowerFlow.SolarPower > 0)
        {
            r = 0xff * PowerFlow.SolarPower.Value;
            g = 0xd0 * PowerFlow.SolarPower.Value;
        }

        if (PowerFlow.StoragePower > 0)
        {
            r += Colors.LightGreen.R * PowerFlow.StoragePower.Value;
            g += Colors.LightGreen.G * PowerFlow.StoragePower.Value;
            b += Colors.LightGreen.B * PowerFlow.StoragePower.Value;
        }

        if (PowerFlow.GridPower > 0)
        {
            r += Colors.LightGray.R * PowerFlow.GridPower.Value;
            g += Colors.LightGray.G * PowerFlow.GridPower.Value;
            b += Colors.LightGray.B * PowerFlow.GridPower.Value;
        }

        r /= totalIncomingPower;
        g /= totalIncomingPower;
        b /= totalIncomingPower;

        LoadArrow.Fill = new SolidColorBrush(Color.FromRgb(Round(r), Round(g), Round(b)));

        static byte Round(double value) => (byte)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private void OnAutoSizeChecked(object sender, RoutedEventArgs e)
    {
        SolarSystemRow.Height = new GridLength(1, GridUnitType.Star);
    }

    private void ShowEventLog(object sender, RoutedEventArgs e)
    {
        if (eventLogView != null)
        {
            EventLogView.Activate();
            EventLogView.WindowState = WindowState.Normal;
        }

        EventLogView.Owner = this;
        EventLogView.Show();
    }

    private void ShowSelfConsumptionOptimization(object sender, RoutedEventArgs e)
    {
        if (selfConsumptionOptimizationView != null)
        {
            SelfConsumptionOptimizationView.Activate();
            SelfConsumptionOptimizationView.WindowState = WindowState.Normal;
        }

        SelfConsumptionOptimizationView.Owner = this;
        SelfConsumptionOptimizationView.Show();
    }

    private void ShowModbusSettings(object sender, RoutedEventArgs e)
    {
        ModbusView.Activate();
        ModbusView.WindowState = WindowState.Normal;
    }

    private void ShowSettings(object sender, RoutedEventArgs e)
    {
        SettingsView.Activate();
        SettingsView.WindowState = WindowState.Normal;
    }

    private void ShowWattPilotSettings(object sender, RoutedEventArgs e)
    {
        WattPilotSettingsView.Activate();
        WattPilotSettingsView.WindowState = WindowState.Normal;
    }

    private void OnRibbonExpanderChanged(object sender, RoutedEventArgs e)
    {
        if (RibbonExpander.IsChecked.HasValue && RibbonExpander.IsChecked.Value)
        {
            rotateAnimation.From = 180;
            rotateAnimation.To = 360;
            scaleAnimation.To = 1;
        }
        else
        {
            rotateAnimation.From = 360;
            rotateAnimation.To = 180;
            scaleAnimation.To = 0;
        }

        Chevron?.BeginAnimation(Chevron.AngleProperty, rotateAnimation);
        RibbonTransform?.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
    }

    private void OnShowAvmChanged(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem showAvm)
        {
            ViewModel.FritzBoxVisibilityChanged(showAvm.IsChecked);
        }
    }

    private void ShowWattPilot(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem showAvm)
        {
            ViewModel.WattPilotVisibilityChanged(showAvm.IsChecked);
        }
    }

    private void SaveSettings(object sender, RoutedEventArgs e)
    {
        Settings.Save();
    }
}
