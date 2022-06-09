namespace De.Hochstaetter.Fronius.Models.Charging;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class WattPilot : BindableBase, IHaveDisplayName
{
    private string? serialNumber;

    [FroniusProprietaryImport("serial", FroniusDataType.Root)]
    [WattPilot("sse")]
    public string? SerialNumber
    {
        get => serialNumber;
        set => Set(ref serialNumber, value);
    }

    private string? hostName;

    [FroniusProprietaryImport("hostname", FroniusDataType.Root)]
    [WattPilot("ffna")]
    public string? HostName
    {
        get => hostName;
        set => Set(ref hostName, value);
    }

    private string? deviceName;

    [WattPilot("fna")]
    [FroniusProprietaryImport("friendly_name", FroniusDataType.Root)]
    public string? DeviceName
    {
        get => deviceName;
        set => Set(ref deviceName, value);
    }

    private string? manufacturer;

    [WattPilot("oem")]
    [FroniusProprietaryImport("manufacturer", FroniusDataType.Root)]
    public string? Manufacturer
    {
        get => manufacturer;
        set => Set(ref manufacturer, value);
    }

    private string? model;

    [WattPilot("typ")]
    [FroniusProprietaryImport("devicetype", FroniusDataType.Root)]
    public string? Model
    {
        get => model;
        set => Set(ref model, value);
    }

    private Version? version;

    [FroniusProprietaryImport("version", FroniusDataType.Root)]
    [WattPilot("fwv")]
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

    [WattPilot("nrg", 0)]
    public double? VoltageL1
    {
        get => voltageL1;
        set => Set(ref voltageL1, value, () => NotifyOfPropertyChange(nameof(VoltageAverage)));
    }

    private double? voltageL2;

    [WattPilot("nrg", 1)]
    public double? VoltageL2
    {
        get => voltageL2;
        set => Set(ref voltageL2, value, () => NotifyOfPropertyChange(nameof(VoltageAverage)));
    }

    private double voltageL3;

    [WattPilot("nrg", 2)]
    public double VoltageL3
    {
        get => voltageL3;
        set => Set(ref voltageL3, value, () => NotifyOfPropertyChange(nameof(VoltageAverage)));
    }

    private double voltageN;

    [WattPilot("nrg", 3)]
    public double VoltageN
    {
        get => voltageN;
        set => Set(ref voltageN, value);
    }

    private double? currentL1;

    [WattPilot("nrg", 4)]
    public double? CurrentL1
    {
        get => currentL1;
        set => Set(ref currentL1, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    private double? currentL2;

    [WattPilot("nrg", 5)]
    public double? CurrentL2
    {
        get => currentL2;
        set => Set(ref currentL2, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    private double? currentL3;

    [WattPilot("nrg", 6)]
    public double? CurrentL3
    {
        get => currentL3;
        set => Set(ref currentL3, value, () => NotifyOfPropertyChange(nameof(CurrentSum)));
    }

    public double? CurrentSum => CurrentL1 + CurrentL2 + CurrentL3;

    private double? powerL1;

    [WattPilot("nrg", 7)]
    public double? PowerL1
    {
        get => powerL1;
        set => Set(ref powerL1, value);
    }

    private double? powerL2;

    [WattPilot("nrg", 8)]
    public double? PowerL2
    {
        get => powerL2;
        set => Set(ref powerL2, value);
    }

    private double? powerL3;

    [WattPilot("nrg", 9)]
    public double? PowerL3
    {
        get => powerL3;
        set => Set(ref powerL3, value);
    }

    private double? powerN;

    [WattPilot("nrg", 10)]
    public double? PowerN
    {
        get => powerN;
        set => Set(ref powerN, value);
    }

    public double? VoltageAverage => (VoltageL1 + VoltageL2 + VoltageL3) / 3;

    private double? powerTotal;

    [WattPilot("nrg", 11)]
    public double? PowerTotal
    {
        get => powerTotal;
        set => Set(ref powerTotal, value);
    }

    private double? powerFactorL1;

    [WattPilot("nrg", 12)]
    public double? PowerFactorL1
    {
        get => powerFactorL1;
        set => Set(ref powerFactorL1, value);
    }

    private double? powerFactorL2;

    [WattPilot("nrg", 13)]
    public double? PowerFactorL2
    {
        get => powerFactorL2;
        set => Set(ref powerFactorL2, value);
    }

    private double? powerFactorL3;

    [WattPilot("nrg", 14)]
    public double? PowerFactorL3
    {
        get => powerFactorL3;
        set => Set(ref powerFactorL3, value);
    }

    private double? powerFactorN;

    [WattPilot("nrg", 15)]
    public double? PowerFactorN
    {
        get => powerFactorN;
        set => Set(ref powerFactorN, value);
    }

    private ModelStatus? status;

    [WattPilot("modelStatus")]
    public ModelStatus? Status
    {
        get => status;
        set => Set(ref status, value, () => NotifyOfPropertyChange(nameof(StatusDisplayName)));
    }

    public string? StatusDisplayName => Status?.ToDisplayName();

    private ModelStatus? statusInternal;

    [WattPilot("msi")]
    public ModelStatus? StatusInternal
    {
        get => statusInternal;
        set => Set(ref statusInternal, value, () => NotifyOfPropertyChange(nameof(StatusInternalDisplayName)));
    }

    public string? StatusInternalDisplayName => StatusInternal?.ToDisplayName();

    private double? minimumChargingCurrent;

    [WattPilot("mca")]
    public double? MinimumChargingCurrent
    {
        get => minimumChargingCurrent;
        set => Set(ref minimumChargingCurrent, value);
    }

    private double? maximumChargingCurrent;

    [WattPilot("ama")]
    public double? MaximumChargingCurrent
    {
        get => maximumChargingCurrent;
        set => Set(ref maximumChargingCurrent, value);
    }

    private double? chargingCurrent;

    [WattPilot("amp")]
    public double? ChargingCurrent
    {
        get => chargingCurrent;
        set => Set(ref chargingCurrent, value);
    }

    private string? downloadLink;

    [WattPilot("dll")]
    public string? DownloadLink
    {
        get => downloadLink;
        set => Set(ref downloadLink, value);
    }

    private string? cloudAccessKey;

    [WattPilot("cak")]
    public string? CloudAccessKey
    {
        get => cloudAccessKey;
        set => Set(ref cloudAccessKey, value);
    }

    private bool? cloudAccessEnabled;

    [WattPilot("cae")]
    public bool? CloudAccessEnabled
    {
        get => cloudAccessEnabled;
        set => Set(ref cloudAccessEnabled, value);
    }

    private CarStatus? carStatus;
    [WattPilot("car")]
    public CarStatus? CarStatus
    {
        get => carStatus;
        set => Set(ref carStatus, value);
    }

    private double? allowedCurrent;
    [WattPilot("acu")]
    public double? AllowedCurrent
    {
        get => allowedCurrent;
        set => Set(ref allowedCurrent, value);
    }

    private double? allowedCurrentInternal;
    [WattPilot("acui")]
    public double? AllowedCurrentInternal
    {
        get => allowedCurrentInternal;
        set => Set(ref allowedCurrentInternal, value);
    }

    private bool? allowCharging;
    [WattPilot("alw")]
    public bool? AllowCharging
    {
        get => allowCharging;
        set => Set(ref allowCharging, value);
    }

    private double? cableCurrentMaximum;
    [WattPilot("cbl")]
    public double? CableCurrentMaximum
    {
        get => cableCurrentMaximum;
        set => Set(ref cableCurrentMaximum, value);
    }

    private IReadOnlyList<WattPilotCard>? cards;
    [WattPilot("cards")]
    public IReadOnlyList<WattPilotCard>? Cards
    {
        get => cards;
        set => Set(ref cards, value);
    }

    private double? frequency;
    [WattPilot("fhz")]
    public double? Frequency
    {
        get => frequency;
        set => Set(ref frequency, value);
    }

    private DateTime? timeStampUtc;
    [WattPilot("utc")]
    public DateTime? TimeStampUtc
    {
        get => timeStampUtc;
        set => Set(ref timeStampUtc, value);
    }

    public string DisplayName => $"{DeviceName ?? HostName ?? SerialNumber ?? Resources.Unknown}";

    public override string ToString() => DisplayName;

    public static WattPilot Parse(JToken token)
    {
        return IoC.Get<IGen24JsonService>().ReadFroniusData<WattPilot>(token);
    }
}
