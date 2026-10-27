using System.Net;
using System.Text.Json;
using De.Hochstaetter.FritzBox.Internet;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>
/// A box with the IPv6 group of the Swiss box as it read on 2026-10-27: a GET of <c>sync_groups/sg/sg1001</c>
/// answers the group, a PUT changes the fields it names and keeps the others. <see cref="IsReachable"/> takes the box
/// off the network, the login included, as the reconnect after a switch does.
/// </summary>
internal sealed class FakeIpv6Box : FakeFritzBox
{
    public const string GroupPath = "/api/v0/generic/sync_groups/sg/sg1001";

    private readonly Lock gate = new();

    private readonly Dictionary<string, string> group = new()
    {
        ["UID"] = "sg1001",
        ["name"] = "sync_ata",
        ["enabled"] = "1",
        ["sync_state"] = "showtime",
        ["ipv4_mode"] = "ipv4_normal",
        ["ipv6_mode"] = Ipv6Client.Automatic,
        ["ipv6_aftr"] = "",
        ["ipv6_wan_first_dns"] = "::",
        ["ipv6_static_prefixlen"] = "56",
        ["dhcpv6c_wanted_prefixlen"] = "60",
        ["6rd_prefixlen"] = "0",
    };

    public FakeIpv6Box()
    {
        Before = (_, _) => IsReachable ? Task.CompletedTask : throw new HttpRequestException("No route to host (the fake box is unreachable)");
    }

    /// <summary>What <c>ipv6_mode</c> holds.</summary>
    public string Mode
    {
        get => Group["ipv6_mode"];
        set
        {
            lock (gate)
            {
                group["ipv6_mode"] = value;
            }
        }
    }

    /// <summary>When <see langword="false"/>, every request fails as a box does that is not on the network.</summary>
    public bool IsReachable { get; set; } = true;

    /// <summary>The answer to a GET instead of the group.</summary>
    public string? Response { get; init; }

    /// <summary>A PUT is answered with 400, as the box answers a value it does not take.</summary>
    public bool RefusePut { get; set; }

    /// <summary>Runs after a PUT changed the group, with the mode it set. A test can take the box off the network with it.</summary>
    public Action<string>? AfterPut { get; set; }

    public IReadOnlyDictionary<string, string> Group
    {
        get
        {
            lock (gate)
            {
                return new Dictionary<string, string>(group);
            }
        }
    }

    /// <summary>The modes the PUTs set, in order.</summary>
    public IReadOnlyList<string> ModesPut => [.. Requests.Where(r => r.Method == HttpMethod.Put).Select(r => JsonSerializer.Deserialize<Dictionary<string, string>>(r.Body)!["ipv6_mode"])];

    protected override HttpResponseMessage Answer(FakeFritzBoxCall call, int number)
    {
        if (call.Path != GroupPath)
        {
            return Reply(HttpStatusCode.NotFound, "{}", "application/json");
        }

        if (call.Method == HttpMethod.Get)
        {
            return Reply(HttpStatusCode.OK, Response ?? JsonSerializer.Serialize(Group), "application/json");
        }

        if (call.Method != HttpMethod.Put)
        {
            return Reply(HttpStatusCode.MethodNotAllowed, "{}", "application/json");
        }

        if (RefusePut)
        {
            return Reply(HttpStatusCode.BadRequest, """{"error":"invalid value"}""", "application/json");
        }

        var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(call.Body)!;

        lock (gate)
        {
            foreach (var (field, value) in fields)
            {
                group[field] = value;
            }
        }

        AfterPut?.Invoke(fields["ipv6_mode"]);
        return Reply(HttpStatusCode.OK, "{}", "application/json");
    }
}
