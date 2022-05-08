namespace De.Hochstaetter.Fronius.Attributes;

public enum FroniusDataType : byte
{
    Attribute,
    Channel,
    Root,
}

public enum Unit : byte
{
    Default=0,
    Joule,
    Percent,
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
internal class FroniusProprietaryImportAttribute : Attribute
{
    public FroniusProprietaryImportAttribute(string name, FroniusDataType dataType, Unit unit)
    {
        Name = name;
        DataType = dataType;
        Unit = unit;
    }

    public FroniusProprietaryImportAttribute(string name) : this(name, FroniusDataType.Channel, Unit.Default) { }

    public FroniusProprietaryImportAttribute(string name, Unit unit) : this(name, FroniusDataType.Channel, unit) { }
    public FroniusProprietaryImportAttribute(string name, FroniusDataType dataType) : this(name, dataType, Unit.Default) { }

    public FroniusProprietaryImportAttribute() : this(string.Empty) { }

    public string Name { get; init; }
    public FroniusDataType DataType { get; init; }
    public Unit Unit { get; init; }
}