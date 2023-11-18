namespace De.Hochstaetter.Fronius.Attributes;

public enum FroniusDataType : byte
{
    Attribute,
    Channel,
    Root,
    Custom,
}

public enum Unit : byte
{
    Default = 0,
    Joule,
    Percent,
    Time
}

[AttributeUsage(AttributeTargets.Property)]
internal class FroniusProprietaryImportAttribute(string name, FroniusDataType dataType, Unit unit, string? propertyName) : Attribute
{
    public FroniusProprietaryImportAttribute(string name, FroniusDataType dataType, Unit unit) : this(name, dataType, unit, null) { }
    public FroniusProprietaryImportAttribute(string name) : this(name, FroniusDataType.Channel, Unit.Default, null) { }
    public FroniusProprietaryImportAttribute(string name, Unit unit) : this(name, FroniusDataType.Channel, unit, null) { }
    public FroniusProprietaryImportAttribute(string name, FroniusDataType dataType) : this(name, dataType, Unit.Default, null) { }
    public FroniusProprietaryImportAttribute(string propertyName, string name) : this(name, FroniusDataType.Custom, Unit.Default, propertyName) { }
    public FroniusProprietaryImportAttribute(string propertyName, string name, Unit unit) : this(name, FroniusDataType.Custom, unit, propertyName) { }

    public string Name => name;
    public FroniusDataType DataType => dataType;
    public Unit Unit => unit;
    public string? PropertyName => propertyName;
}
