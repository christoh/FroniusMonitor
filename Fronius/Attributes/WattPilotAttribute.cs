namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class WattPilotAttribute : Attribute
{
    public WattPilotAttribute(string tokenName, bool isReadOnly, int index)
    {
        TokenName = tokenName;
        Index = index;
        IsReadOnly = isReadOnly;
    }

    public WattPilotAttribute(string tokenName, int index) : this(tokenName, true, index) { }
    public WattPilotAttribute(string tokenName) : this(tokenName, true, -1) { }
    public WattPilotAttribute(string tokenName, bool isReadOnly) : this(tokenName, isReadOnly, -1) { }

    public string TokenName { get; init; }
    public int Index { get; init; }
    public bool IsReadOnly { get; init; }
}
