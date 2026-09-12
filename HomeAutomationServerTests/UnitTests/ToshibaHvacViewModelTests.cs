using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.ViewModels;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The interaction logic of the dashboard's Toshiba control, without a view: which choices a model offers, what a
/// click sends, and what becomes of a command the air conditioner did not echo.
/// </summary>
public sealed class ToshibaHvacViewModelTests
{
    private const string Key = "Toshiba;HVAC;11111111222233334444555555555555";

    // Internal rather than private: the MVVM toolkit generates a validation partial for every ObservableValidator
    // subclass, and that code lives outside this class.
    internal sealed class FakeCommander : IToshibaHvacCommander
    {
        public List<(string[] Ids, string State)> Sent { get; } = [];
        public List<string> Silent { get; } = [];
        public bool IsSendingWhileCalled { get; private set; }
        public ToshibaHvacViewModel? Owner { get; set; }

        public Task<ToshibaHvacCommandResult> SendToshibaHvacCommand(string[] ids, ToshibaHvacStateData state)
        {
            Sent.Add((ids, state.ToString()));
            IsSendingWhileCalled = Owner?.IsSending ?? false;
            return Task.FromResult(new ToshibaHvacCommandResult { MessageId = "MB_TEST-00000001", Unconfirmed = [.. ids.Where(Silent.Contains)] });
        }
    }

    internal sealed class TestableViewModel(FakeCommander commander) : ToshibaHvacViewModel(commander)
    {
        public List<ToshibaHvacCommandResult> Unconfirmed { get; } = [];

        protected override Task ShowUnconfirmed(ToshibaHvacCommandResult result)
        {
            Unconfirmed.Add(result);
            return Task.CompletedTask;
        }
    }

    private readonly FakeCommander commander = new();
    private readonly TestableViewModel viewModel;

    /// <summary>Model 3 with horizontal swing, the silent modes and the fixed louver positions.</summary>
    private readonly ToshibaHvacMappingDevice device = new()
    {
        Name = "Living room",
        DeviceUniqueId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        AcModelId = 3,
        MeritFeature = 0x6002,
        State = new ToshibaHvacStateData { IsTurnedOn = true, Mode = ToshibaHvacOperatingMode.Cooling, FanSpeed = ToshibaHvacFanSpeed.Manual3, TargetTemperatureCelsius = 22, PowerLimit = 75, WifiLedStatus = ToshibaHvacWifiLedStatus.On },
    };

    public ToshibaHvacViewModelTests()
    {
        viewModel = new TestableViewModel(commander) { Device = device, DeviceKey = Key };
        commander.Owner = viewModel;
    }

    [Fact]
    public void The_menus_offer_what_the_model_has()
    {
        Assert.Equal([ToshibaHvacMeritFeaturesA.None, ToshibaHvacMeritFeaturesA.Eco, ToshibaHvacMeritFeaturesA.HighPower, ToshibaHvacMeritFeaturesA.Silent2, ToshibaHvacMeritFeaturesA.Silent1], viewModel.MeritFeatures.Select(o => o.Value));
        Assert.Equal([ToshibaHvacSwingMode.Off, ToshibaHvacSwingMode.Vertical, ToshibaHvacSwingMode.Horizontal, ToshibaHvacSwingMode.Both, ToshibaHvacSwingMode.Fixed1, ToshibaHvacSwingMode.Fixed2, ToshibaHvacSwingMode.Fixed3, ToshibaHvacSwingMode.Fixed4, ToshibaHvacSwingMode.Fixed5], viewModel.SwingModes.Select(o => o.Value));
        Assert.Equal(7, viewModel.FanSpeeds.Count);
        Assert.Equal<byte>([50, 75, 100], viewModel.PowerLimits.Select(o => o.Value));
        Assert.Equal(30, viewModel.Temperatures.First().Value);
        Assert.Equal(17, viewModel.Temperatures.Last().Value);

        // A plain model 2 without the feature bits has neither silent modes nor horizontal swing nor fixed positions.
        var plain = new ToshibaHvacMappingDevice { AcModelId = 2, MeritFeature = 0 };
        Assert.Equal([ToshibaHvacMeritFeaturesA.None, ToshibaHvacMeritFeaturesA.Eco, ToshibaHvacMeritFeaturesA.HighPower], ToshibaHvacViewModel.AvailableMeritFeatures(plain));
        Assert.Equal([ToshibaHvacSwingMode.Off, ToshibaHvacSwingMode.Vertical], ToshibaHvacViewModel.AvailableSwingModes(plain));
    }

    [Fact]
    public void The_check_marks_follow_the_device_state()
    {
        Assert.Equal(ToshibaHvacFanSpeed.Manual3, Assert.Single(viewModel.FanSpeeds, o => o.IsSelected).Value);
        Assert.Equal<byte>(75, Assert.Single(viewModel.PowerLimits, o => o.IsSelected).Value);
        Assert.Equal<sbyte>(22, Assert.Single(viewModel.Temperatures, o => o.IsSelected).Value);

        // What the server later pushes arrives as a change of the same state object.
        device.State.FanSpeed = ToshibaHvacFanSpeed.Auto;

        Assert.Equal(ToshibaHvacFanSpeed.Auto, Assert.Single(viewModel.FanSpeeds, o => o.IsSelected).Value);
    }

    [Fact]
    public async Task A_click_on_the_fan_sends_the_next_speed_as_a_delta_to_the_device_by_its_key()
    {
        await viewModel.CycleFanSpeedCommand.ExecuteAsync(null);

        var (ids, state) = Assert.Single(commander.Sent);
        Assert.Equal([Key], ids);

        // Byte 3 is the fan speed (Manual4 = 0x35); every other byte is 0xff, "leave it alone".
        Assert.Equal("ffffff35" + new string('f', 15 * 2), state);
        Assert.True(commander.IsSendingWhileCalled);
        Assert.False(viewModel.IsSending);
        Assert.Empty(viewModel.Unconfirmed);
    }

    [Fact]
    public async Task The_power_switch_sends_the_opposite_of_the_current_state()
    {
        await viewModel.TogglePowerCommand.ExecuteAsync(null);

        Assert.StartsWith("31ff", Assert.Single(commander.Sent).State);
    }

    [Fact]
    public async Task The_temperature_is_stepped_and_clamped_and_the_same_value_is_not_sent()
    {
        await viewModel.TemperatureUpCommand.ExecuteAsync(null);
        Assert.Equal("ffff17ff", Assert.Single(commander.Sent).State[..8]);

        commander.Sent.Clear();
        await viewModel.SetTemperatureCommand.ExecuteAsync((sbyte)40);
        Assert.Equal("ffff1eff", Assert.Single(commander.Sent).State[..8]);

        commander.Sent.Clear();
        device.State.TargetTemperatureCelsius = ToshibaHvacViewModel.MinimumTemperature;
        await viewModel.TemperatureDownCommand.ExecuteAsync(null);
        Assert.Empty(commander.Sent);
    }

    [Fact]
    public void The_8_degree_heating_mode_shows_the_temperature_the_device_means()
    {
        device.State.MeritFeaturesA = ToshibaHvacMeritFeaturesA.Heating8C;
        device.State.TargetTemperatureCelsius = 24;

        Assert.Equal<sbyte?>(8, viewModel.DisplayTargetTemperature);
        Assert.Equal("08°C", viewModel.TargetTemperatureText);
    }

    [Fact]
    public async Task A_command_the_device_did_not_echo_is_reported_once_and_the_buttons_come_back()
    {
        commander.Silent.Add(Key);

        await viewModel.ToggleWifiLedCommand.ExecuteAsync(null);

        var result = Assert.Single(viewModel.Unconfirmed);
        Assert.Equal([Key], result.Unconfirmed);
        Assert.False(viewModel.IsSending);
        Assert.True(viewModel.CanSend);
    }

    [Fact]
    public void Nothing_can_be_sent_without_a_device_or_a_key()
    {
        Assert.True(viewModel.CanSend);

        viewModel.DeviceKey = null;

        Assert.False(viewModel.CanSend);
        Assert.False(viewModel.TogglePowerCommand.CanExecute(null));
        Assert.False(viewModel.SetModeCommand.CanExecute(ToshibaHvacOperatingMode.Heating));
    }
}
