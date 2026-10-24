using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FritzBox.SmartHome;

/// <summary>
/// Turns the REST overview into the <see cref="FritzBoxDeviceList"/> the AHA interface delivers, so that the server,
/// its JSON and both clients see no difference.
/// </summary>
/// <remarks>
/// <para>
/// One <see cref="FritzBoxDevice"/> per unit, under the unit's AIN. AHA lists a lamp twice, as the device (which can
/// do nothing) and as its unit (which switches, dims and colors); only the unit is of use, so the device entry is not
/// made. An outlet is a single unit with the AIN of its device in both interfaces.
/// </para>
/// <para>
/// What AHA has and REST does not: the numeric <c>id</c> (made up from the UID here, see <see cref="GetId"/>), the
/// switching mode <c>auto</c>/<c>manuell</c> and <c>txbusy</c> (left empty), and the temperature offset (not in the
/// overview, so <see cref="FritzBoxTemperatureSensor.Offset"/> stays empty and the temperature is the one the device
/// measures, without the offset AHA adds).
/// </para>
/// </remarks>
internal static class SmartHomeMapper
{
    public static FritzBoxDeviceList ToDeviceList(SmartHomeOverview overview)
    {
        var devices = overview.Devices.ToDictionary(d => d.Uid);
        var result = new FritzBoxDeviceList();

        foreach (var unit in overview.Units.Where(u => !u.IsGroupUnit))
        {
            result.Devices.Add(ToDevice(unit, unit.DeviceUid is { } deviceUid ? devices.GetValueOrDefault(deviceUid) : null));
        }

        return result;
    }

    private static FritzBoxDevice ToDevice(SmartHomeUnit unit, SmartHomeDevice? device)
    {
        var interfaces = unit.Interfaces;
        var features = GetFeatures(unit, device);

        return new FritzBoxDevice
        {
            Id = GetId(unit.Uid),
            Ain = string.IsNullOrEmpty(unit.Ain) ? unit.Uid : unit.Ain,
            DisplayName = unit.Name ?? device?.Name ?? string.Empty,
            Manufacturer = device?.Manufacturer,
            Model = device?.ProductName,
            FirmwareVersionString = device?.FirmwareVersion,
            IsPresent = unit.IsConnected && device?.IsConnected != false,
            Features = features,
            SimpleSwitch = interfaces.OnOffInterface is { } onOff ? new FritzBoxSimpleSwitch { IsTurnedOn = onOff.IsValid ? onOff.Active : null } : null,
            Switch = ToSwitch(interfaces.OnOffInterface, features),
            LevelControl = ToLevel(interfaces.LevelControlInterface),
            Color = ToColor(interfaces.ColorControlInterface),
            PowerMeter = ToPowerMeter(interfaces.MultimeterInterface),
            TemperatureSensor = interfaces.TemperatureInterface is { } temperature ? new FritzBoxTemperatureSensor { Temperature = temperature.IsValid ? temperature.Celsius : null } : null,
        };
    }

    /// <summary>
    /// The REST API has no numeric id. AHA's ids are small numbers the box hands out; this one is the 32 bit FNV-1a hash
    /// of the UID, the same for a unit at every poll and on every box. Code that still tells devices apart by
    /// <see cref="FritzBoxDevice.Id"/> keeps working that way, and a hash of a dozen UIDs does not collide in practice.
    /// </summary>
    internal static uint GetId(string uid)
    {
        var hash = 2166136261u;

        foreach (var c in uid)
        {
            hash = unchecked((hash ^ c) * 16777619u);
        }

        return hash;
    }

    /// <summary>The bits AHA would set in <c>functionbitmask</c> for this unit.</summary>
    private static FritzBoxFeatures GetFeatures(SmartHomeUnit unit, SmartHomeDevice? device)
    {
        var interfaces = unit.Interfaces;
        var features = FritzBoxFeatures.None;

        if (device is { IsZigbeeDevice: false } && device.Uid != unit.Uid)
        {
            features |= FritzBoxFeatures.HanFunUnit;
        }

        features |= device?.ProductCategory switch
        {
            "lamp" => FritzBoxFeatures.Light,
            "socket" => FritzBoxFeatures.AcOutlet,
            _ => FritzBoxFeatures.None,
        };

        if (interfaces.OnOffInterface != null) features |= FritzBoxFeatures.TurnOnOff;
        if (interfaces.LevelControlInterface != null) features |= FritzBoxFeatures.HasLevels;
        if (interfaces.ColorControlInterface != null) features |= FritzBoxFeatures.ColoredLight;
        if (interfaces.MultimeterInterface != null) features |= FritzBoxFeatures.PowerMeter;
        if (interfaces.TemperatureInterface != null) features |= FritzBoxFeatures.TemperatureSensor;
        return features;
    }

    /// <summary>AHA has a <c>switch</c> besides <c>simpleonoff</c> only for outlets. It carries their locks.</summary>
    private static FritzBoxSwitch? ToSwitch(SmartHomeOnOff? onOff, FritzBoxFeatures features) =>
        onOff == null || (features & FritzBoxFeatures.AcOutlet) == FritzBoxFeatures.None && onOff is { IsLockedDeviceApi: null, IsLockedDeviceLocal: null }
            ? null
            : new FritzBoxSwitch
            {
                IsTurnedOn = onOff.IsValid ? onOff.Active : null,
                IsUiLocked = onOff.IsLockedDeviceApi,
                IsDeviceLocked = onOff.IsLockedDeviceLocal,
            };

    private static FritzBoxLevel? ToLevel(SmartHomeLevelControl? levelControl)
    {
        if (levelControl == null)
        {
            return null;
        }

        var percent = levelControl.IsValid ? levelControl.Level : null;
        return new FritzBoxLevel { Level = percent / 100d, LevelAbsolute = percent.HasValue ? Math.Round(percent.Value * 2.55, MidpointRounding.AwayFromZero) : null };
    }

    private static FritzBoxColorControl? ToColor(SmartHomeColorControl? color)
    {
        if (color == null)
        {
            return null;
        }

        var hsColor = color.IsValid ? color.HsColor : null;

        return new FritzBoxColorControl
        {
            SupportedModes = color.SupportedColorModes?.Aggregate(FritzBoxColorMode.None, (modes, mode) => modes | ToColorMode(mode)),
            CurrentMode = color.IsValid ? ToColorMode(color.CurrentColorMode) : null,
            HueDegrees = hsColor?.Hue,
            SaturationAbsolute = hsColor?.Saturation,
            UnmappedHueDegrees = hsColor?.Hue,
            UnmappedSaturation = hsColor?.Saturation,
            TemperatureKelvin = color.IsValid ? color.ColorTemperature : null,
        };
    }

    private static FritzBoxColorMode ToColorMode(string? mode) => mode switch
    {
        SmartHomeColorControl.HueSaturation => FritzBoxColorMode.Hsv,
        SmartHomeColorControl.Temperature => FritzBoxColorMode.Temperature,
        _ => FritzBoxColorMode.None,
    };

    private static FritzBoxPowerMeter? ToPowerMeter(SmartHomeMultimeter? multimeter) => multimeter == null
        ? null
        : new FritzBoxPowerMeter
        {
            PowerWatts = multimeter.IsValid ? multimeter.Power / 1000d : null,
            Voltage = multimeter.IsValid ? multimeter.Voltage / 1000d : null,
            EnergyConsumed = multimeter.IsValid ? multimeter.Energy : null,
        };
}
