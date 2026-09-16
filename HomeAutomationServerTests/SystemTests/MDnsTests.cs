using Makaretu.Dns;

namespace De.Hochstaetter.HomeAutomationServerTests.SystemTests;

/// <summary>
/// Asks the local network what Fronius and FritzBox devices announce themselves over multicast DNS, and writes
/// what answers into the test output. It reports rather than asserts: which devices are on the LAN is not
/// something a test can know in advance.
/// </summary>
public sealed class MDnsTests(ITestOutputHelper output)
{
    private static readonly DomainName[] Services =
    [
        new("_fronius-se-inverter", "_tcp", "local"),
        new("_fronius-se-smartmeter", "_tcp", "local"),
        new("_fronius-se-wattpilot", "_tcp", "local"),
        new("_fronius-se-ohmpilot", "_tcp", "local"),
        new("_fbox", "_tcp", "local"),
    ];

    [SystemFact]
    public async Task The_devices_on_the_network_announce_themselves()
    {
        using var mdns = new MulticastService();
        mdns.AnswerReceived += OnAnswerReceived;
        mdns.NetworkInterfaceDiscovered += OnInterfacesDiscovered;
        mdns.Start();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
        finally
        {
            // Both handlers write into the test output, which is closed the moment the test returns, so they have
            // to be gone before that happens.
            mdns.AnswerReceived -= OnAnswerReceived;
            mdns.NetworkInterfaceDiscovered -= OnInterfacesDiscovered;
            mdns.Stop();
        }

        return;

        void OnInterfacesDiscovered(object? sender, NetworkInterfaceEventArgs e)
        {
            foreach (var service in Services)
            {
                mdns.SendQuery(service, type: DnsType.PTR);
            }
        }

        void OnAnswerReceived(object? sender, MessageEventArgs e)
        {
            foreach (var record in e.Message.Answers.OfType<SRVRecord>())
            {
                output.WriteLine($"{record.Name} => {record.Target}");
                mdns.SendQuery(record.Target, type: DnsType.ANY);
            }

            foreach (var record in e.Message.Answers.OfType<AddressRecord>())
            {
                output.WriteLine($"{record.CanonicalName} => {record.Address}");
            }
        }
    }
}
