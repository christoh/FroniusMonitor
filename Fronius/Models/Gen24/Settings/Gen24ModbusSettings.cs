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
public partial class Gen24ModbusSettings : Gen24ParsingBase, ICloneable
{
    private static readonly IReadOnlyList<int> baudRates = [9600, 19200];

    private static readonly IReadOnlyList<ushort> tcpPorts = [502, 1502];

    public IReadOnlyList<int> BaudRates => baudRates;

    public IReadOnlyList<ushort> TcpPorts => tcpPorts;

    [ObservableProperty]
    public partial ModbusInterfaceRole Rtu0 { get; set; } = ModbusInterfaceRole.Disabled;

    [ObservableProperty]
    public partial ModbusInterfaceRole Rtu1 { get; set; } = ModbusInterfaceRole.Disabled;

    [ObservableProperty]
    [FroniusProprietaryImport("baud", FroniusDataType.Root)]
    public partial int? BaudRate { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("demo", FroniusDataType.Root)]
    public partial bool? IsDemoMode { get; set; }

    [FroniusProprietaryImport("meterAddress", FroniusDataType.Root)]
    public byte? MeterAddress
    {
        get;

        set => Set(ref field, value, () =>
        {
            if (value is < 1 or > 247)
            {
                throw new ArgumentOutOfRangeException(Resources.MeterAddressError, null as Exception);
            }
        });
    }

    [ObservableProperty]
    [FroniusProprietaryImport("mode", FroniusDataType.Root)]
    public partial ModbusSlaveMode? Mode { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("parity", FroniusDataType.Root)]
    public partial ModbusParity? Parity { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("port", FroniusDataType.Root)]
    public partial ushort? TcpPort { get; set; }

    [FroniusProprietaryImport("scAddress", FroniusDataType.Root)]
    public byte? SunSpecAddress
    {
        get;
        set => Set(ref field, value, () =>
        {
            if (value is 0)
            {
                throw new ArgumentOutOfRangeException(Resources.SunspecAddressError, null as Exception);
            }
        });
    }

    [ObservableProperty]
    [FroniusProprietaryImport("rtu_inverter_slave_id", FroniusDataType.Root)]
    public partial byte? InverterAddress { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("sunspecMode", FroniusDataType.Root)]
    public partial SunspecMode? SunspecMode { get; set; }

    [ObservableProperty]
    public partial bool? AllowControl { get; set; }

    [ObservableProperty]
    public partial bool? RestrictControl { get; set; }

    public string? IpAddress
    {
        get;
        set => Set(ref field, value, () =>
        {
            if (!string.IsNullOrEmpty(value) && !value.Split(',').All(IsValidIpOrIpWithMask))
            {
                throw new ArgumentException(Resources.MustBeIpv4Address);
            }
        });
    }

    private static bool IsValidIpOrIpWithMask(string value)
    {
        var parts = value.Split('/');
        var isValidIp = parts[0].Split('.').Length == 4 // 4 octets
            && parts[0].Split('.').All(octet => byte.TryParse(octet, out _)); // and each is valid

        return parts.Length switch
        {
            // like 192.168.178.1
            1 => isValidIp,
            // with mask, like 192.168.178.1/16 (mask range needs to be within 0 and 32)
            2 when int.TryParse(parts[1], out int mask) && mask is > 0 and <= 32 => isValidIp,
            _ => false
        };
    }

    public static Gen24ModbusSettings Parse(JToken? token)
    {
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

    public override object Clone() => MemberwiseClone();
}
