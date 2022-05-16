using System.Diagnostics.CodeAnalysis;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class ModbusViewModel : ViewModelBase
    {
        private readonly IWebClientService webClientService;
        private readonly IGen24JsonService gen24Service;
        private Gen24ModbusSettings oldSettings = null!;
        private ModbusView view=null!;

        public ModbusViewModel(IGen24JsonService gen24Service, IWebClientService webClientService)
        {
            this.webClientService = webClientService;
            this.gen24Service = gen24Service;
        }

        private Gen24ModbusSettings settings = null!;

        public Gen24ModbusSettings Settings
        {
            get => settings;
            set => Set(ref settings, value);
        }

        public IReadOnlyList<ModbusInterfaceRole> ModBusInterfaceRoles => Enum.GetValues<ModbusInterfaceRole>();

        public IReadOnlyList<ModbusParity> ModBusParities => Enum.GetValues<ModbusParity>();

        public IReadOnlyList<SunspecMode> SunspecModes => Enum.GetValues<SunspecMode>();

        private bool enableTcp;

        public bool EnableTcp
        {
            get => enableTcp;
            set => Set(ref enableTcp, value);
        }

        private ICommand? applyCommand;
        public ICommand ApplyCommand => applyCommand ??= new NoParameterCommand(Apply);

        private ICommand? undoCommand;
        public ICommand UndoCommand => undoCommand ??= new NoParameterCommand(Undo);

        internal override async Task OnInitialize()
        {
            await base.OnInitialize().ConfigureAwait(false);
            view = IoC.Get<MainWindow>().ModbusView;
            oldSettings = Gen24ModbusSettings.Parse(gen24Service, await webClientService.GetFroniusJsonResponse("config/modbus").ConfigureAwait(false));
            Undo();
        }

        private void Undo()
        {
            Settings = (Gen24ModbusSettings)oldSettings.Clone();
            EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
        }

        public async void Apply()
        {
            var isModbusRtu = Settings.Rtu0 == ModbusInterfaceRole.Slave || Settings.Rtu1 == ModbusInterfaceRole.Slave;
            var isAnyModbus = isModbusRtu || EnableTcp;

            Settings.InverterAddress = !isAnyModbus ? (byte)0 : (byte)1;

            if (EnableTcp && isModbusRtu)
            {
                Settings.Mode = ModbusSlaveMode.Both;
            }
            else if (EnableTcp)
            {
                Settings.Mode = ModbusSlaveMode.Tcp;
            }
            else if (isModbusRtu)
            {
                Settings.Mode = ModbusSlaveMode.Rtu;
            }
            else
            {
                Settings.Mode = ModbusSlaveMode.Off;
            }

            var updateToken = Settings.GetToken(gen24Service, oldSettings);

            if (!updateToken.Children().Any())
            {
                await Dispatcher.InvokeAsync(() => MessageBox.Show(view, Resources.NoSettingsChanged, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning));
                return;
            }

            try
            {
                var _ = await webClientService.GetFroniusJsonResponse("config/modbus", updateToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => MessageBox.Show
                (
                    view, string.Format(Resources.InverterCommError, ex.Message)+Environment.NewLine+Environment.NewLine+updateToken,
                    ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
                ));

                return;
            }

            oldSettings = Settings;
            Undo();
        }
    }
}
