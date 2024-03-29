﻿namespace De.Hochstaetter.Fronius.Models;

public class ModbusMapping : IHaveDisplayName
{
    [XmlAttribute]
    public byte ModbusAddress { get; set; }

    [XmlAttribute]
    public string SerialNumber { get; set; } = string.Empty;

    private byte phase = 1;

    [XmlAttribute]
    public byte Phase
    {
        get => phase;
        set => phase = value is >= 1 and <= 3 ? value : throw new InvalidDataException(Resources.IncorrectPhaseNumber);
    }

    public string DisplayName => $"{SerialNumber} => {ModbusAddress}";

    public override string ToString() => DisplayName;
}