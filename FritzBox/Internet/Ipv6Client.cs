using De.Hochstaetter.FritzBox.Http;

namespace De.Hochstaetter.FritzBox.Internet;

/// <summary>
/// The IPv6 support of the box's internet connection, the switch of <c>Internet &gt; Zugangsdaten &gt; IPv6</c> in the
/// web interface. It uses the web interface's own request, <c>api/v0/generic/sync_groups/sg/sg1001</c>, captured on
/// FRITZ!OS 8.25: AVM does not publish it, so it may change with an update. TR-064 can only read the IPv6 status, and
/// USP has it read-only.
/// </summary>
/// <remarks>
/// <para>
/// The group holds every IPv6 setting of the connection. The web interface writes all of them back, but a body with
/// <c>ipv6_mode</c> alone does the same and leaves every other field as it was (tested on 2026-10-27). So nothing is
/// written that was not meant to change, and no field is lost that a later FRITZ!OS adds.
/// </para>
/// <para>
/// The box answers before it acts. Every switch makes it reconnect, IPv4 included, for 10 to 20 s, so the answer
/// says nothing about success; reading the mode back once the box is reachable again does.
/// </para>
/// </remarks>
public sealed class Ipv6Client(FritzBoxHttpClient http)
{
    /// <summary>IPv6 switched off.</summary>
    public const string Off = "ipv6_off";

    /// <summary>IPv6 switched on, with the addresses the provider hands out.</summary>
    public const string Automatic = "ipv6_automatic";

    private const string ModeField = "ipv6_mode";
    private const string Path = "generic/sync_groups/sg/sg1001";

    /// <summary>
    /// The mode the box is in: <see cref="Off"/>, <see cref="Automatic"/> or another mode of the web interface
    /// (native IPv6, 6rd, 6to4, a tunnel), which is returned as the box names it.
    /// </summary>
    /// <exception cref="InvalidDataException">The answer has no <c>ipv6_mode</c>.</exception>
    public async Task<string> GetModeAsync(CancellationToken token = default)
    {
        var json = await http.GetRestAsync(Path, token).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);

        return document.RootElement.ValueKind == JsonValueKind.Object &&
               document.RootElement.TryGetProperty(ModeField, out var mode) &&
               mode.ValueKind == JsonValueKind.String &&
               mode.GetString() is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"{Path} answered without {ModeField}: {json}");
    }

    /// <summary>Switches IPv6 to <paramref name="mode"/>. The box reconnects afterwards.</summary>
    public Task SetModeAsync(string mode, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        return http.SendRestAsync(HttpMethod.Put, Path, JsonSerializer.Serialize(new Dictionary<string, string> { [ModeField] = mode }), token);
    }
}
