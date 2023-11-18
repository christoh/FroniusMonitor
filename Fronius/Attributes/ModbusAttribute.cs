namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ModbusAttribute(ushort start, ushort length, bool isReadOnly = true) : Attribute
{
    public ModbusAttribute(ushort start, bool isReadOnly = true) : this(start, 0xffff, isReadOnly) { }

    public ushort Start { get; init; } = start;
    public ushort Length { get; init; } = length;
    public bool IsReadOnly { get; init; } = isReadOnly;
    public bool IsAccumulated { get; init; }
}
