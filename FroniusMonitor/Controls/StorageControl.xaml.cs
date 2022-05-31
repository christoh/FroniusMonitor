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

    public static readonly DependencyProperty Gen24Property = DependencyProperty.Register
    (
        nameof(Gen24), typeof(Gen24System), typeof(StorageControl),
        new PropertyMetadata((d, e) => ((StorageControl)d).OnStorageChanged(e))
    );

    public Gen24System? Gen24
    {
        get => (Gen24System?)GetValue(Gen24Property);
        set => SetValue(Gen24Property, value);
    }

    public StorageControl()
    {
        InitializeComponent();

        Unloaded += (_, _) =>
        {
            if (Gen24 != null)
            {
                Gen24.PropertyChanged -= OnDataChanged;
            }
        };
    }

    private void OnStorageChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is Storage oldStorage)
        {
            oldStorage.PropertyChanged -= OnDataChanged;
        }

        if (Gen24 != null)
        {
            Gen24.PropertyChanged += OnDataChanged;
        }

        OnDataChanged();
    }


    private void OnDataChanged(object? _ = null, PropertyChangedEventArgs? __ = null)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var storage = Gen24?.Storage;

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
        });
    }
}
