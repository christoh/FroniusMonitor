using System.Windows;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.FroniusMonitor.Unity;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class PowerConsumer
{
    #region Dependency Properties

    public static readonly DependencyProperty PowerMeterProperty = DependencyProperty.Register
    (
        nameof(PowerMeter), typeof(IPowerMeter1P), typeof(PowerConsumer),
        new PropertyMetadata((d, e) => ((PowerConsumer)d).OnFritzBoxDeviceChanged())
    );

    public IPowerMeter1P? PowerMeter
    {
        get => (IPowerMeter1P?)GetValue(PowerMeterProperty);
        set => SetValue(PowerMeterProperty, value);
    }

    #endregion

    public PowerConsumer()
    {
        InitializeComponent();
    }

    private void OnFritzBoxDeviceChanged()
    {
        BackgroundProvider.Background = PowerMeter is not { IsPresent: true }
            ? Brushes.OrangeRed
            : PowerMeter?.IsTurnedOn == null || PowerMeter.IsTurnedOn.Value
                ? Brushes.AntiqueWhite
                : Brushes.LightGray;
    }

    private async void OnPowerButtonClick(object sender, RoutedEventArgs e)
    {
        if (PowerMeter is not { IsPresent: true, CanTurnOnOff: true }) return;
        await PowerMeter.TurnOnOff(!PowerMeter.IsTurnedOn.HasValue || !PowerMeter.IsTurnedOn.Value).ConfigureAwait(false);
    }
}
