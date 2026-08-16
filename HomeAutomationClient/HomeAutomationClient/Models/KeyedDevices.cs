using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using System.Collections.Concurrent;

namespace De.Hochstaetter.HomeAutomationClient.Models;

public class KeyedDevice<T> : IKeyedDevice
{
    public required string Key { get; init; }
    object IKeyedDevice.Blob => Device!;
    public required T Device { get; init; }
    public override string ToString() => Device?.ToString() ?? Key;
}

public class KeyedFritzBoxDevice : KeyedDevice<FritzBoxDevice>;

public class KeyedGen24System : KeyedDevice<Gen24System>;

public class KeyedWattPilot : KeyedDevice<WattPilot>;

public class KeyedWattPilotUpdate : KeyedDevice<ConcurrentQueue<WattPilotUpdate>>;
