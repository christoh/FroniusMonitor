using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.ViewModels;

namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class MainWindow
{
    public static readonly DependencyProperty PowerFlowProperty = DependencyProperty.Register
    (
        nameof(PowerFlow), typeof(PowerFlow), typeof(MainWindow),
        new PropertyMetadata((d, _) => ((MainWindow)d).OnPowerFlowChanged())
    );

    public PowerFlow? PowerFlow
    {
        get => (PowerFlow?)GetValue(PowerFlowProperty);
        set => SetValue(PowerFlowProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();

        DataContext = IoC.Get<MainViewModel>();

        Loaded += async (_, _) =>
        {
            var binding = new Binding($"{nameof(ViewModel.SolarSystemService)}.{nameof(ViewModel.SolarSystemService.SolarSystem)}.{nameof(ViewModel.SolarSystemService.SolarSystem.PowerFlow)}");
            SetBinding(PowerFlowProperty, binding);
            await ViewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext;

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

        if (PowerFlow.LoadPower > 0)
        {
            LoadArrow.Fill = Brushes.LightGray;
            return;
        }

        LoadArrow.Power = ViewModel.SolarSystemService.LoadPowerAvg - (ViewModel.IncludeInverterPower ? ViewModel.SolarSystemService.PowerLossAvg : 0);

        IReadOnlyList<double> allIncomingPowers = new[] { ViewModel.SolarSystemService.SolarPowerAvg, ViewModel.SolarSystemService.StoragePowerAvg, ViewModel.SolarSystemService.GridPowerAvg }.Where(ps => ps is > 0).Select(ps => ps!.Value).ToArray();
        var totalIncomingPower = allIncomingPowers.Sum();

        double r = 0, g = 0, b = 0;

        if (ViewModel.SolarSystemService.SolarPowerAvg > 0)
        {
            r = 0xff * ViewModel.SolarSystemService.SolarPowerAvg.Value;
            g = 0xd0 * ViewModel.SolarSystemService.SolarPowerAvg.Value;
        }

        if (ViewModel.SolarSystemService.StoragePowerAvg > 0)
        {
            r += Colors.LightGreen.R * ViewModel.SolarSystemService.StoragePowerAvg.Value;
            g += Colors.LightGreen.G * ViewModel.SolarSystemService.StoragePowerAvg.Value;
            b += Colors.LightGreen.B * ViewModel.SolarSystemService.StoragePowerAvg.Value;
        }

        if (ViewModel.SolarSystemService.GridPowerAvg > 0)
        {
            r += Colors.LightGray.R * ViewModel.SolarSystemService.GridPowerAvg.Value;
            g += Colors.LightGray.G * ViewModel.SolarSystemService.GridPowerAvg.Value;
            b += Colors.LightGray.B * ViewModel.SolarSystemService.GridPowerAvg.Value;
        }

        r /= totalIncomingPower;
        g /= totalIncomingPower;
        b /= totalIncomingPower;

        LoadArrow.Fill = new SolidColorBrush(Color.FromRgb(Round(r), Round(g), Round(b)));

        byte Round(double value) => (byte)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
