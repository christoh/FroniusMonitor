namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// An IPv4 address, and whatever of a mask, a host name and a list of them is switched on. Covers both IP rules of
/// the WPF app: <c>Ipv4OrHostname</c> with <see cref="AllowHostname"/>, and the allow list of the Modbus dialog
/// with <see cref="AllowMask"/> and <see cref="AllowList"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class Ipv4Attribute : ValidationRuleAttribute
{
    /// <summary>Take a host name as well as an address.</summary>
    public bool AllowHostname { get; set; }

    /// <summary>Take a prefix length after a slash, as in <c>192.168.178.0/24</c>.</summary>
    public bool AllowMask { get; set; }

    /// <summary>Take several entries separated by commas, each of which has to hold on its own.</summary>
    public bool AllowList { get; set; }

    protected override string Complaint => AllowHostname ? Resources.NoHostnameOrIpv4Address : Resources.MustBeIpv4Address;

    protected override bool IsAcceptable(object? value)
    {
        var text = value?.ToString() ?? string.Empty;

        return (AllowList ? text.Split(',') : [text]).All(IsValidEntry);
    }

    private bool IsValidEntry(string entry)
    {
        var parts = entry.Split('/');

        return parts.Length switch
        {
            1 => IsAddressOrHostname(parts[0]),
            // A mask only makes sense on an address, so a host name with one is refused even where names are allowed.
            2 when AllowMask => IsAddress(parts[0]) && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var mask) && mask is > 0 and <= 32,
            _ => false,
        };
    }

    private bool IsAddressOrHostname(string entry) => IsAddress(entry) || (AllowHostname && Uri.CheckHostName(entry) is UriHostNameType.Dns);

    private static bool IsAddress(string entry)
    {
        var octets = entry.Split('.');

        return octets.Length == 4 && octets.All(octet => byte.TryParse(octet, NumberStyles.None, CultureInfo.InvariantCulture, out _));
    }
}
