namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ModbusAttribute : Attribute
{
    public ModbusAttribute(ushort start, ushort length, string? unitName = null, bool isReadOnly = true)
    {
        Start = start;
        Length = length;
        IsReadOnly = isReadOnly;
        UnitName = unitName;
    }

    public ModbusAttribute(ushort start, string? unitName = null, bool isReadOnly = true) : this(start, 0xffff, unitName, isReadOnly) { }

    public ushort Start { get; init; }
    public ushort Length { get; init; }
    public bool IsReadOnly { get; init; }
    public string? UnitName { get; init; }
}
