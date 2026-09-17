using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Misc;
using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The addresses of the app, in both directions. A device is named by its manufacturer and serial number; a page
/// that belongs to no device is a single segment of its own.
/// </summary>
public sealed class ViewPathTests
{
    // Gen24System.Manufacturer is always "Fronius" and the serial number comes from the versions of its config,
    // so the address of an inverter is made by filling that in rather than by setting the two properties.
    private static KeyedGen24System Inverter(string serialNumber) => new()
    {
        Key = serialNumber,
        Device = new Gen24System { Config = new Gen24Config { Versions = new Gen24Versions { SerialNumber = serialNumber } } },
    };

    [Fact]
    public void The_power_flow_page_recognizes_the_address_it_writes()
    {
        Assert.True(ViewPath.IsPowerFlow(ViewPath.PowerFlow));
    }

    [Theory]
    [InlineData("/powerflow")]
    [InlineData("powerflow")]
    [InlineData("/PowerFlow")]
    // A trailing slash is the same address: a browser and a handwritten link disagree about it, we must not.
    [InlineData("/powerflow/")]
    public void A_handwritten_power_flow_address_is_accepted(string path)
    {
        Assert.True(ViewPath.IsPowerFlow(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/powerflowing")]
    [InlineData("/powerflow/Fronius")]
    [InlineData("/inverterdetails/Fronius/12345678")]
    public void Nothing_else_is_the_power_flow_address(string? path)
    {
        Assert.False(ViewPath.IsPowerFlow(path));
    }

    [Fact]
    public void A_device_address_is_not_a_page_and_leads_back_to_its_device()
    {
        var inverter = Inverter("12345678");
        IKeyedDevice[] devices = [inverter];
        var path = ViewPath.For(inverter.Device);

        Assert.Equal("/inverterdetails/Fronius/12345678", path);
        Assert.False(ViewPath.IsPowerFlow(path));
        Assert.Same(inverter, ViewPath.Find(devices, path));
    }

    [Fact]
    public void The_power_flow_address_names_no_device()
    {
        IKeyedDevice[] devices = [Inverter("12345678")];

        Assert.Null(ViewPath.Find(devices, ViewPath.PowerFlow));
    }
}
