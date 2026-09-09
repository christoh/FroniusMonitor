using System.Text.Json;
using System.Text.Json.Nodes;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The settings behind the inverter settings tab: what the three write endpoints of the server send to the
/// inverter, and whether these objects survive the trip over the API at all.
/// </summary>
/// <remarks>
/// The deltas matter more than they look. Each of the three endpoints writes a different path of the inverter and
/// the delta decides both what changes and whether anything is sent: an empty object means the inverter already
/// holds what was asked for, and a tree of empty objects would ask it to change nothing.
/// </remarks>
public class Gen24InverterSettingsTests
{
    private static Gen24InverterSettings Loaded() => new()
    {
        SystemName = "Hochstaetter",
        TimeZoneName = "Europe/Zurich",
        TimeSync = true,
        Mppt = new Gen24Mppt
        {
            StringCombination = StringCombination.NotCombined,
            Mppt1 = new Gen24Mppt1 { PowerMode = MpptPowerMode.Auto, DynamicPeakManager = MpptOnOff.On, WattPeak = 5000, DcFixedVoltage = 600 },
            Mppt2 = new Gen24Mppt2 { PowerMode = MpptPowerMode.Fix, DynamicPeakManager = MpptOnOff.Off, WattPeak = 3000, DcFixedVoltage = 400 },
        },
        PowerLimitSettings = new Gen24PowerLimitSettings
        {
            Visualization = new Gen24PowerLimitsVisualization { WattPeakReferenceValue = 8000 },
            ExportLimits = new Gen24PowerLimits
            {
                EnableFailSafeMode = false,
                ActivePower = new Gen24PowerLimit
                {
                    IsEnabled = true,
                    PhaseMode = PhaseMode.PhaseSum,
                    NetWorkMode = NetworkMode.Local,
                    HardLimit = new Gen24PowerLimitDefinition { IsEnabled = true, PowerLimit = 7000 },
                    SoftLimit = new Gen24PowerLimitDefinition { IsEnabled = false, PowerLimit = 0 },
                },
            },
        },
    };

    #region The common settings

    [Fact]
    public void CommonDeltaCoversTheThreeFieldsOfItsEndpointAndNothingElse()
    {
        var wanted = (Gen24InverterSettings)Loaded().Clone();
        wanted.SystemName = "Something else";
        wanted.TimeZoneName = "Europe/Berlin";
        wanted.TimeSync = false;

        // What the inverter holds still has the trackers and the limits in it, and those must not turn up here:
        // they belong to api/config/powerunit and api/config/limit_settings, not to api/config/common.
        wanted.Mppt!.Mppt1!.WattPeak = 9999;
        wanted.PowerLimitSettings.Visualization.WattPeakReferenceValue = 12345;

        var delta = IoC.Get<IGen24JsonService>().GetUpdateToken(wanted, Loaded());

        Assert.Equal("Something else", delta["systemName"]?.GetValue<string>());
        Assert.Equal("Europe/Berlin", delta["timezone"]?.GetValue<string>());
        Assert.False(delta["timesync"]?.GetValue<bool>());
        Assert.Equal(3, delta.Count);
    }

    [Fact]
    public void CommonDeltaIsEmptyWhereNothingChanged()
    {
        var delta = IoC.Get<IGen24JsonService>().GetUpdateToken((Gen24InverterSettings)Loaded().Clone(), Loaded());
        Assert.False(delta.HasAnyValue());
    }

    #endregion

    #region The string trackers

    [Fact]
    public void MpptDeltaNamesOnlyTheTrackerThatChanged()
    {
        var wanted = (Gen24Mppt)Loaded().Mppt!.Clone();
        wanted.Mppt2!.WattPeak = 4200;

        var delta = wanted.GetToken(Loaded().Mppt);
        var trackers = delta["mppt"];

        Assert.NotNull(trackers);
        Assert.Null(trackers!["mppt1"]);
        Assert.Equal(4200u, trackers["mppt2"]?["PV_POWERACTIVE_CONNECTED_PEAK_MAX_02_U32"]?.GetValue<uint>());
        Assert.Single(trackers.AsObject());
    }

    [Fact]
    public void MpptDeltaUsesTheChannelNamesOfEachTracker()
    {
        var wanted = (Gen24Mppt)Loaded().Mppt!.Clone();
        wanted.Mppt1!.PowerMode = MpptPowerMode.Fix;
        wanted.Mppt2!.PowerMode = MpptPowerMode.Off;

        var trackers = wanted.GetToken(Loaded().Mppt)["mppt"]!;

        Assert.NotNull(trackers["mppt1"]?["PV_MODE_MPP_01_U16"]);
        Assert.NotNull(trackers["mppt2"]?["PV_MODE_MPP_02_U16"]);
    }

    [Fact]
    public void MpptDeltaIsEmptyWhereNothingChanged()
    {
        var delta = ((Gen24Mppt)Loaded().Mppt!.Clone()).GetToken(Loaded().Mppt);
        Assert.False(delta.HasAnyValue());
        Assert.Empty(delta.AsObject());
    }

    [Fact]
    public void MpptDeltaLeavesOutATrackerTheInverterDoesNotHave()
    {
        var current = (Gen24Mppt)Loaded().Mppt!.Clone();
        current.Mppt2 = null;

        var wanted = (Gen24Mppt)Loaded().Mppt!.Clone();
        wanted.Mppt2!.WattPeak = 4200;

        var delta = wanted.GetToken(current);

        Assert.False(delta.HasAnyValue());
    }

    #endregion

    #region The export limits

    [Fact]
    public void PowerLimitDeltaNestsWhatChangedAndNothingMore()
    {
        var wanted = (Gen24PowerLimitSettings)Loaded().PowerLimitSettings.Clone();
        wanted.ExportLimits.ActivePower.HardLimit.PowerLimit = 6500;

        var delta = wanted.GetToken(Loaded().PowerLimitSettings);

        Assert.Equal(6500, delta["exportLimits"]?["activePower"]?["hardLimit"]?["powerLimit"]?.GetValue<double>());
        Assert.Null(delta["exportLimits"]?["activePower"]?["softLimit"]);
        Assert.Null(delta["visualization"]);
    }

    [Fact]
    public void PowerLimitDeltaSaysWhatUnitTheSoftLimitIsInWheneverItSendsOne()
    {
        var wanted = (Gen24PowerLimitSettings)Loaded().PowerLimitSettings.Clone();
        wanted.ExportLimits.ActivePower.SoftLimit.IsEnabled = true;
        wanted.ExportLimits.ActivePower.SoftLimit.PowerLimit = 3000;

        var delta = wanted.GetToken(Loaded().PowerLimitSettings);

        Assert.Equal(3000, delta["exportLimits"]?["activePower"]?["softLimit"]?["powerLimit"]?.GetValue<double>());
        Assert.Equal("absolute", delta["visualization"]?["exportLimits"]?["activePower"]?["displayModeSoftLimit"]?.GetValue<string>());
    }

    [Fact]
    public void PowerLimitDeltaSaysNothingAboutUnitsWhenTheSoftLimitDoesNotTravel()
    {
        var wanted = (Gen24PowerLimitSettings)Loaded().PowerLimitSettings.Clone();
        wanted.Visualization.WattPeakReferenceValue = 9000;

        var delta = wanted.GetToken(Loaded().PowerLimitSettings);

        Assert.Equal(9000, delta["visualization"]?["wattPeakReferenceValue"]?.GetValue<double>());
        Assert.Null(delta["visualization"]?["exportLimits"]);
    }

    [Fact]
    public void PowerLimitDeltaIsEmptyWhereNothingChanged()
    {
        var delta = ((Gen24PowerLimitSettings)Loaded().PowerLimitSettings.Clone()).GetToken(Loaded().PowerLimitSettings);

        Assert.False(delta.HasAnyValue());
        Assert.Empty(delta.AsObject());
    }

    [Fact]
    public void PowerLimitDeltaCarriesTheSwitchThatTurnsLimitingOff()
    {
        var wanted = (Gen24PowerLimitSettings)Loaded().PowerLimitSettings.Clone();
        wanted.ExportLimits.ActivePower.IsEnabled = false;

        var delta = wanted.GetToken(Loaded().PowerLimitSettings);

        Assert.False(delta["exportLimits"]?["activePower"]?["activated"]?.GetValue<bool>());
    }

    #endregion

    #region Over the wire

    /// <summary>
    /// The whole settings object travels to the client in the snapshot and back for a write, and two of its
    /// properties are derived from others with setters that write back - the obsolete <c>PowerLimitMode</c> and
    /// <c>IsNetworkModeEnabled</c>. A round trip that applied those in the wrong order would quietly change what
    /// the user asked for.
    /// </summary>
    [Theory]
    [InlineData(true, PhaseMode.PhaseSum, NetworkMode.Local)]
    [InlineData(true, PhaseMode.WeakestPhase, NetworkMode.Network)]
    [InlineData(false, PhaseMode.WeakestPhase, NetworkMode.Local)]
    [InlineData(false, PhaseMode.PhaseSum, NetworkMode.Network)]
    public void SurvivesTheRoundTripOverTheApi(bool isEnabled, PhaseMode phaseMode, NetworkMode networkMode)
    {
        var original = Loaded();
        original.PowerLimitSettings.ExportLimits.ActivePower.IsEnabled = isEnabled;
        original.PowerLimitSettings.ExportLimits.ActivePower.PhaseMode = phaseMode;
        original.PowerLimitSettings.ExportLimits.ActivePower.NetWorkMode = networkMode;

        var returned = JsonSerializer.Deserialize<Gen24InverterSettings>(JsonSerializer.Serialize(original));

        Assert.NotNull(returned);
        Assert.Equal("Hochstaetter", returned!.SystemName);
        Assert.Equal("Europe/Zurich", returned.TimeZoneName);
        Assert.True(returned.TimeSync);

        Assert.Equal(MpptPowerMode.Auto, returned.Mppt?.Mppt1?.PowerMode);
        Assert.Equal(MpptOnOff.On, returned.Mppt?.Mppt1?.DynamicPeakManager);
        Assert.Equal(5000u, returned.Mppt?.Mppt1?.WattPeak!.Value);
        Assert.Equal(600, returned.Mppt?.Mppt1?.DcFixedVoltage);
        Assert.Equal(MpptPowerMode.Fix, returned.Mppt?.Mppt2?.PowerMode);
        Assert.Equal(3000u, returned.Mppt?.Mppt2?.WattPeak!.Value);

        var activePower = returned.PowerLimitSettings.ExportLimits.ActivePower;
        Assert.Equal(isEnabled, activePower.IsEnabled);
        Assert.Equal(phaseMode, activePower.PhaseMode);
        Assert.Equal(networkMode, activePower.NetWorkMode);
        Assert.Equal(networkMode == NetworkMode.Network, activePower.IsNetworkModeEnabled);
        Assert.True(activePower.HardLimit.IsEnabled);
        Assert.Equal(7000, activePower.HardLimit.PowerLimit);
        Assert.Equal(8000, returned.PowerLimitSettings.Visualization.WattPeakReferenceValue);
    }

    /// <summary>
    /// A round trip must not change what the delta says either: the settings the client sends back have to look
    /// unchanged to the server when the user changed nothing.
    /// </summary>
    [Fact]
    public void ADeltaAgainstWhatCameBackOverTheApiIsEmpty()
    {
        var original = Loaded();
        var returned = JsonSerializer.Deserialize<Gen24InverterSettings>(JsonSerializer.Serialize(original))!;

        Assert.False(IoC.Get<IGen24JsonService>().GetUpdateToken(returned, original).HasAnyValue());
        Assert.False(returned.Mppt!.GetToken(original.Mppt).HasAnyValue());
        Assert.False(returned.PowerLimitSettings.GetToken(original.PowerLimitSettings).HasAnyValue());
    }

    #endregion

    #region What the inverter answers

    /// <summary>
    /// The shape the server reads: one call to <c>api/config/</c>, and each group picked out of it. The paths are
    /// easy to get wrong and a wrong one reads as "the inverter holds nothing", which would write everything.
    /// </summary>
    [Fact]
    public void ParsesTheGroupsOutOfWhatTheInverterAnswers()
    {
        var config = JsonNode.Parse
        ("""
         {
           "common": { "ui": { "systemName": "Hochstaetter", "timezone": "Europe/Zurich", "timesync": true } },
           "powerunit": { "powerunit": {
             "mppt": {
               "PV_MODE_COMBINE_U16": 0,
               "mppt1": { "PV_MODE_MPP_01_U16": "auto", "PV_MODE_DYNAMICPEAK_01_U16": "on", "PV_POWERACTIVE_CONNECTED_PEAK_MAX_01_U32": 5000, "PV_VOLTAGE_FIX_01_F32": 600 },
               "mppt2": { "PV_MODE_MPP_02_U16": "fix", "PV_POWERACTIVE_CONNECTED_PEAK_MAX_02_U32": 3000, "PV_VOLTAGE_FIX_02_F32": 400 }
             },
             "system": { "DEVICE_POWERACTIVE_NOMINAL_F32": 10000 }
           } },
           "limit_settings": { "powerLimits": {
             "visualization": { "wattPeakReferenceValue": 8000 },
             "exportLimits": {
               "failSafeModeEnabled": true,
               "activePower": {
                 "activated": true, "phaseMode": "phaseSum", "networkMode": "limitLocal",
                 "hardLimit": { "enabled": true, "powerLimit": 7000 },
                 "softLimit": { "enabled": false, "powerLimit": 0 }
               }
             }
           } }
         }
         """)!;

        var settings = Gen24InverterSettings.Parse(config);

        Assert.Equal("Hochstaetter", settings.SystemName);
        Assert.Equal("Europe/Zurich", settings.TimeZoneName);
        Assert.True(settings.TimeSync);

        Assert.Equal(MpptPowerMode.Auto, settings.Mppt?.Mppt1?.PowerMode);
        Assert.Equal(MpptOnOff.On, settings.Mppt?.Mppt1?.DynamicPeakManager);
        Assert.Equal(5000u, settings.Mppt?.Mppt1?.WattPeak!.Value);
        Assert.Equal(MpptPowerMode.Fix, settings.Mppt?.Mppt2?.PowerMode);
        Assert.Equal(400, settings.Mppt?.Mppt2?.DcFixedVoltage);

        Assert.True(settings.PowerLimitSettings.ExportLimits.EnableFailSafeMode);
        Assert.True(settings.PowerLimitSettings.ExportLimits.ActivePower.IsEnabled);
        Assert.Equal(PhaseMode.PhaseSum, settings.PowerLimitSettings.ExportLimits.ActivePower.PhaseMode);
        Assert.Equal(7000, settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit.PowerLimit);
        Assert.Equal(8000, settings.PowerLimitSettings.Visualization.WattPeakReferenceValue);

        // The same paths the mppt and powerLimits endpoints use to read what the inverter currently holds.
        Assert.Equal(5000u, Gen24Mppt.Parse(config["powerunit"]?["powerunit"]?["mppt"]).Mppt1?.WattPeak!.Value);
        Assert.Equal(8000, Gen24PowerLimitSettings.Parse(config["limit_settings"]?["powerLimits"]).Visualization.WattPeakReferenceValue);
    }

    #endregion
}
