namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IDataCollectionService
    {
        HomeAutomationSystem? HomeAutomationSystem { get; }
        WebConnection? WattPilotConnection { get; set; }
        public IToshibaHvacService HvacService { get; }
        public IWebClientService WebClientService { get; }
        public IWebClientService? WebClientService2 { get; set; }
        public BindableCollection<ISwitchable>? SwitchableDevices { get; }

        public int FroniusUpdateRate { get; set; }
        IList<SmartMeterCalibrationHistoryItem> SmartMeterHistory { get; }

        event EventHandler<SolarDataEventArgs>? NewDataReceived;

        Task Start(WebConnection? inverterConnection, WebConnection? inverter2Connection, WebConnection? fritzBoxConnection, WebConnection? wattPilotConnection);
        void Stop();
        void SuspendPowerConsumers();
        void ResumePowerConsumers();
        void InvalidateFritzBox();
        Task<IList<SmartMeterCalibrationHistoryItem>> ReadCalibrationHistory();
        Task<IList<SmartMeterCalibrationHistoryItem>> AddCalibrationHistoryItem(double consumedEnergyOffsetWattHours, double producedEnergyOffsetWattHours);
        IServiceProvider Container { get; }
        IServiceProvider? Container2 { get; }
    }
}
