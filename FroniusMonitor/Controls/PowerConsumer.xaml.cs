using System.Windows;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class PowerConsumer
{
    #region Dependency Properties

    public static readonly DependencyProperty FritzBoxDeviceProperty = DependencyProperty.Register
    (
        nameof(FritzBoxDevice), typeof(FritzBoxDevice), typeof(PowerConsumer),
        new PropertyMetadata((d, e) => ((PowerConsumer)d).OnFritzBoxDeviceChanged())
    );

    public FritzBoxDevice? FritzBoxDevice
    {
        get => (FritzBoxDevice?)GetValue(FritzBoxDeviceProperty);
        set => SetValue(FritzBoxDeviceProperty, value);
    }

    #endregion

    public PowerConsumer()
    {
        InitializeComponent();
    }

    private void OnFritzBoxDeviceChanged()
    {
        BackgroundProvider.Background = FritzBoxDevice is not {IsPresent: true}
            ? Brushes.OrangeRed 
            : FritzBoxDevice.SimpleSwitch?.IsTurnedOn == null || FritzBoxDevice.SimpleSwitch.IsTurnedOn.Value
                ? Brushes.AntiqueWhite
                : Brushes.LightGray;
    }
}