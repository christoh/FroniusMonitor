namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class WattPilotAttribute : Attribute
{
    public WattPilotAttribute(string tokenName, bool isReadOnly, int index, Type? type)
    {
        TokenName = tokenName;
        Index = index;
        IsReadOnly = isReadOnly;
        Type = type;
    }

    public WattPilotAttribute(string tokenName, int index) : this(tokenName, true, index, null) { }
    public WattPilotAttribute(string tokenName) : this(tokenName, true, -1, null) { }
    public WattPilotAttribute(string tokenName, bool isReadOnly) : this(tokenName, isReadOnly, -1, null) { }
    public WattPilotAttribute(string tokenName, Type type) : this(tokenName, true, -1, type) { }

    public string TokenName { get; init; }
    public int Index { get; init; }
    public bool IsReadOnly { get; init; }
    public Type? Type { get; init; }
}
