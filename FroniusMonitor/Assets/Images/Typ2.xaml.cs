namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class Typ2
{
    public static readonly DependencyProperty WattPilotProperty = DependencyProperty.Register
    (
        nameof(WattPilot), typeof(WattPilot), typeof(Typ2),
        new PropertyMetadata((d, e) => ((Typ2)d).OnWattPilotChanged(e))
    );

    public WattPilot? WattPilot
    {
        get => (WattPilot)GetValue(WattPilotProperty);
        set => SetValue(WattPilotProperty, value);
    }

    public Typ2()
    {
        InitializeComponent();
    }

    private void OnWattPilotChanged(DependencyPropertyChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (e.OldValue is WattPilot oldWattPilot)
            {
                oldWattPilot.PropertyChanged -= WattPilot_PropertyChanged!;
            }

            if (e.NewValue is WattPilot newWattPilot)
            {
                newWattPilot.PropertyChanged += WattPilot_PropertyChanged!;
                var colorL1 = GetColor(newWattPilot.L1CableEnabled, newWattPilot.L1CarEnabled);
                var colorL2 = GetColor(newWattPilot.L2CableEnabled, newWattPilot.L2CarEnabled);
                var colorL3 = GetColor(newWattPilot.L3CableEnabled, newWattPilot.L3CarEnabled);
                L1.Fill = colorL1;
                L2.Fill = colorL2;
                L3.Fill = colorL3;
                UpdatePeAndN(colorL1, colorL2, colorL3);
            }
        });
    }

    private void WattPilot_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var wattPilot = (WattPilot)sender;

            switch (e.PropertyName)
            {
                case nameof(wattPilot.L1CarEnabled):
                case nameof(wattPilot.L1CableEnabled):
                    var colorL1 = GetColor(wattPilot.L1CableEnabled, wattPilot.L1CarEnabled);
                    L1.Fill = colorL1;
                    break;

                case nameof(wattPilot.L2CarEnabled):
                case nameof(wattPilot.L2CableEnabled):
                    var colorL2 = GetColor(wattPilot.L2CableEnabled, wattPilot.L2CarEnabled);
                    L2.Fill = colorL2;
                    break;

                case nameof(wattPilot.L3CarEnabled):
                case nameof(wattPilot.L3CableEnabled):
                    var colorL3 = GetColor(wattPilot.L3CableEnabled, wattPilot.L3CarEnabled);
                    L3.Fill = colorL3;
                    break;
            }

            UpdatePeAndN((SolidColorBrush)L1.Fill, (SolidColorBrush)L2.Fill, (SolidColorBrush)L3.Fill);
        });
    }

    private void UpdatePeAndN(SolidColorBrush l1, SolidColorBrush l2, SolidColorBrush l3)
    {
        if (IsAny(Colors.LightGreen, l1, l2, l3))
        {
            N.Fill = Pe.Fill = Brushes.LightGreen;
            return;
        }

        if (IsAny(Color.FromRgb(248, 232, 19), l1, l2, l3))
        {
            var brush = new SolidColorBrush(Color.FromRgb(248, 232, 19));
            brush.Freeze();
            N.Fill = Pe.Fill = brush;
            return;
        }

        if (IsAny(Colors.White, l1, l2, l3))
        {
            N.Fill = Pe.Fill = Brushes.White;
            return;
        }

        N.Fill = Pe.Fill = Brushes.Transparent;

        bool IsAny(Color color, params SolidColorBrush[] brushes)
        {
            return brushes.Any(b => b.Color == color);
        }
    }

    private SolidColorBrush GetColor(bool? cableEnabled, bool? carEnabled)
    {
        if (carEnabled is null || cableEnabled is null)
        {
            return Brushes.Transparent;
        }

        if (carEnabled.Value && cableEnabled.Value)
        {
            return Brushes.LightGreen;
        }

        if (carEnabled.Value)
        {
            return Brushes.White;
        }

        var brush = new SolidColorBrush(Color.FromRgb(248, 232, 19));
        brush.Freeze();
        return brush;
    }
}