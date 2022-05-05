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
        InitializeComponent();

        DataContext = vm;

        Loaded += async (_, _) =>
        {
            ViewModel.Dispatcher = Dispatcher;
            var binding = new Binding($"{nameof(ViewModel.SolarSystemService)}.{nameof(ViewModel.SolarSystemService.SolarSystem)}.{nameof(ViewModel.SolarSystemService.SolarSystem.Gen24System)}.{nameof(ViewModel.SolarSystemService.SolarSystem.Gen24System.PowerFlow)}");
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

        LoadArrow.Power = PowerFlow.LoadPower - (ViewModel.IncludeInverterPower ? ViewModel.SolarSystemService.PowerLossAvg : 0);

        if (PowerFlow.LoadPower > 0)
        {
            LoadArrow.Fill = Brushes.LightGray;
            return;
        }

        var totalIncomingPower = new[] {PowerFlow.SolarPower, PowerFlow.StoragePower, PowerFlow.GridPower}.Where(ps => ps is > 0).Select(ps => ps!.Value).Sum();

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

        byte Round(double value) => (byte)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private void OnAutoSizeChecked(object sender, RoutedEventArgs e)
    {
        PowerConsumerColumn.Width = new GridLength(0, GridUnitType.Auto);
    }
}
