namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum ModBusInterfaceRole
{
    Disabled = 0,
    Master,
    Slave,
}

public enum ModbusSlaveMode
{
    [EnumParse(ParseAs = "both")] Both,
    [EnumParse(ParseAs = "off")] Off,
    [EnumParse(ParseAs = "rtu")] Rtu,
    [EnumParse(ParseAs = "tcp")] Tcp,
}

public enum ModbusParity
{
    [EnumParse(ParseAs = "n")] None,
    [EnumParse(ParseAs = "e")] Even,
    [EnumParse(ParseAs = "o")] Odd,
}

public enum SunspecMode
{
    [EnumParse(ParseAs = "float")] Float,
    [EnumParse(ParseAs = "int")] Int,
}

public class Gen24ModbusSettings : BindableBase, ICloneable
{
    private static readonly IReadOnlyList<int> baudRates = new[] { 9600, 19200 };
    
    public IReadOnlyList<int> BaudRates => baudRates;

    private ModBusInterfaceRole rtu0 = ModBusInterfaceRole.Disabled;

    public ModBusInterfaceRole Rtu0
    {
        get => rtu0;
        set => Set(ref rtu0, value);
    }

    private ModBusInterfaceRole rtu1 = ModBusInterfaceRole.Disabled;

    public ModBusInterfaceRole Rtu1
    {
        get => rtu1;
        set => Set(ref rtu1, value);
    }

    private bool? isDemoMode;
    [FroniusProprietaryImport("demo", FroniusDataType.Root)]
    public bool? IsDemoMode
    {
        get => isDemoMode;
        set => Set(ref isDemoMode, value);
    }

    private byte? meterAddress; // Range 1-247, default 200
    [FroniusProprietaryImport("meterAddress", FroniusDataType.Root)]
    public byte? MeterAddress
    {
        get => meterAddress;

        set => Set(ref meterAddress, value, () =>
        {
            if (value is < 1 or > 247)
            {
                throw new ArgumentOutOfRangeException(Resources.MeterAddressError, null as Exception);
            }
        });
    }

    private ModbusSlaveMode? mode;
    [FroniusProprietaryImport("mode", FroniusDataType.Root)]
    public ModbusSlaveMode? Mode
    {
        get => mode;
        set => Set(ref mode, value);
    }

    private ModbusParity? parity;
    [FroniusProprietaryImport("parity", FroniusDataType.Root)]
    public ModbusParity? Parity
    {
        get => parity;
        set => Set(ref parity, value);
    }

    private ushort? tcpPort;
    [FroniusProprietaryImport("port",FroniusDataType.Root)]
    public ushort? TcpPort
    {
        get => tcpPort;
        set => Set(ref tcpPort, value);
    }

    private byte? sunSpecAddress;
    [FroniusProprietaryImport("scAddress",FroniusDataType.Root)]
    public byte? SunSpecAddress
    {
        get => sunSpecAddress;
        set => Set(ref sunSpecAddress, value, () =>
        {
            if (value is 0)
            {
                throw new ArgumentOutOfRangeException(Resources.SunspecAddressError, null as Exception);
            }
        });
    }

    private byte? inverterAddress;
    [FroniusProprietaryImport("rtu_inverter_slave_id", FroniusDataType.Root)]
    public byte? InverterAddress
    {
        get => inverterAddress;
        set => Set(ref inverterAddress, value);
    }

    private SunspecMode? sunspecMode;
    [FroniusProprietaryImport("sunspecMode", FroniusDataType.Root)]
    public SunspecMode? SunspecMode
    {
        get => sunspecMode;
        set => Set(ref sunspecMode, value);
    }

    private bool allowControl;

    public bool AllowControl
    {
        get => allowControl;
        set => Set(ref allowControl, value);
    }

    private bool restrictControl;

    public bool RestrictControl
    {
        get => restrictControl;
        set => Set(ref restrictControl, value);
    }

    private string ipAddress = string.Empty;

    public string IpAddress
    {
        get => ipAddress;
        set => Set(ref ipAddress, value);
    }

    public static Gen24ModbusSettings Parse(IGen24JsonService gen24JsonService,string json)
    {
        var token = JToken.Parse(json);

        if (token == null)
        {
            throw new NullReferenceException("No Modbus config present");
        }

        var result=gen24JsonService.ReadFroniusData<Gen24ModbusSettings>(token["slave"]);
        var masterInterfaces = GetInterfaces("master");
        var slaveInterfaces=GetInterfaces("slave");

        SetInterfaces(slaveInterfaces,ModBusInterfaceRole.Slave);
        SetInterfaces(masterInterfaces,ModBusInterfaceRole.Master);
            
        return result;

        IEnumerable<string> GetInterfaces(string prefix)
        {
            return token!.SelectTokens($"{prefix}.rtuif[*].if")?.Select(t => t.Value<string>() ?? string.Empty) ?? Array.Empty<string>();
        }

        void SetInterfaces(IEnumerable<string> interfaces,ModBusInterfaceRole role)
        {
            interfaces.Apply(i =>
            {
                var pi = typeof(Gen24ModbusSettings).GetProperties().Single(p => string.Compare(p.Name, i, StringComparison.InvariantCultureIgnoreCase) == 0);
                pi.SetValue(result, role);
            });
        }
    }

    public object Clone() => MemberwiseClone();
}
