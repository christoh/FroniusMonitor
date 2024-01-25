namespace De.Hochstaetter.Fronius.Models.Charging;

public record WattPilotPhaseMap(byte L1Map, byte L2Map, byte L3Map) : IHaveDisplayName
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

    public string DisplayName => ToString();

    public int[] Array = [L1Map, L2Map, L3Map];

    public override string ToString() => $"{ToString(L1Map)} / {ToString(L2Map)} / {ToString(L3Map)}";

    private static string ToString(byte map) => map == 0 ? "-" : "L" + map.ToString(CultureInfo.CurrentCulture);
}
