using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

/// <summary>
/// The power flow page: where the power of the house comes from and where it goes, as cards with animated wires
/// between them. Built from the developer's <c>Plans/PowerflowPage-plan.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// A page, not a dialog: inside the main view on the browser and the phones, a window of its own on the desktop,
/// the way a detail page is shown and unlike the price chart. So it has no parameters and no result, and it lives
/// as long as its view does - <see cref="Initialize"/> when the view is loaded, <see cref="Stop"/> when it is
/// unloaded, which on a head with one view at a time happens every time the user goes back to the dashboard.
/// </para>
/// <para>
/// Nothing is computed here that the dashboard does not already have. The sources are the update service's
/// inverters with their own power flow, the grid and the house are the site power flow, the consumers are every
/// power consumer that measures its power - and since a Wattpilot is an <c>IPowerConsumer3P</c>, that includes the
/// cars. <see cref="PowerFlowSnapshot.From"/> is the arithmetic, a pure function with its own tests.
/// </para>
/// <para>
/// Two halves, for two threads. <see cref="Snapshot"/> is rebuilt on the hub's thread whenever any followed
/// device reports, and is cheap enough to rebuild for every report. <see cref="Items"/> is what the view binds to
/// and may only be touched on the UI thread, so the view calls <see cref="Apply"/> there when it sees a new
/// snapshot - the marshalling is the view's, as the interaction rule wants, the folding is this class's.
/// </para>
/// </remarks>
public sealed partial class PowerFlowViewModel(IUpdateService updateService, IGen24LocalizationService gen24Loc) : ViewModelBase
{
    private readonly List<INotifyPropertyChanged> followedDevices = [];
    private readonly Lock followLock = new();
    private ObservableCollection<KeyedGen24System>? inverters;
    private ObservableCollection<IKeyedDevice>? consumers;
    private Gen24PowerFlow? flow;
    private bool isFollowing;

    /// <summary>The latest picture, replaced as a whole on the hub's thread. Read it, do not bind to it; bind to <see cref="Items"/>.</summary>
    [ObservableProperty]
    public partial PowerFlowSnapshot Snapshot { get; private set; } = PowerFlowSnapshot.Empty;

    /// <summary>The bindable side, kept in step by <see cref="Apply"/>.</summary>
    public PowerFlowViewModelItems Items { get; } = new();

    /// <summary>Starts following the devices. Harmless to call again while following.</summary>
    public override Task Initialize()
    {
        if (isFollowing)
        {
            return Task.CompletedTask;
        }

        isFollowing = true;

        if (updateService is INotifyPropertyChanged notifying)
        {
            notifying.PropertyChanged += OnUpdateServiceChanged;
        }

        FollowFlow();
        FollowDevices();
        return Task.CompletedTask;
    }

    /// <summary>Lets go of every device: a page that is off screen must not go on rebuilding itself.</summary>
    public void Stop()
    {
        if (!isFollowing)
        {
            return;
        }

        isFollowing = false;

        if (updateService is INotifyPropertyChanged notifying)
        {
            notifying.PropertyChanged -= OnUpdateServiceChanged;
        }

        if (flow != null)
        {
            flow.PropertyChanged -= OnDeviceChanged;
            flow = null;
        }

        lock (followLock)
        {
            CollectionFollowing.Unfollow(ref inverters, OnDevicesChanged);
            CollectionFollowing.Unfollow(ref consumers, OnDevicesChanged);
            followedDevices.ForEach(device => device.PropertyChanged -= OnDeviceChanged);
            followedDevices.Clear();
        }
    }

    /// <summary>
    /// Folds <see cref="Snapshot"/> into <see cref="Items"/>. Call it on the UI thread, and only there.
    /// </summary>
    /// <returns>True when a card came or went, so the wires have to be drawn anew.</returns>
    public bool Apply() => Items.Apply(Snapshot);

    private void OnUpdateServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The service replaces its site power flow at logout and its collections at logout and when an inverter
        // appears; the handlers move with them. Everything else it announces - the meter, the config, the battery
        // inverter, each a new object on every report - is heard from the devices themselves.
        switch (e.PropertyName)
        {
            case nameof(IUpdateService.SitePowerFlow):
                FollowFlow();
                break;

            case nameof(IUpdateService.Inverters) or nameof(IUpdateService.AllPowerConsumers):
                FollowDevices();
                break;

            case null or "":
                FollowFlow();
                FollowDevices();
                break;
        }
    }

    private void FollowFlow()
    {
        if (flow != null)
        {
            flow.PropertyChanged -= OnDeviceChanged;
        }

        flow = updateService.SitePowerFlow;
        flow.PropertyChanged += OnDeviceChanged;
        Rebuild();
    }

    private void OnDevicesChanged(object? sender, NotifyCollectionChangedEventArgs e) => FollowDevices();

    /// <summary>
    /// Every inverter and every consumer is followed as a device, because that is where the figures change: an
    /// inverter's sensors are replaced on every update, a plug's power on every reading, a Wattpilot's phases
    /// with every delta. The collections only say which devices there are.
    /// </summary>
    private void FollowDevices()
    {
        lock (followLock)
        {
            CollectionFollowing.Follow(ref inverters, updateService.Inverters, OnDevicesChanged);
            CollectionFollowing.Follow(ref consumers, updateService.AllPowerConsumers, OnDevicesChanged);
            followedDevices.ForEach(device => device.PropertyChanged -= OnDeviceChanged);
            followedDevices.Clear();
            followedDevices.AddRange(updateService.Inverters.Select(inverter => (object)inverter.Device).Concat(updateService.AllPowerConsumers.Select(consumer => consumer.Device)).OfType<INotifyPropertyChanged>());
            followedDevices.ForEach(device => device.PropertyChanged += OnDeviceChanged);
        }

        Rebuild();
    }

    private void OnDeviceChanged(object? sender, PropertyChangedEventArgs e) => Rebuild();

    /// <summary>
    /// While nobody can see the page - its window minimized, the app in the background - there is no snapshot to
    /// build; the one built when it comes back reads the devices as they are then.
    /// </summary>
    private void Rebuild() => WhenShown(RebuildNow);

    private void RebuildNow()
    {
        // Copies, because the collections are the service's and it adds to them on the same thread this runs on.
        // The site flow is all zeros until the first inverter reports; passed as null it reads as "nothing yet".
        // The trackers are named in the inverter's own words - "MPPT1", "MPPT2" in the Channels section of its
        // localization - the way the detail views name them.
        Snapshot = PowerFlowSnapshot.From([.. updateService.Inverters], updateService.Inverters.Count > 0 ? flow : null, [.. updateService.AllPowerConsumers], number => gen24Loc.GetLocalizedString(Gen24LocalizationSection.Channels, $"MPPT{number}"));
    }
}
