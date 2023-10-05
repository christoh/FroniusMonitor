using Microsoft.Extensions.DependencyInjection;

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

    public static readonly DependencyProperty PowerFlow2Property = DependencyProperty.Register
    (
        nameof(PowerFlow2), typeof(Gen24PowerFlow), typeof(MainWindow),
        new PropertyMetadata((d, _) => ((MainWindow)d).OnPowerFlowChanged())
    );

    public Gen24PowerFlow? PowerFlow2
    {
        get => (Gen24PowerFlow?)GetValue(PowerFlow2Property);
        set => SetValue(PowerFlow2Property, value);
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
        vm.View = this;

        Loaded += (_, _) =>
        {
            ViewModel.Dispatcher = Dispatcher;
            var binding = new Binding($"{nameof(ViewModel.DataCollectionService)}.{nameof(ViewModel.DataCollectionService.HomeAutomationSystem)}.{nameof(ViewModel.DataCollectionService.HomeAutomationSystem.Gen24Sensors)}.{nameof(ViewModel.DataCollectionService.HomeAutomationSystem.Gen24Sensors.PowerFlow)}");
            SetBinding(PowerFlowProperty, binding);
            binding = new Binding($"{nameof(ViewModel.DataCollectionService)}.{nameof(ViewModel.DataCollectionService.HomeAutomationSystem)}.{nameof(ViewModel.DataCollectionService.HomeAutomationSystem.Gen24Sensors2)}.{nameof(ViewModel.DataCollectionService.HomeAutomationSystem.Gen24Sensors2.PowerFlow)}");
            SetBinding(PowerFlow2Property, binding);
            RibbonTransform.ScaleY = App.Settings.ShowRibbon ? 1 : 0;
            RibbonExpander.IsChecked = App.Settings.ShowRibbon;
            OnRibbonExpanderChanged();
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

    public T GetView<T>(IServiceProvider? container = null) where T : Window
    {
        var view = OwnedWindows.OfType<T>().SingleOrDefault
        (
            w => container == null ||
                 w is not IInverterScoped scoped ||
                 scoped.Gen24Service == container.GetRequiredService<IGen24Service>()
        );

        if (view == null)
        {
            view = (container ?? IoC.Injector!).GetRequiredService<T>();
            view.Owner = this;
        }

        view.Show();
        return view;
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
        if (PowerFlow is null && PowerFlow2 is null)
        {
            return;
        }

        var sitePowerFlow = ViewModel.DataCollectionService.HomeAutomationSystem?.SitePowerFlow;
        LoadArrow.Power = sitePowerFlow?.LoadPowerCorrected - (ViewModel.IncludeInverterPower ? sitePowerFlow?.LoadPowerCorrected + sitePowerFlow?.SolarPower + sitePowerFlow?.GridPowerCorrected + sitePowerFlow?.StoragePower : 0);
        LoadArrowPrimaryInverter.Power = PowerFlow?.LoadPowerCorrected - (ViewModel.IncludeInverterPower ? PowerFlow?.LoadPowerCorrected + PowerFlow?.SolarPower + PowerFlow?.GridPowerCorrected + PowerFlow?.StoragePower : 0);

        ColorLoadArrow(LoadArrow, sitePowerFlow, Brushes.Salmon);
        ColorLoadArrow(LoadArrowPrimaryInverter, PowerFlow, new SolidColorBrush(Color.FromRgb(0xff, 0xd0, 0)));
    }

    private static void ColorLoadArrow(PowerFlowArrow arrow, Gen24PowerFlow? powerFlow, Brush incomingPowerBrush)
    {
        if (powerFlow == null)
        {
            return;
        }

        if (arrow.Power > 0)
        {
            arrow.Fill = incomingPowerBrush;
            return;
        }

        var totalIncomingPower = new[] { powerFlow.SolarPower, powerFlow.StoragePower, powerFlow.GridPower }.Where(ps => ps is > 0).Select(ps => ps!.Value).Sum();

        double r = 0, g = 0, b = 0;

        if (powerFlow.SolarPower > 0)
        {
            r = 0xff * powerFlow.SolarPower.Value;
            g = 0xd0 * powerFlow.SolarPower.Value;
        }

        if (powerFlow.StoragePower > 0)
        {
            r += Colors.LightGreen.R * powerFlow.StoragePower.Value;
            g += Colors.LightGreen.G * powerFlow.StoragePower.Value;
            b += Colors.LightGreen.B * powerFlow.StoragePower.Value;
        }

        if (powerFlow.GridPower > 0)
        {
            r += Colors.LightGray.R * powerFlow.GridPower.Value;
            g += Colors.LightGray.G * powerFlow.GridPower.Value;
            b += Colors.LightGray.B * powerFlow.GridPower.Value;
        }

        r /= totalIncomingPower;
        g /= totalIncomingPower;
        b /= totalIncomingPower;

        arrow.Fill = new SolidColorBrush(Color.FromRgb(Round(r), Round(g), Round(b)));
        return;

        static byte Round(double value) => (byte)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private void OnAutoSizeChecked(object sender, RoutedEventArgs e)
    {
        SolarSystemRow.Height = new GridLength(1, GridUnitType.Star);
    }

    private void ShowEventLog(object sender, RoutedEventArgs e)
    {
        GetView<EventLogView>(IoC.Injector).Focus();
    }

    private void ShowSelfConsumptionOptimization(object sender, RoutedEventArgs e)
    {
        GetView<SelfConsumptionOptimizationView>(IoC.Injector).Focus();
    }

    private void ShowModbusSettings(object sender, RoutedEventArgs e)
    {
        GetView<ModbusView>(IoC.Injector).Focus();
    }

    private void ShowSettings(object sender, RoutedEventArgs e)
    {
        GetView<SettingsView>().Focus();
    }

    private void ShowWattPilotSettings(object sender, RoutedEventArgs e)
    {
        GetView<WattPilotSettingsView>().Focus();
    }

    private void OnRibbonExpanderChanged(object? sender = null, RoutedEventArgs? e = null)
    {
        if (RibbonExpander.IsChecked.HasValue && RibbonExpander.IsChecked.Value)
        {
            rotateAnimation.To = 180;
            scaleAnimation.To = 1;
            App.Settings.ShowRibbon = true;
        }
        else
        {
            rotateAnimation.To = 0;
            scaleAnimation.To = 0;
            App.Settings.ShowRibbon = false;
        }

        Chevron?.BeginAnimation(Chevron.AngleProperty, rotateAnimation);
        RibbonTransform?.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        Settings.Save();
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
