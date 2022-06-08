namespace De.Hochstaetter.Fronius.Models.Charging
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class WattPilot : BindableBase
    {
        private string? serialNumber;
        [FroniusProprietaryImport("serial", FroniusDataType.Root)]
        public string? SerialNumber
        {
            get => serialNumber;
            set => Set(ref serialNumber, value);
        }

        private string? hostName;
        [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
        public string? HostName
        {
            get => hostName;
            set => Set(ref hostName, value);
        }

        private string? deviceName;
        [FroniusProprietaryImport("friendly_name", FroniusDataType.Root)]
        public string? DeviceName
        {
            get => deviceName;
            set => Set(ref deviceName, value);
        }

        private string? manufacturer;
        [FroniusProprietaryImport("manufacturer", FroniusDataType.Root)]
        public string? Manufacturer
        {
            get => manufacturer;
            set => Set(ref manufacturer, value);
        }

        private string? model;
        [FroniusProprietaryImport("devicetype", FroniusDataType.Root)]
        public string? Model
        {
            get => model;
            set => Set(ref model, value);
        }

        private Version? version;
        [FroniusProprietaryImport("version", FroniusDataType.Root)]
        public Version? Version
        {
            get => version;
            set => Set(ref version, value);
        }

        private int? protocol;
        [FroniusProprietaryImport("protocol", FroniusDataType.Root)]
        public int? Protocol
        {
            get => protocol;
            set => Set(ref protocol, value);
        }

        private bool? isSecured;
        [FroniusProprietaryImport("secured", FroniusDataType.Root)]
        public bool? IsSecured
        {
            get => isSecured;
            set => Set(ref isSecured, value);
        }

        private double? voltageL1;

        public double? VoltageL1
        {
            get => voltageL1;
            set => Set(ref voltageL1, value);
        }

        private double? voltageL2;

        public double? VoltageL2
        {
            get => voltageL2;
            set => Set(ref voltageL2, value);
        }

        private double voltageL3;

        public double VoltageL3
        {
            get => voltageL3;
            set => Set(ref voltageL3, value);
        }

        private double voltageN;

        public double VoltageN
        {
            get => voltageN;
            set => Set(ref voltageN, value);
        }

        private double? currentL1;

        public double? CurrentL1
        {
            get => currentL1;
            set => Set(ref currentL1, value);
        }

        private double? currentL2;

        public double? CurrentL2
        {
            get => currentL2;
            set => Set(ref currentL2, value);
        }

        private double? currentL3;

        public double? CurrentL3
        {
            get => currentL3;
            set => Set(ref currentL3, value);
        }

        private double? powerL1;

        public double? PowerL1
        {
            get => powerL1;
            set => Set(ref powerL1, value);
        }

        private double? powerL2;

        public double? PowerL2
        {
            get => powerL2;
            set => Set(ref powerL2, value);
        }

        private double? powerL3;

        public double? PowerL3
        {
            get => powerL3;
            set => Set(ref powerL3, value);
        }

        private double? powerN;

        public double? PowerN
        {
            get => powerN;
            set => Set(ref powerN, value);
        }

        private double? powerTotal;

        public double? PowerTotal
        {
            get => powerTotal;
            set => Set(ref powerTotal, value);
        }

        private double? powerFactorL1;

        public double? PowerFactorL1
        {
            get => powerFactorL1;
            set => Set(ref powerFactorL1, value);
        }

        private double? powerFactorL2;

        public double? PowerFactorL2
        {
            get => powerFactorL2;
            set => Set(ref powerFactorL2, value);
        }

        private double? powerFactorL3;

        public double? PowerFactorL3
        {
            get => powerFactorL3;
            set => Set(ref powerFactorL3, value);
        }

        private double? powerFactorN;

        public double? PowerFactorN
        {
            get => powerFactorN;
            set => Set(ref powerFactorN, value);
        }

        private ModelStatus? status;

        public ModelStatus? Status
        {
            get => status;
            set => Set(ref status, value);
        }

        private ModelStatus? statusInternal;

        public ModelStatus? StatusInternal
        {
            get => statusInternal;
            set => Set(ref statusInternal, value);
        }

        private double? minimumChargingCurrent;

        public double? MinimumChargingCurrent
        {
            get => minimumChargingCurrent;
            set => Set(ref minimumChargingCurrent, value);
        }

        private double? maximumChargingCurrent;

        public double? MaximumChargingCurrent
        {
            get => maximumChargingCurrent;
            set => Set(ref maximumChargingCurrent, value);
        }

        private double? chargingCurrent;

        public double? ChargingCurrent
        {
            get => chargingCurrent;
            set => Set(ref chargingCurrent, value);
        }

        private string? downloadLink;

        public string? DownloadLink
        {
            get => downloadLink;
            set => Set(ref downloadLink, value);
        }

        public static WattPilot Parse(JToken token)
        {
            return IoC.Get<IGen24JsonService>().ReadFroniusData<WattPilot>(token);
        }
    }
}
