using De.Hochstaetter.Fronius.Contracts;
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
    [Fact]
    public async Task A_wattpilot_is_a_three_phase_consumer_with_the_totals_on_the_single_phase_side()
    {
        var wattPilot = new WattPilot
        {
            PowerTotal = 3700, PowerL1 = 1200, PowerL2 = 1250, PowerL3 = 1250,
            VoltageL1 = 230, VoltageL2 = 232, VoltageL3 = 228,
            CurrentL1 = 5.2, CurrentL2 = 5.4, CurrentL3 = 5.5,
            TotalEnergy = 123_456, Version = "40.7", IsChargingAllowed = true,
        };

        IPowerConsumer3P consumer = wattPilot;

        Assert.True(consumer.CanMeasurePower);
        Assert.Equal(3700, consumer.ActivePower);
        Assert.Equal(230, consumer.Voltage);
        Assert.Equal(16.1, consumer.Current!.Value, 6);
        Assert.Equal(123_456, consumer.EnergyConsumed);
        Assert.Equal("40.7", consumer.DeviceVersion);

        Assert.Equal(1200, consumer.ActivePowerL1);
        Assert.Equal(1250, consumer.ActivePowerL2);
        Assert.Equal(1250, consumer.ActivePowerL3);
        Assert.Equal(228, consumer.PhaseVoltageL3);
        Assert.Equal(5.4, consumer.CurrentL2);

        // The consumer contract brings a switch, a dimmer and two colour controls with it. The charger has none of
        // them, says so, and the two hue properties that fall back on each other come out as null and not as a
        // stack overflow.
        Assert.False(consumer.CanSwitch);
        Assert.True(consumer.IsTurnedOn);
        Assert.False(consumer.CanDim);
        Assert.False(consumer.HasHsvColorControl);
        Assert.False(consumer.HasColorTemperatureControl);
        Assert.Null(consumer.HueDegrees);
        Assert.Null(consumer.HueRadians);
        Assert.Null(consumer.Level);
        await Assert.ThrowsAsync<NotSupportedException>(() => consumer.TurnOnOff(false));
        await Assert.ThrowsAsync<NotSupportedException>(() => consumer.SetLevel(0.5));
    }

    [Fact]
    public void The_consumer_view_of_the_charger_stays_out_of_its_json()
    {
        // The server pushes the charger to its clients as JSON. The consumer contract is implemented explicitly
        // so that none of it turns up there: a client reading "CanMeasurePower" would be reading a property the
        // charger never sent.
        var json = System.Text.Json.JsonSerializer.Serialize(new WattPilot { PowerTotal = 100 });

        Assert.DoesNotContain("CanMeasurePower", json);
        Assert.DoesNotContain("ActivePower", json);
        Assert.DoesNotContain("EnergyConsumed", json);
        Assert.Contains("PowerTotal", json);
    }
}
