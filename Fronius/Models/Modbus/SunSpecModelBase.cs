namespace De.Hochstaetter.Fronius.Models.Modbus;

public abstract class SunSpecModelBase : BindableBase
{
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    private SunSpecModelBase(ushort modelNumber, ushort absoluteRegister)
    {
        if (!SupportedModels.Contains(modelNumber))
        {
            throw new ArgumentException($"{GetType().Name} does not support SunSPec model {modelNumber}");
        }

        AbsoluteRegister = absoluteRegister;
        ModelNumber = modelNumber;
    }

    protected SunSpecModelBase(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : this(modelNumber, absoluteRegister)
    {
        Data = new Memory<byte>(new byte[data.Length]);
        data.CopyTo(Data);
    }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected SunSpecModelBase(ushort modelNumber, ushort absoluteRegister, ushort dataLength) : this(modelNumber, absoluteRegister)
    {
        Data = new Memory<byte>(new byte[Math.Max(dataLength, MinimumDataLength) << 1]);
    }

    public ushort DataLength => (ushort)(Data.Length >> 1);
    public ReadOnlyMemory<byte> RawData => Data;
    public ushort ModelNumber { get; private set; }
    protected Memory<byte> Data { get; private set; }
    public ushort AbsoluteRegister { get; private set; }
    public abstract IReadOnlyList<ushort> SupportedModels { get; }
    public abstract ushort MinimumDataLength { get; }

    public void CopyFrom(SunSpecModelBase other)
    {
        if (!GetType().IsInstanceOfType(other))
        {
            throw new ArgumentException($"Cannot copy from {other.GetType().Name} to {GetType().Name}");
        }

        Data = new Memory<byte>(new byte[other.Data.Length]);
        other.Data.CopyTo(Data);
        AbsoluteRegister = other.AbsoluteRegister;
        ModelNumber = other.ModelNumber;
    }

    protected string? GetString([CallerMemberName] string propertyName = null!)
    {
        var attribute = GetAttribute(propertyName);
        return Data.ReadString(attribute.Start, attribute.Length);
    }

    protected void SetString(string? value, [CallerMemberName] string propertyName = null!)
    {
        var attribute = GetAttribute(propertyName);
        var oldValue = Data.ReadString(attribute.Start, attribute.Length);
        Data.WriteString(value, attribute.Start, attribute.Length);

        if (!string.Equals(value, oldValue, StringComparison.Ordinal))
        {
            NotifyOfPropertyChange(propertyName);
        }
    }

    protected T Get<T>([CallerMemberName] string propertyName = null!) where T : unmanaged
    {
        var attribute = GetAttribute(propertyName);
        return Data.Read<T>(attribute.Start);
    }

    protected void Set<T>(T value, [CallerMemberName] string propertyName = null!) where T : unmanaged
    {
        var attribute = GetAttribute(propertyName);
        var oldValue = Data.Read<T>(attribute.Start);
        Data.Write(attribute.Start, value);

        if (!oldValue.Equals(value))
        {
            NotifyOfPropertyChange(propertyName);
        }
    }

    protected static double? ToDouble<T>(T value, short sf = 0, bool isAccumulated = false) where T : IConvertible
    {
        var isNull =
            value is (short)-32768 ||
            value is (ushort)0xffff ||
            value is unchecked((int)0x80000000) ||
            value is 0xffffffff && !isAccumulated ||
            value is 0U && isAccumulated ||
            value is (float)float.NaN ||
            sf == -32768;

        return isNull ? null : value.ToDouble(CultureInfo.InvariantCulture) * Math.Pow(10, sf);
    }

    protected static T FromDouble<T>(double? value, short sf = 0, bool isAccumulated = false) where T : IConvertible
    {
        object nullValue =
            typeof(T) == typeof(short) ? (short)-32768 :
            typeof(T) == typeof(ushort) ? (ushort)0xffff :
            typeof(T) == typeof(int) ? unchecked((int)0x80000000) :
            typeof(T) == typeof(uint) ? (isAccumulated ? 0 : 0xffffffff) :
            typeof(T) == typeof(float) ? float.NaN :
            throw new NotSupportedException("Unsupported type");

        return (T)Convert.ChangeType
        (
            value is null or double.NaN || sf == -32768 ? nullValue : Math.Round(value.Value / Math.Pow(10, sf), typeof(T) != typeof(float) ? 0 : 8, MidpointRounding.AwayFromZero),
            typeof(T),
            CultureInfo.InvariantCulture
        );
    }


    private ModbusAttribute GetAttribute(string propertyName)
    {
        var propertyInfo = GetType().GetProperty(propertyName) ?? throw new InvalidDataException("Property is not public instance");
        return propertyInfo.GetCustomAttribute(typeof(ModbusAttribute)) as ModbusAttribute ?? throw new InvalidDataException($"{nameof(ModbusAttribute)} is missing");
    }
}
