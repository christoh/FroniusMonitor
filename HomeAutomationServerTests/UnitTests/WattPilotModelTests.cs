using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The WattPilot model apart from its JSON: what a clone shares with its original and what it does not.
/// </summary>
public class WattPilotModelTests
{
    [Fact]
    public void A_clone_has_load_balancing_currents_of_its_own()
    {
        // The settings dialog edits a clone and sends whatever differs from the original. The currents compare by
        // value, and a setter that drops an Equals value would keep the original's instance in the clone - the
        // dialog would then edit both and nothing would ever differ.
        var original = new WattPilot
        {
            LoadBalancingCurrents = new WattPilotLoadBalancingCurrents { LocalMaximumCurrent = 16, DynamicMaximumCurrent = 10, MaximumCurrentDnoLine = 32, TimeStampEpoch = 1_700_000_000 },
        };

        var clone = (WattPilot)original.Clone();
        clone.LoadBalancingCurrents!.DynamicMaximumCurrent = 12;

        Assert.NotSame(original.LoadBalancingCurrents, clone.LoadBalancingCurrents);
        Assert.Equal(10, original.LoadBalancingCurrents!.DynamicMaximumCurrent);
        Assert.Equal(12, clone.LoadBalancingCurrents.DynamicMaximumCurrent);
        Assert.NotEqual(original.LoadBalancingCurrents, clone.LoadBalancingCurrents);
    }

    [Fact]
    public void Equal_load_balancing_currents_still_replace_each_other()
    {
        // A response from the charger reads a fresh object into the property. Reference comparison must not turn
        // that into "no change" either way - the new instance is taken, and bindings are told.
        var wattPilot = new WattPilot
        {
            LoadBalancingCurrents = new WattPilotLoadBalancingCurrents { LocalMaximumCurrent = 16, DynamicMaximumCurrent = 10, MaximumCurrentDnoLine = 32, TimeStampEpoch = 1_700_000_000 },
        };

        var replacement = wattPilot.LoadBalancingCurrents!.Copy();
        var notified = false;
        wattPilot.PropertyChanged += (_, e) => notified |= e.PropertyName == nameof(WattPilot.LoadBalancingCurrents);

        wattPilot.LoadBalancingCurrents = replacement;

        Assert.Same(replacement, wattPilot.LoadBalancingCurrents);
        Assert.True(notified);
    }
}
