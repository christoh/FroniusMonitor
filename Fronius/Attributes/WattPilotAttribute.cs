namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class WattPilotAttribute(string tokenName, bool isReadOnly, int index, Type? type) : Attribute
{
    public WattPilotAttribute(string tokenName, int index) : this(tokenName, true, index, null) { }
    public WattPilotAttribute(string tokenName, int index, bool isReadOnly) : this(tokenName, isReadOnly, index, null) { }
    public WattPilotAttribute(string tokenName) : this(tokenName, true, -1, null) { }
    public WattPilotAttribute(string tokenName, bool isReadOnly) : this(tokenName, isReadOnly, -1, null) { }
    public WattPilotAttribute(string tokenName, Type type) : this(tokenName, true, -1, type) { }
    public WattPilotAttribute(string tokenName,bool isReadOnly, Type type) : this(tokenName, isReadOnly, -1, type) { }

    public string TokenName { get; init; } = tokenName;
    public int Index { get; init; } = index;
    public bool IsReadOnly { get; init; } = isReadOnly;
    public Type? Type { get; init; } = type;
}
