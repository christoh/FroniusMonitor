namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class StorageControl
{
    private bool isInChargingAnimation;

    private static readonly ColorAnimation chargingAnimation = new()
    {
        To = Color.FromRgb(0, 136, 178),
        AutoReverse = true,
        RepeatBehavior = RepeatBehavior.Forever,
        Duration = TimeSpan.FromSeconds(1.5),
    };

    public static readonly DependencyProperty Gen24SensorsProperty = DependencyProperty.Register
    (
        nameof(Gen24Sensors), typeof(Gen24Sensors), typeof(StorageControl),
        new PropertyMetadata((d, e) => ((StorageControl)d).OnDataChanged())
    );

    public Gen24Sensors? Gen24Sensors
    {
        get => (Gen24Sensors?)GetValue(Gen24SensorsProperty);
        set => SetValue(Gen24SensorsProperty, value);
    }

    public static readonly DependencyProperty HomeAutomationSystemProperty = DependencyProperty.Register
    (
        nameof(HomeAutomationSystem), typeof(HomeAutomationSystem), typeof(StorageControl)
    );

    public HomeAutomationSystem HomeAutomationSystem
    {
        get => (HomeAutomationSystem)GetValue(HomeAutomationSystemProperty);
        set => SetValue(HomeAutomationSystemProperty, value);
    }

    public StorageControl()
    {
        InitializeComponent();
    }

    private void OnDataChanged()
    {
        var storage = Gen24Sensors?.Storage;

        if (storage == null)
        {
            return;
        }

        SocRectangle.Height = (storage.StateOfCharge ?? 0) * BackgroundRectangle.Height;

        if (storage.Power > 10)
        {
            if (isInChargingAnimation) return;
            isInChargingAnimation = true;
            PlusPole.Background = Enclosure.BorderBrush = new SolidColorBrush(Colors.DarkGreen);
            PlusPole.Background.BeginAnimation(SolidColorBrush.ColorProperty, chargingAnimation);
        }
        else
        {
            if (!PlusPole.Background.IsFrozen)
            {
                PlusPole.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
            }

            var brush = storage.TrafficLight switch
            {
                TrafficLight.Red => Brushes.Red,
                TrafficLight.Green => Brushes.DarkGreen,
                _ => Brushes.DarkGray
            };

            isInChargingAnimation = false;
            PlusPole.Background = Enclosure.BorderBrush = brush;
        }
    }

    private void OnDetailsClicked(object sender, RoutedEventArgs e)
    {
        IoC.Get<MainWindow>().GetView<BatteryDetailsView>(IoC.Injector).Focus();
    }
}
