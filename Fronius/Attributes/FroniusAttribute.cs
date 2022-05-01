namespace De.Hochstaetter.Fronius.Attributes
{
    public enum FroniusDataType : byte
    {
        Attribute,
        Channel,
    }

    public enum Unit : byte
    {
        Default=0,
        Joule,
        Percent,
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FroniusAttribute : Attribute
    {
        public FroniusAttribute(string name, FroniusDataType dataType, Unit unit)
        {
            Name = name;
            DataType = dataType;
            Unit = unit;
        }

        public FroniusAttribute(string name) : this(name, FroniusDataType.Channel, Unit.Default) { }

        public FroniusAttribute(string name, Unit unit) : this(name, FroniusDataType.Channel, unit) { }
        public FroniusAttribute(string name, FroniusDataType dataType) : this(name, dataType, Unit.Default) { }

        public FroniusAttribute() : this(string.Empty) { }

        public string Name { get; init; }
        public FroniusDataType DataType { get; init; }
        public Unit Unit { get; init; }
    }
}
