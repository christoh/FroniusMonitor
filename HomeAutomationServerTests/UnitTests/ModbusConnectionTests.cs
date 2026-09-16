using De.Hochstaetter.HomeAutomationServer.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// A Modbus connection is written as <c>host:port/address</c>, and the host may be an IPv6 address, which brings
/// its own colons. Port and address both have defaults, and the key string is the canonical form the settings are
/// stored and compared by.
/// </summary>
public sealed class ModbusConnectionTests
{
    [Theory]
    // A plain IPv4 address with everything spelled out.
    [InlineData("192.168.100.2:502/145", "192.168.100.2", (ushort)502, (byte)145, "192.168.100.2:502/145")]
    // A host name, and a Modbus address with leading zeroes that the key string drops.
    [InlineData("www.example.com:1502/00003", "www.example.com", (ushort)1502, (byte)3, "www.example.com:1502/3")]
    // An unbracketed IPv6 address: its last group looks like a port and must not be taken for one, so the whole
    // thing is the host and the port falls back to 502. The key string brackets it.
    [InlineData("123a::4d6:1502/3", "123a::4d6:1502", (ushort)502, (byte)3, "[123a::4d6:1502]:502/3")]
    // The same address bracketed, where 1502 really is the port.
    [InlineData("[123a::4d6]:1502/0", "123a::4d6", (ushort)1502, (byte)0, "[123a::4d6]:1502/0")]
    public void A_connection_string_is_parsed_into_host_port_and_address(string text, string hostName, ushort port, byte modbusAddress, string keyString)
    {
        var result = ModbusConnection.Parse(text);

        Assert.Equal(hostName, result.HostName);
        Assert.Equal(port, result.Port);
        Assert.Equal(modbusAddress, result.ModbusAddress);
        Assert.Equal(keyString, result.KeyString);
    }
}
