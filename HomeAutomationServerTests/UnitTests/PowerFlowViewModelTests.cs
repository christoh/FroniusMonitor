using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the power flow page follows beyond the devices. The arithmetic is <c>PowerFlowSnapshotTests</c> and the
/// page itself is <c>PowerFlowViewTests</c>; this is about the Solar Web switch, which is not a device and
/// announces itself through nothing the update service says.
/// </summary>
public sealed class PowerFlowViewModelTests
{
    private readonly FakeUpdateService service = new();
    private readonly FakePowerDisplayOptions options = new();
    private readonly PowerFlowViewModel viewModel;

    public PowerFlowViewModelTests()
    {
        viewModel = new PowerFlowViewModel(service, IoC.Get<IGen24LocalizationService>(), options);
        service.Inverters.Add(new KeyedGen24System { Key = "inv", Device = new Gen24System() });
        service.SitePowerFlow.LoadPower = -3000;
        service.SitePowerFlow.SolarPower = 6000;
        service.SitePowerFlow.InverterAcPower = 5700;
    }

    [Fact]
    public async Task Throwing_the_solar_web_switch_builds_a_new_snapshot()
    {
        await viewModel.Initialize();
        Assert.Equal(3000, viewModel.Snapshot.House.Power);

        options.IncludeInverterPower = true;
        Assert.Equal(3300, viewModel.Snapshot.House.Power);

        options.IncludeInverterPower = false;
        Assert.Equal(3000, viewModel.Snapshot.House.Power);
    }

    /// <summary>A page that has been left is a page that follows nothing, the switch included.</summary>
    [Fact]
    public async Task A_stopped_page_does_not_hear_the_switch()
    {
        await viewModel.Initialize();
        viewModel.Stop();

        options.IncludeInverterPower = true;

        Assert.Equal(3000, viewModel.Snapshot.House.Power);
    }
}
