namespace De.Hochstaetter.Fronius.Models.Charging;

public record WattPilotPhaseMap(byte L1Map, byte L2Map, byte L3Map)
{
    public static IReadOnlyList<WattPilotPhaseMap> All =>
    [
        new WattPilotPhaseMap(1,2,3),
        new WattPilotPhaseMap(1,3,2),
        new WattPilotPhaseMap(2,1,3),
        new WattPilotPhaseMap(2,3,1),
        new WattPilotPhaseMap(3,1,2),
        new WattPilotPhaseMap(3,2,1),
        new WattPilotPhaseMap(1,0,0),
        new WattPilotPhaseMap(0,1,0),
        new WattPilotPhaseMap(0,0,1),
    ];

    public override string ToString() => $"{ToString(L1Map)} / {ToString(L2Map)} / {ToString(L3Map)}";

    private static string ToString(byte map) => map switch
    {
        0 => "-",
        _ => "L" + map.ToString(CultureInfo.CurrentCulture)
    };
}
