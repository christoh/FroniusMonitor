namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class WattPilotAttribute : Attribute
{
    public WattPilotAttribute(string tokenName, int index = -1)
    {
        TokenName = tokenName;
        Index = index;
    }

    public string TokenName { get; init; }
    public int Index { get; init; }
}
