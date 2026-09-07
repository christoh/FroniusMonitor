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
    /// <summary>
    /// The limits of the two bus addresses. Named because two things declare them: the rule on the property here,
    /// which is what the server validates a request body against, and the rule on the string a dialog binds its
    /// text box to. Both have to say the same numbers.
    /// </summary>
    public const int MinMeterAddress = 1, MaxMeterAddress = 247, MinSunSpecAddress = 1, MaxSunSpecAddress = 255;

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

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(MinMeterAddress, MaxMeterAddress, MessageResourceKey = nameof(Resources.MeterAddressError))]
    [FroniusProprietaryImport("meterAddress", FroniusDataType.Root)]
    public partial byte? MeterAddress { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("mode", FroniusDataType.Root)]
    public partial ModbusSlaveMode? Mode { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("parity", FroniusDataType.Root)]
    public partial ModbusParity? Parity { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("port", FroniusDataType.Root)]
    public partial ushort? TcpPort { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(MinSunSpecAddress, MaxSunSpecAddress, MessageResourceKey = nameof(Resources.SunspecAddressError))]
    [FroniusProprietaryImport("scAddress", FroniusDataType.Root)]
    public partial byte? SunSpecAddress { get; set; }

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

    /// <summary>
    /// The addresses that may control the inverter over Modbus TCP: one or more IPv4 addresses, each with an
    /// optional prefix length, separated by commas.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Ipv4(AllowMask = true, AllowList = true)]
    public partial string? IpAddress { get; set; }

    public static Gen24ModbusSettings Parse(JsonNode? token)
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

        var ctrToken = token["slave"]?["ctr"];
        var restrictToken = ctrToken?["restriction"];

        result.AllowControl = ctrToken?["on"].AsBoolean();
        result.RestrictControl = restrictToken?["on"].AsBoolean();
        result.IpAddress = restrictToken?["ip"].AsString();

        return result;

        // Was the JSONPath "<prefix>.rtuif[*].if". System.Text.Json has no path queries, and the walk says the
        // same thing in the same number of lines.
        IEnumerable<string> GetInterfaces(string prefix) =>
            token[prefix]?["rtuif"] is not JsonArray interfaces
                ? []
                : interfaces.Select(entry => entry?["if"].AsString() ?? string.Empty);

        void SetInterfaces(IEnumerable<string> interfaces, ModbusInterfaceRole role)
        {
            interfaces.Apply(i =>
            {
                var pi = typeof(Gen24ModbusSettings).GetProperties().Single(p => string.Compare(p.Name, i, StringComparison.InvariantCultureIgnoreCase) == 0);
                pi.SetValue(result, role);
            });
        }
    }

    public JsonNode GetToken(Gen24ModbusSettings? oldModbusSettings = null)
    {
        var gen24Service = IoC.Get<IGen24JsonService>();
        var slaveToken = gen24Service.GetUpdateToken(this, oldModbusSettings);
        var token = new JsonObject();

        if (oldModbusSettings == null || oldModbusSettings.Rtu0 != Rtu0 || oldModbusSettings.Rtu1 != Rtu1)
        {
            var ifToken = new JsonArray();
            slaveToken.Add("rtuif", ifToken);

            Add(Rtu0, ModbusInterfaceRole.Slave, "rtu0");
            Add(Rtu1, ModbusInterfaceRole.Slave, "rtu1");

            ifToken = new JsonArray();
            var masterToken = new JsonObject { ["rtuif"] = ifToken };
            token.Add("master", masterToken);

            Add(Rtu0, ModbusInterfaceRole.Master, "rtu0");
            Add(Rtu1, ModbusInterfaceRole.Master, "rtu1");

            void Add(ModbusInterfaceRole? value, ModbusInterfaceRole arrayType, string jsonName)
            {
                if (value == arrayType)
                {
                    ifToken.Add(new JsonObject { ["if"] = jsonName });
                }
            }
        }

        if (oldModbusSettings == null || oldModbusSettings.AllowControl != AllowControl || oldModbusSettings.RestrictControl != RestrictControl || oldModbusSettings.IpAddress != IpAddress)
        {
            var ctrToken = new JsonObject();

            if ((oldModbusSettings == null || oldModbusSettings.AllowControl != AllowControl) && AllowControl.HasValue)
            {
                ctrToken.Add("on", AllowControl.Value);
            }

            if (oldModbusSettings == null || oldModbusSettings.RestrictControl != RestrictControl || oldModbusSettings.IpAddress != IpAddress)
            {
                var restrictionToken = new JsonObject();
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

        if (oldModbusSettings == null || slaveToken.Count > 0)
        {
            token.Add("slave", slaveToken);
        }

        return token;

    }

    /// <summary>
    /// A copy that shares nothing with the original.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>MemberwiseClone</c>. <see cref="BindableBase"/> is an <c>ObservableValidator</c>, whose
    /// error store is a dictionary held in a field: a shallow copy would hand the copy the *same* dictionary while
    /// giving it an error count of its own, so the two would report validation states that contradict each other
    /// and Undo would restore an object that still carries the errors of the one it replaces. It would also copy
    /// the <see cref="INotifyPropertyChanged.PropertyChanged"/> subscribers of the original.
    /// Assigning through the properties gives the copy its own state and validates it on the way in.
    /// </remarks>
    public override object Clone() => new Gen24ModbusSettings
    {
        Rtu0 = Rtu0,
        Rtu1 = Rtu1,
        BaudRate = BaudRate,
        IsDemoMode = IsDemoMode,
        MeterAddress = MeterAddress,
        Mode = Mode,
        Parity = Parity,
        TcpPort = TcpPort,
        SunSpecAddress = SunSpecAddress,
        InverterAddress = InverterAddress,
        SunspecMode = SunspecMode,
        AllowControl = AllowControl,
        RestrictControl = RestrictControl,
        IpAddress = IpAddress,
    };
}
