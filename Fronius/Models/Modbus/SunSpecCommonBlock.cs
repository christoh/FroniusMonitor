﻿namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecCommonBlock : SunSpecModelBase
{
    public SunSpecCommonBlock(Memory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

    public SunSpecCommonBlock(ushort absoluteRegister, ushort dataLength = 0) : base(1, absoluteRegister, dataLength) { }

    [Modbus(0, 16)]
    public string? Manufacturer
    {
        get => GetString();
        set => SetString(value);
    }

    [Modbus(16, 16)]
    public string? ModelName
    {
        get => GetString();
        set => SetString(value);
    }

    [Modbus(32, 8)]
    public string? Options
    {
        get => GetString();
        set => SetString(value);
    }

    [Modbus(40, 8)]
    public string? Version
    {
        get => GetString();
        set => SetString(value);
    }

    [Modbus(48, 16)]
    public string? SerialNumber
    {
        get => GetString();
        set => SetString(value);
    }

    [Modbus(64)]
    public ushort ModbusAddress
    {
        get => Get<ushort>();
        set => Set(value);
    }

    public override string ToString() => $"{Manufacturer ?? "---"} - {ModelName ?? "---"} - {SerialNumber ?? "---"}";

    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 1, };

    public override ushort MinimumDataLength => 65;
}