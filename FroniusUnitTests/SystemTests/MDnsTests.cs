using Makaretu.Dns;
using NUnit.Framework.Constraints;
using System.Linq;
using System.Net;

namespace FroniusUnitTests.SystemTests;

[TestFixture]
public class MDnsTests
{
    [Test]
    public async Task DiscoverWorks()
    {
        var sd = new ServiceDiscovery();
        var mdns = new MulticastService();
        mdns.AnswerReceived += OnAnswerReceived;

        //sd.ServiceDiscovered += (s, serviceName) =>
        //{
        //    mdns.SendQuery(serviceName, type: DnsType.PTR);
        //};

        //sd.ServiceInstanceDiscovered += (s, e) =>
        //{
        //    mdns.SendQuery(e.ServiceInstanceName, type: DnsType.SRV);
        //};

        mdns.NetworkInterfaceDiscovered += OnInterfacesDiscovered;
        mdns.Start();

        await Task.Delay(TimeSpan.FromSeconds(10));

        void OnInterfacesDiscovered(object? sender, NetworkInterfaceEventArgs e)
        {
            mdns.SendQuery(new DomainName("_fronius-se-inverter", "_tcp", "local"), type: DnsType.PTR);
            mdns.SendQuery(new DomainName("_fronius-se-smartmeter", "_tcp", "local"), type: DnsType.PTR);
            mdns.SendQuery(new DomainName("_fronius-se-wattpilot", "_tcp", "local"), type: DnsType.PTR);
            mdns.SendQuery(new DomainName("_fronius-se-ohmpilot", "_tcp", "local"), type: DnsType.PTR);
            mdns.SendQuery(new DomainName("_fbox", "_tcp", "local"), type: DnsType.PTR);
            //sd.QueryAllServices();
        }


        void OnAnswerReceived(object? sender, MessageEventArgs e)
        {
            foreach (var record in e.Message.Answers.OfType<SRVRecord>())
            {
                TestContext.Out.WriteLine($"{record.Name} => {record.Target}");
                TestContext.Out.Flush();
                mdns.SendQuery(record.Target, type: DnsType.ANY);
            }

            foreach (var record in e.Message.Answers.OfType<AddressRecord>())
            {
                TestContext.Out.WriteLine($"{record.CanonicalName}=> {record.Address}");
                TestContext.Out.Flush();
            }
        }
    }
}
