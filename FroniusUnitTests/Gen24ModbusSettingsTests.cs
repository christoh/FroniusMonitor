namespace FroniusUnitTests;

[TestFixture]
public class Gen24ModbusSettingsTests
{
    [Test]
    public void IpAddress_SetValidValue_ShouldNotThrowException()
    {
        var settings = new Gen24ModbusSettings();

        // Allow null and empty address
        Assert.DoesNotThrow(() => settings.IpAddress = "");
        Assert.DoesNotThrow(() => settings.IpAddress = null);

        // Individual IPv4 Address
        Assert.DoesNotThrow(() => settings.IpAddress = "192.168.1.1");
        Assert.DoesNotThrow(() => settings.IpAddress = "10.0.0.100");
        Assert.DoesNotThrow(() => settings.IpAddress = "172.16.0.10");

        // IPv4 Address with Subnet Mask (CIDR Notation)
        Assert.DoesNotThrow(() => settings.IpAddress = "192.168.1.0/24");
        Assert.DoesNotThrow(() => settings.IpAddress = "10.0.0.0/8");
        Assert.DoesNotThrow(() => settings.IpAddress = "172.16.0.0/16");

        // Multiple IPv4 Addresses and Subnet Masks separated by Commas
        Assert.DoesNotThrow(() => settings.IpAddress = "192.168.1.1,10.0.0.100,172.16.0.10");
        Assert.DoesNotThrow(() => settings.IpAddress = "192.168.1.0/24,10.0.0.0/8,172.16.0.0/16");
        Assert.DoesNotThrow(() => settings.IpAddress = "192.168.1.1,10.0.0.100,172.16.0.10,"
           + "192.168.1.0/24,10.0.0.0/8,172.16.0.0/16");
    }

    [Test]
    public void IpAddress_SetInvalidValue_ShouldThrowArgumentException()
    {
        var settings = new Gen24ModbusSettings();

        Assert.Throws<ArgumentException>(() => settings.IpAddress = "InvalidIpAddress");
        Assert.Throws<ArgumentException>(() => settings.IpAddress = "InvalidIpAddress/24");
    }
}
