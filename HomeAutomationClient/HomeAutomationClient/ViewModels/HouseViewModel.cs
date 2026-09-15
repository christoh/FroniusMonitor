using System.Collections.Specialized;
using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

/// <summary>
/// What the house block on the dashboard shows - see <see cref="HousePower"/> for the figures and
/// <c>Controls/HouseControl</c> for the view. Follows the <see cref="IUpdateService"/>: the site power flow,
/// which the service replaces at logout, the Wattpilots as they come and go, and the peak power that scales the
/// gauges. A singleton that lives as long as the app, so nothing here is ever unsubscribed.
/// </summary>
/// <remarks>
/// The events arrive on the hub's thread and the properties are set there. That is fine for bindings, which the
/// framework marshals; it is why this class binds plain values and never touches a collection of the view.
/// </remarks>
public sealed partial class HouseViewModel : ViewModelBase
{
    /// <summary>The scale of the power gauges where no inverter has told its peak power yet.</summary>
    private const double DefaultPowerMaximum = 10_000;

    /// <summary>The scale of a Wattpilot whose possible charging power is not known: 16 A on three phases.</summary>
    private const double DefaultCarPowerMaximum = 11_000;

    private readonly IUpdateService updateService;
    private readonly List<WattPilot> wattPilots = [];
    private Gen24PowerFlow? flow;

    public HouseViewModel(IUpdateService updateService)
    {
        this.updateService = updateService;

        if (updateService is INotifyPropertyChanged notifying)
        {
            notifying.PropertyChanged += OnUpdateServiceChanged;
        }

        updateService.Inverters.CollectionChanged += (_, _) => Update();
        updateService.AllPowerConsumers.CollectionChanged += OnPowerConsumersChanged;
        FollowFlow();
        FollowWattPilots();
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasCars))]
    public partial HousePower Power { get; private set; } = HousePower.None;

    /// <summary>The cars' row is only shown where there is a Wattpilot.</summary>
    public bool HasCars => Power.CarPower != null;

    /// <summary>
    /// The scale of the house gauge: the peak power of all panels. A house that draws more than the roof could
    /// ever deliver is at the red end.
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LossMaximum), nameof(SolarMaximum))]
    public partial double PowerMaximum { get; private set; } = DefaultPowerMaximum;

    /// <summary>The scale of the loss gauge: a twentieth of the peak power, a few percent being what an inverter loses at full load.</summary>
    public double LossMaximum => PowerMaximum * .02;

    /// <summary>The scale of the solar gauge: 70 % of the peak power, which is about what the panels deliver on a clear summer day.</summary>
    public double SolarMaximum => PowerMaximum * 0.7;

    /// <summary>The scale of the cars' gauge: what all Wattpilots could draw at once.</summary>
    [ObservableProperty]
    public partial double CarPowerMaximum { get; private set; } = DefaultCarPowerMaximum;

    private void OnUpdateServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IUpdateService.SitePowerFlow) or null or "")
        {
            FollowFlow();
            return;
        }

        Update();
    }

    private void FollowFlow()
    {
        if (flow != null)
        {
            flow.PropertyChanged -= OnFlowChanged;
        }

        flow = updateService.SitePowerFlow;
        flow.PropertyChanged += OnFlowChanged;
        Update();
    }

    private void OnFlowChanged(object? sender, PropertyChangedEventArgs e) => Update();

    private void OnPowerConsumersChanged(object? sender, NotifyCollectionChangedEventArgs e) => FollowWattPilots();

    private void FollowWattPilots()
    {
        lock (wattPilots)
        {
            wattPilots.ForEach(w => w.PropertyChanged -= OnWattPilotChanged);
            wattPilots.Clear();
            wattPilots.AddRange(updateService.AllPowerConsumers.OfType<KeyedWattPilot>().Select(k => k.Device));
            wattPilots.ForEach(w => w.PropertyChanged += OnWattPilotChanged);
        }

        Update();
    }

    private void OnWattPilotChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WattPilot.PowerTotal) or nameof(WattPilot.MaximumChargingPowerPossibleSum) or null or "")
        {
            Update();
        }
    }

    private void Update()
    {
        double? carPower;
        double carPowerMaximum;

        lock (wattPilots)
        {
            carPower = HousePower.CarPowerOf(wattPilots);
            carPowerMaximum = wattPilots.Count == 0 ? DefaultCarPowerMaximum : wattPilots.Sum(w => w.MaximumChargingPowerPossibleSum > 0 ? w.MaximumChargingPowerPossibleSum : DefaultCarPowerMaximum);
        }

        // The site power flow is all zeros until the first inverter has reported; zeros would read as a house
        // that consumes nothing.
        Power = HousePower.From(updateService.Inverters.Count > 0 ? flow : null, carPower);
        PowerMaximum = updateService.SitePvPeakPower > 0 ? updateService.SitePvPeakPower : DefaultPowerMaximum;
        CarPowerMaximum = carPowerMaximum;
    }
}
