﻿namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotPhaseMap(byte l1Map, byte l2Map, byte l3Map) : IHaveDisplayName
{
    public static IReadOnlyList<WattPilotPhaseMap> All => new[]
    {
        new WattPilotPhaseMap(1,2,3),
        new WattPilotPhaseMap(1,3,2),
        new WattPilotPhaseMap(2,1,3),
        new WattPilotPhaseMap(2,3,1),
        new WattPilotPhaseMap(3,1,2),
        new WattPilotPhaseMap(3,2,1),
        new WattPilotPhaseMap(1,0,0),
        new WattPilotPhaseMap(0,1,0),
        new WattPilotPhaseMap(0,0,1),
    };

    public byte L1Map { get; } = l1Map;
    public byte L2Map { get; } = l2Map;
    public byte L3Map { get; } = l3Map;
    public string DisplayName => ToString();

    public override string ToString() => $"{ToString(L1Map)} / {ToString(L2Map)} / {ToString(L3Map)}";
    
    private static string ToString(byte map) => map == 0 ? "-" : map.ToString(CultureInfo.CurrentCulture);
}
