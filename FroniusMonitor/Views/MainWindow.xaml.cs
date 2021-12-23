using System.Windows;
using System.Windows.Data;
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
        new PropertyMetadata((d, e) => ((MainWindow)d).OnPowerFlowChanged())
    );

    public PowerFlow? PowerFlow
    {
        get => (PowerFlow?)GetValue(PowerFlowProperty);
        set => SetValue(PowerFlowProperty, value);
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

        IReadOnlyList<double> allIncomingPowers = new[] { PowerFlow.StoragePower, PowerFlow.GridPower, PowerFlow.SolarPower }.Where(ps => ps.HasValue && ps.Value > 0).Select(ps=>ps!.Value).ToArray();
        var totalIncomingPower = allIncomingPowers.Sum();

        double r=0, g=0, b=0;

        if (PowerFlow.SolarPower > 0)
        {
            r = 0xff * PowerFlow.SolarPower!.Value;
            g = 0xd0 * PowerFlow.SolarPower!.Value;
        }

        if (PowerFlow.StoragePower > 0)
        {
            r += Colors.LightGreen.R * PowerFlow.StoragePower!.Value;
            g += Colors.LightGreen.G * PowerFlow.StoragePower!.Value;
            b += Colors.LightGreen.B * PowerFlow.StoragePower!.Value;
        }

        if (PowerFlow.GridPower > 0)
        {
            r += Colors.LightGray.R * PowerFlow.GridPower!.Value;
            g += Colors.LightGray.G * PowerFlow.GridPower!.Value;
            b += Colors.LightGray.B * PowerFlow.GridPower!.Value;
        }

        r /= totalIncomingPower;
        g /= totalIncomingPower;
        b /= totalIncomingPower;

        LoadArrow.Fill = new SolidColorBrush(Color.FromRgb(Round(r),Round(g),Round(b)));

        byte Round(double value) => (byte)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public MainWindow()
    {
        InitializeComponent();

        DataContext = IoC.Get<MainViewModel>();
        Loaded += async (s, e) =>
        {
            var binding=new Binding($"{nameof(ViewModel.SolarSystemService)}.{nameof(ViewModel.SolarSystemService.SolarSystem)}.{nameof(ViewModel.SolarSystemService.SolarSystem.PowerFlow)}");
            SetBinding(PowerFlowProperty, binding);
            await ViewModel.OnInitialize().ConfigureAwait(false);
        };
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext;
}

