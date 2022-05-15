namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class ModbusViewModel : ViewModelBase
    {
        private readonly IWebClientService webClientService;
        private readonly IGen24JsonService gen24Service;

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

        private bool enableTcp;

        public bool EnableTcp
        {
            get => enableTcp;
            set => Set(ref enableTcp, value);
        }

        internal override async Task OnInitialize()
        {
            await base.OnInitialize().ConfigureAwait(false);
            Settings = Gen24ModbusSettings.Parse(gen24Service, await webClientService.GetFroniusJsonResponse("config/modbus").ConfigureAwait(false));
            EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
        }
    }
}
