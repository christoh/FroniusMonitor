using System.Collections.Concurrent;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Models;

public class KeyedDevice<T> : IKeyedDevice
{
    public required string Key { get; init; }
    object IKeyedDevice.Device => Device!;
    public required T Device { get; init; }
    public override string ToString()
    {
        return (Device switch
        {
            Gen24PowerMeter3P smartMeter => smartMeter.Model,
            Gen24System gen24System => gen24System.Config?.InverterSettings?.SystemName ?? gen24System.Model ?? gen24System.Manufacturer + " " + gen24System.SerialNumber,
            Gen24Storage storage => storage.Model ?? $"{Loc.Battery}: {storage.Model}",
            WattPilot wattPilot => wattPilot.DeviceName,
            _ => Device?.ToString() ?? Key,
        }) ?? Loc.Unknown;
    }
}

public class KeyedFritzBoxDevice : KeyedDevice<FritzBoxDevice>;

public class KeyedGen24System : KeyedDevice<Gen24System>;

public class KeyedWattPilot : KeyedDevice<WattPilot>;

public class KeyedWattPilotUpdate : KeyedDevice<ConcurrentQueue<WattPilotUpdate>>;
