namespace De.Hochstaetter.Fronius.Models.Settings;

[SuppressMessage("ReSharper", "CommentTypo")]
public partial class ModbusConnection : BindableBase, IHaveDisplayName
{
    // TODO: No full syntax checking. Only suitable for splitting. Also ::ffff:1.2.3.4 and fe80::1234%eth0 is not supported.
    [GeneratedRegex(@"^\[?(?<HostName>[\-0-9A-Za-z._]+|[0-9a-fA-F:]+)\]?(:(?<Port>[0-9]+))?\/(?<ModbusAddress>[0-9]+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ParserRegex();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeyString),nameof(DisplayName))]
    [XmlAttribute]
    public partial string HostName { get; set; } = "192.168.178.xxx";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeyString),nameof(DisplayName))]
    [XmlAttribute]
    public partial ushort Port { get; set; } = 502;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeyString),nameof(DisplayName))]
    [XmlAttribute]
    public partial byte ModbusAddress { get; set; }

    [XmlIgnore] public string KeyString => $"{SurroundIpV6Address(HostName)}:{Port}/{ModbusAddress}";

    [XmlIgnore] public string DisplayName => KeyString;

    public override string ToString() => DisplayName;

    public static ModbusConnection Parse(string keyString)
    {
        var split = keyString.Split('/');

        if (split.Length != 2)
        {
            throw new ArgumentException("Must contain exactly one forward slash");
        }

        var match = ParserRegex().Match(keyString);

        if (!match.Success)
        {
            throw new ArgumentException("No valid Modbus address");
        }

        var portMatch = match.Groups["Port"];

        return new ModbusConnection
        {
            HostName = match.Groups["HostName"].Value,
            Port = portMatch.Success ? ushort.Parse(match.Groups["Port"].Value, NumberStyles.None, CultureInfo.InvariantCulture) : (ushort)502,
            ModbusAddress = byte.Parse(match.Groups["ModbusAddress"].Value, NumberStyles.None, CultureInfo.InvariantCulture),
        };
    }

    private static string SurroundIpV6Address(string hostNameOrAddress)
    {
        return IPAddress.TryParse(hostNameOrAddress, out var ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{hostNameOrAddress}]" : hostNameOrAddress;
    }
}