namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum ModbusInterfaceRole
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

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum ModbusParity
{
    [EnumParse(ParseAs = "n")] None,
    [EnumParse(ParseAs = "e")] Even,
    [EnumParse(ParseAs = "o")] Odd,
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum SunspecMode
{
    [EnumParse(ParseAs = "float")] Float,
    [EnumParse(ParseAs = "int")] Int,
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24ModbusSettings : BindableBase, ICloneable
{
    private static readonly IReadOnlyList<int> baudRates = new[] { 9600, 19200 };

    private static readonly IReadOnlyList<ushort> tcpPorts = new[] { (ushort)502, (ushort)1502 };

    public IReadOnlyList<int> BaudRates => baudRates;

    public IReadOnlyList<ushort> TcpPorts => tcpPorts;

    private ModbusInterfaceRole rtu0 = ModbusInterfaceRole.Disabled;

    public ModbusInterfaceRole Rtu0
    {
        get => rtu0;
        set => Set(ref rtu0, value);
    }

    private ModbusInterfaceRole rtu1 = ModbusInterfaceRole.Disabled;

    public ModbusInterfaceRole Rtu1
    {
        get => rtu1;
        set => Set(ref rtu1, value);
    }

    private int? baudRate;
    [FroniusProprietaryImport("baud", FroniusDataType.Root)]
    public int? BaudRate
    {
        get => baudRate;
        set => Set(ref baudRate, value);
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

    [FroniusProprietaryImport("port", FroniusDataType.Root)]
    public ushort? TcpPort
    {
        get => tcpPort;
        set => Set(ref tcpPort, value);
    }

    private byte? sunSpecAddress;

    [FroniusProprietaryImport("scAddress", FroniusDataType.Root)]
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

    private bool? allowControl;

    public bool? AllowControl
    {
        get => allowControl;
        set => Set(ref allowControl, value);
    }

    private bool? restrictControl;

    public bool? RestrictControl
    {
        get => restrictControl;
        set => Set(ref restrictControl, value);
    }

    private string? ipAddress;

    public string? IpAddress
    {
        get => ipAddress;
        set => Set(ref ipAddress, value, () =>
        {
            if (value != null && !Regex.IsMatch(value, @"^$|^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/([0-9]|[1-2][0-9]|3[0-2]))?((,(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/([0-9]|[1-2][0-9]|3[0-2]))?)?)+$"))
            {
                throw new ArgumentException(Resources.MustBeIpv4Address);
            }
        });
    }

    public static Gen24ModbusSettings Parse(string json)
    {
        var token = JToken.Parse(json);

        if (token == null)
        {
            throw new NullReferenceException("No Modbus config present");
        }

        var gen24JsonService = IoC.Get<IGen24JsonService>();
        var result = gen24JsonService.ReadFroniusData<Gen24ModbusSettings>(token["slave"]);
        var masterInterfaces = GetInterfaces("master");
        var slaveInterfaces = GetInterfaces("slave");

        SetInterfaces(slaveInterfaces, ModbusInterfaceRole.Slave);
        SetInterfaces(masterInterfaces, ModbusInterfaceRole.Master);

        var ctrToken = token.SelectToken("slave.ctr");
        var restrictToken = ctrToken?["restriction"];

        result.AllowControl = ctrToken?["on"]?.Value<bool>();
        result.RestrictControl = restrictToken?["on"]?.Value<bool>();
        result.IpAddress = restrictToken?["ip"]?.Value<string>();

        return result;

        IEnumerable<string> GetInterfaces(string prefix)
        {
            return token.SelectTokens($"{prefix}.rtuif[*].if").Select(t => t.Value<string>() ?? string.Empty);
        }

        void SetInterfaces(IEnumerable<string> interfaces, ModbusInterfaceRole role)
        {
            interfaces.Apply(i =>
            {
                var pi = typeof(Gen24ModbusSettings).GetProperties().Single(p => string.Compare(p.Name, i, StringComparison.InvariantCultureIgnoreCase) == 0);
                pi.SetValue(result, role);
            });
        }
    }

    public JToken GetToken(Gen24ModbusSettings? oldModbusSettings = null)
    {
        var gen24Service = IoC.Get<IGen24JsonService>();
        var slaveToken = gen24Service.GetUpdateToken(this, oldModbusSettings);
        var token = new JObject();

        if (oldModbusSettings == null || oldModbusSettings.Rtu0 != Rtu0 || oldModbusSettings.Rtu1 != Rtu1)
        {
            var ifToken = new JArray();
            slaveToken.Add("rtuif", ifToken);

            Add(Rtu0, ModbusInterfaceRole.Slave, "rtu0");
            Add(Rtu1, ModbusInterfaceRole.Slave, "rtu1");

            ifToken = new JArray();
            var masterToken = new JObject { { "rtuif", ifToken } };
            token.Add("master", masterToken);

            Add(Rtu0, ModbusInterfaceRole.Master, "rtu0");
            Add(Rtu1, ModbusInterfaceRole.Master, "rtu1");

            void Add(ModbusInterfaceRole? value, ModbusInterfaceRole arrayType, string jsonName)
            {
                if (value == arrayType)
                {
                    ifToken.Add(new JObject { { "if", jsonName } });
                }
            }
        }

        if (oldModbusSettings == null || oldModbusSettings.AllowControl != AllowControl || oldModbusSettings.RestrictControl != RestrictControl || oldModbusSettings.IpAddress != IpAddress)
        {
            var ctrToken = new JObject();

            if ((oldModbusSettings == null || oldModbusSettings.AllowControl != AllowControl) && AllowControl.HasValue)
            {
                ctrToken.Add("on", AllowControl.Value);
            }

            if (oldModbusSettings == null || oldModbusSettings.RestrictControl != RestrictControl || oldModbusSettings.IpAddress != IpAddress)
            {
                var restrictionToken = new JObject();
                ctrToken.Add("restriction", restrictionToken);

                if (RestrictControl.HasValue && (oldModbusSettings == null || oldModbusSettings.RestrictControl != RestrictControl))
                {
                    restrictionToken.Add("on", RestrictControl);
                }

                if (IpAddress != null && (oldModbusSettings == null || oldModbusSettings.IpAddress != IpAddress))
                {
                    restrictionToken.Add("ip", IpAddress);
                }
            }

            slaveToken.Add("ctr", ctrToken);
        }

        if (oldModbusSettings == null || slaveToken.Children().Any())
        {
            token.Add("slave", slaveToken);
        }

        return token;

    }

    public object Clone() => MemberwiseClone();
}
