using System.Windows;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.FroniusMonitor.Unity;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class PowerConsumer
{
    private static readonly ISolarSystemService solarSystemService = IoC.TryGet<ISolarSystemService>();

    #region Dependency Properties

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register
    (
        nameof(Device), typeof(IPowerConsumer1P), typeof(PowerConsumer),
        new PropertyMetadata((d, _) => ((PowerConsumer)d).OnFritzBoxDeviceChanged())
    );

    public IPowerConsumer1P? Device
    {
        get => (IPowerConsumer1P?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    #endregion

    public PowerConsumer()
    {
        InitializeComponent();
    }

    private void OnFritzBoxDeviceChanged()
    {
        BackgroundProvider.Background = Device is not {IsPresent: true}
            ? Brushes.OrangeRed
            : Device?.IsTurnedOn == null || Device.IsTurnedOn.Value
                ? Brushes.AntiqueWhite
                : Brushes.LightGray;
    }

    private async void OnPowerButtonClick(object sender, RoutedEventArgs e)
    {
        if (Device is not {IsPresent: true, CanSwitch: true}) return;
        solarSystemService.SuspendPowerConsumers();

        try
        {
            await Device.TurnOnOff(!Device.IsTurnedOn.HasValue || !Device.IsTurnedOn.Value).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
        }
        finally
        {
            solarSystemService.ResumePowerConsumers();
        }
    }

    private void OnDimLevelChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Device?.Level is not { } level || Math.Abs(e.NewValue - level) < .00001)
        {
            return;
        }

        Device.SetLevel(e.NewValue);
    }
}
