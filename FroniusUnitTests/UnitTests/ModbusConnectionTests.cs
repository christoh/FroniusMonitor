using De.Hochstaetter.Fronius.Models.Settings;

namespace FroniusUnitTests.UnitTests;

[TestFixture]
public class ModbusConnectionTests
{
    [Test]
    public void Test_ModbusConnection_Parser_Success()
    {
        var text = "192.168.100.2:502/145";
        var result = ModbusConnection.Parse(text);
        Assert.AreEqual("192.168.100.2", result.HostName);
        Assert.AreEqual((ushort)502, result.Port);
        Assert.AreEqual((byte)145, result.ModbusAddress);
        Assert.AreEqual(text, result.KeyString);

        text = "www.example.com:1502/00003";
        result = ModbusConnection.Parse(text);
        Assert.AreEqual("www.example.com", result.HostName);
        Assert.AreEqual((ushort)1502, result.Port);
        Assert.AreEqual((byte)3, result.ModbusAddress);
        Assert.AreEqual("www.example.com:1502/3", result.KeyString);

        text = "123a::4d6:1502/3";
        result = ModbusConnection.Parse(text);
        Assert.AreEqual("123a::4d6:1502", result.HostName);
        Assert.AreEqual((ushort)502, result.Port);
        Assert.AreEqual((byte)3, result.ModbusAddress);
        Assert.AreEqual("[123a::4d6:1502]:502/3", result.KeyString);

        text = "[123a::4d6]:1502/0";
        result = ModbusConnection.Parse(text);
        Assert.AreEqual("123a::4d6", result.HostName);
        Assert.AreEqual((ushort)1502, result.Port);
        Assert.AreEqual((byte)0, result.ModbusAddress);
        Assert.AreEqual("[123a::4d6]:1502/0", result.KeyString);
    }
}
