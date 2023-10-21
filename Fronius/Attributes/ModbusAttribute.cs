namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ModbusAttribute : Attribute
{
    public ModbusAttribute(ushort start, ushort length, bool isReadOnly = true)
    {
        Start = start;
        Length = length;
        IsReadOnly = isReadOnly;
    }

    public ModbusAttribute(ushort start, bool isReadOnly = true):this(start,0xffff,isReadOnly){ }

    public ushort Start { get; init; }
    public ushort Length { get; init; }
    public bool IsReadOnly { get; init; }
}