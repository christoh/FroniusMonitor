using System.Reflection;
using De.Hochstaetter.Fronius.Attributes;
using De.Hochstaetter.Fronius.Extensions;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class WattPilotSettingsViewModel : ViewModelBase
    {
        private static readonly CableLockBehavior[] cableLockBehaviors = Enum.GetValues<CableLockBehavior>();
        private static readonly ChargingLogic[] chargingLogicList = Enum.GetValues<ChargingLogic>();
        private static readonly AwattarCountry[] energyPriceCountries = Enum.GetValues<AwattarCountry>().OrderBy(c=>c.ToDisplayName()).ToArray();

        private readonly ISolarSystemService solarSystemService;
        private readonly IWattPilotService wattPilotService;
        private WattPilot oldWattPilot = null!;
        private readonly MainWindow mainWindow;

        public WattPilotSettingsViewModel(ISolarSystemService solarSystemService, IWattPilotService wattPilotService, MainWindow mainWindow)
        {
            this.solarSystemService = solarSystemService;
            this.wattPilotService = wattPilotService;
            this.mainWindow = mainWindow;
        }

        public static IReadOnlyList<CableLockBehavior> CableLockBehaviors => cableLockBehaviors;
        public static IReadOnlyList<ChargingLogic> ChargingLogicList => chargingLogicList;
        public static IReadOnlyList<AwattarCountry> EnergyPriceCountries => energyPriceCountries;


        private WattPilot wattPilot = null!;

        public WattPilot WattPilot
        {
            get => wattPilot;
            set => Set(ref wattPilot, value, () => NotifyOfPropertyChange(nameof(ApiLink)));
        }

        private string title = "Wattpilot";

        public string Title
        {
            get => title;
            set => Set(ref title, value);
        }

        private bool isInUpdate;

        public bool IsInUpdate
        {
            get => isInUpdate;
            set => Set(ref isInUpdate, value);
        }

        public string ApiLink => string.Format(Resources.EnableApiLink, "https://" + (WattPilot.SerialNumber ?? "<Serial>") + ".api.v3.go-e.io");

        private ICommand? applyCommand;
        public ICommand ApplyCommand => applyCommand ??= new NoParameterCommand(Apply);

        private ICommand? undoCommand;
        public ICommand UndoCommand => undoCommand ??= new NoParameterCommand(Undo);


        internal override async Task OnInitialize()
        {
            await base.OnInitialize().ConfigureAwait(false);
            var localWattPilot = wattPilotService.WattPilot ?? solarSystemService.SolarSystem?.WattPilot;

            if (localWattPilot == null)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(Resources.NoWattPilot, Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    mainWindow.SettingsView.Close();
                    mainWindow.DataContext = null;
                });

                return;
            }

            oldWattPilot = (WattPilot)localWattPilot.Clone();
            Undo();
        }

        public void Undo()
        {
            WattPilot = (WattPilot)oldWattPilot.Clone();
            Title = $"{WattPilot.DeviceName} {WattPilot.SerialNumber}";
        }

        public async void Apply()
        {
            try
            {
                IsInUpdate = true;
                wattPilotService.BeginSendValues();

                try
                {
                    var errors = new List<string>();

                    foreach (var propertyInfo in typeof(WattPilot).GetProperties().Where(p => p.GetCustomAttribute<WattPilotAttribute>() != null))
                    {
                        var oldValue = propertyInfo.GetValue(oldWattPilot);
                        var newValue = propertyInfo.GetValue(WattPilot);

                        if
                        (
                            ReferenceEquals(oldValue, newValue) || oldValue is not null && oldValue.Equals(newValue) || newValue is not null && newValue.Equals(oldValue) ||
                            propertyInfo.GetCustomAttribute<WattPilotAttribute>()!.IsReadOnly
                        )
                        {
                            continue;
                        }

                        try
                        {
                            await wattPilotService.SendValue(WattPilot, propertyInfo.Name).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{ex.GetType().Name}: {propertyInfo.Name} = '{propertyInfo.GetValue(WattPilot)}' ({ex.Message})");
                        }
                    }

                    if (errors.Count > 0)
                    {
                        var notWritten = "• " + string.Join(Environment.NewLine + "• ", errors);

                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show
                            (
                                mainWindow.WattPilotSettingsView,
                                "The following settings were not written to the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten,
                                Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error
                            );
                        });
                    }
                }
                finally
                {
                    try
                    {
                        await wattPilotService.WaitSendValues().ConfigureAwait(false);
                    }
                    catch (TimeoutException) when (wattPilotService.UnsuccessfulWrites.Count > 0)
                    {
                        var notWritten = "• " + string.Join(Environment.NewLine + "• ", wattPilotService.UnsuccessfulWrites.Select(a => a.ToString()));

                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show
                            (
                                mainWindow.WattPilotSettingsView,
                                "The following settings were not confirmed by the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten,
                                Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning
                            );
                        });
                    }
                }

            }
            finally
            {
                oldWattPilot = WattPilot;
                Undo();
                IsInUpdate = false;
            }
        }
    }
}
