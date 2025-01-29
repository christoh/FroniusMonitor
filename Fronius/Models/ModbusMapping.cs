namespace De.Hochstaetter.Fronius.Models;

public class ModbusMapping : IHaveDisplayName
{
    [XmlAttribute]
    public byte ModbusAddress { get; set; }

    [XmlAttribute]
    public string SerialNumber { get; set; } = string.Empty;

    [XmlAttribute]
    public byte Phase
    {
        get;
        set => field = value is >= 1 and <= 3 ? value : throw new InvalidDataException(Resources.IncorrectPhaseNumber);
    } = 1;

    public string DisplayName => $"{SerialNumber} => {ModbusAddress}";

    public override string ToString() => DisplayName;
}