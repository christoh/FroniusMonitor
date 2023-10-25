namespace De.Hochstaetter.Fronius.Services.Modbus;

public class SunSpecMeterClient : SunSpecClientBase, ISunSpecMeterClient
{
    public async Task<SunSpecMeterOld> GetDataAsync(CancellationToken token = default)
    {
        if (!IsConnected)
        {
            throw new SocketException((int)SocketError.NotConnected);
        }

        var (meter, deviceTypeAndDataFormat, numberOfRegisters, data) = await CreateSunSpecDevice<SunSpecMeterOld>(token).ConfigureAwait(false);
        meter.Protocol = (SunSpecProtocol)(deviceTypeAndDataFormat / 10 % 10);
        meter.MeterType = (SunSpecMeterType)(deviceTypeAndDataFormat % 10);

        switch (meter.Protocol)
        {
            case SunSpecProtocol.Float:
                if (numberOfRegisters < 124)
                {
                    throw new InvalidDataException("Need at least 124 registers");
                }

                await Task.Run(() => ReadFloatData(meter, data), token).ConfigureAwait(false);
                break;

            case SunSpecProtocol.IntAndScaleFactor:
                if (numberOfRegisters < 105)
                {
                    throw new InvalidDataException("Need at least 105 registers");
                }

                await Task.Run(() => ReadIntSfData(meter, data), token).ConfigureAwait(false);
                break;

            default:
                throw new InvalidDataException("Unknown Sunspec protocol");
        }

        return meter;
    }

    private static void ReadFloatData(SunSpecMeterOld meterOld, ReadOnlyMemory<byte> data)
    {
        meterOld.TotalCurrent = Read<float>(40072);
        meterOld.CurrentL1 = Read<float>(40074);
        meterOld.CurrentL2 = Read<float>(40076);
        meterOld.CurrentL3 = Read<float>(40078);

        meterOld.PhaseVoltageAverage = Read<float>(40080);
        meterOld.PhaseVoltageL1 = Read<float>(40082);
        meterOld.PhaseVoltageL2 = Read<float>(40084);
        meterOld.PhaseVoltageL3 = Read<float>(40086);
        meterOld.LineVoltageAverage = Read<float>(40088);
        meterOld.LineVoltageL12 = Read<float>(40090);
        meterOld.LineVoltageL23 = Read<float>(40092);
        meterOld.LineVoltageL31 = Read<float>(40094);

        meterOld.Frequency = Read<float>(40096);

        meterOld.ActivePowerSum = Read<float>(40098);
        meterOld.ActivePowerL1 = Read<float>(40100);
        meterOld.ActivePowerL2 = Read<float>(40102);
        meterOld.ActivePowerL3 = Read<float>(40104);

        meterOld.ApparentPowerSum = Read<float>(40106);
        meterOld.ApparentPowerL1 = Read<float>(40108);
        meterOld.ApparentPowerL2 = Read<float>(40110);
        meterOld.ApparentPowerL3 = Read<float>(40112);

        meterOld.ReactivePowerSum = Read<float>(40114);
        meterOld.ReactivePowerL1 = Read<float>(40116);
        meterOld.ReactivePowerL2 = Read<float>(40118);
        meterOld.ReactivePowerL3 = Read<float>(40120);

        meterOld.PowerFactorTotal = Read<float>(40122);
        meterOld.PowerFactorL1 = Read<float>(40124);
        meterOld.PowerFactorL2 = Read<float>(40126);
        meterOld.PowerFactorL3 = Read<float>(40128);

        meterOld.EnergyActiveProduced = Read<float>(40130);
        meterOld.EnergyActiveProducedL1 = Read<float>(40132);
        meterOld.EnergyActiveProducedL2 = Read<float>(40134);
        meterOld.EnergyActiveProducedL3 = Read<float>(40136);
        meterOld.EnergyActiveConsumed = Read<float>(40138);
        meterOld.EnergyActiveConsumedL1 = Read<float>(40140);
        meterOld.EnergyActiveConsumedL2 = Read<float>(40142);
        meterOld.EnergyActiveConsumedL3 = Read<float>(40144);

        meterOld.EnergyApparentProduced = Read<float>(40146);
        meterOld.EnergyApparentProducedL1 = Read<float>(40148);
        meterOld.EnergyApparentProducedL2 = Read<float>(40150);
        meterOld.EnergyApparentProducedL3 = Read<float>(40152);
        meterOld.EnergyApparentConsumed = Read<float>(40154);
        meterOld.EnergyApparentConsumedL1 = Read<float>(40156);
        meterOld.EnergyApparentConsumedL2 = Read<float>(40158);
        meterOld.EnergyApparentConsumedL3 = Read<float>(40160);

        meterOld.EnergyReactiveConsumedQ1 = Read<float>(40162);
        meterOld.EnergyReactiveConsumedQ1L1 = Read<float>(40164);
        meterOld.EnergyReactiveConsumedQ1L2 = Read<float>(40166);
        meterOld.EnergyReactiveConsumedQ1L3 = Read<float>(40168);
        meterOld.EnergyReactiveConsumedQ2 = Read<float>(40170);
        meterOld.EnergyReactiveConsumedQ2L1 = Read<float>(40172);
        meterOld.EnergyReactiveConsumedQ2L2 = Read<float>(40174);
        meterOld.EnergyReactiveConsumedQ2L3 = Read<float>(40176);

        meterOld.EnergyReactiveProducedQ3 = Read<float>(40178);
        meterOld.EnergyReactiveProducedQ3L1 = Read<float>(40180);
        meterOld.EnergyReactiveProducedQ3L2 = Read<float>(40182);
        meterOld.EnergyReactiveProducedQ3L3 = Read<float>(40184);
        meterOld.EnergyReactiveProducedQ4 = Read<float>(40186);
        meterOld.EnergyReactiveProducedQ4L1 = Read<float>(40188);
        meterOld.EnergyReactiveProducedQ4L2 = Read<float>(40190);
        meterOld.EnergyReactiveProducedQ4L3 = Read<float>(40192);

        meterOld.EventBitField = Read<uint>(40194);
        
        return;

        [Pure]
        T Read<T>(ushort registerNumber) where T : unmanaged => ReadAbsolute<T>(data, registerNumber);
    }

    private static void ReadIntSfData(SunSpecMeterOld meterOld, ReadOnlyMemory<byte> data)
    {
        var currentScaleFactor = Math.Pow(10, Read<short>(40076));//-4
        meterOld.TotalCurrent = Read<short>(40072) * currentScaleFactor;
        meterOld.CurrentL1 = Read<short>(40073) * currentScaleFactor;
        meterOld.CurrentL2 = Read<short>(40074) * currentScaleFactor;
        meterOld.CurrentL3 = Read<short>(40075) * currentScaleFactor;

        var voltageScaleFactor = Math.Pow(10, Read<short>(40085));//-1
        meterOld.PhaseVoltageAverage = Read<short>(40077) * voltageScaleFactor;
        meterOld.PhaseVoltageL1 = Read<short>(40078) * voltageScaleFactor;
        meterOld.PhaseVoltageL2 = Read<short>(40079) * voltageScaleFactor;
        meterOld.PhaseVoltageL3 = Read<short>(40080) * voltageScaleFactor;
        meterOld.LineVoltageAverage = Read<short>(40081) * voltageScaleFactor;
        meterOld.LineVoltageL12 = Read<short>(40082) * voltageScaleFactor;
        meterOld.LineVoltageL23 = Read<short>(40083) * voltageScaleFactor;
        meterOld.LineVoltageL31 = Read<short>(40084) * voltageScaleFactor;

        var frequencyScaleFactor = Math.Pow(10, Read<short>(40087));//-2
        meterOld.Frequency = Read<short>(40086) * frequencyScaleFactor;

        var activePowerScaleFactor = Math.Pow(10, Read<short>(40092));//-2
        meterOld.ActivePowerSum = Read<short>(40088) * activePowerScaleFactor;
        meterOld.ActivePowerL1 = Read<short>(40089) * activePowerScaleFactor;
        meterOld.ActivePowerL2 = Read<short>(40090) * activePowerScaleFactor;
        meterOld.ActivePowerL3 = Read<short>(40091) * activePowerScaleFactor;

        var apparentPowerScaleFactor = Math.Pow(10, Read<short>(40097));//-2
        meterOld.ApparentPowerSum = Read<short>(40093) * apparentPowerScaleFactor;
        meterOld.ApparentPowerL1 = Read<short>(40094) * apparentPowerScaleFactor;
        meterOld.ApparentPowerL2 = Read<short>(40095) * apparentPowerScaleFactor;
        meterOld.ApparentPowerL3 = Read<short>(40096) * apparentPowerScaleFactor;

        var reactivePowerScaleFactor = Math.Pow(10, Read<short>(40102));//-2
        meterOld.ReactivePowerSum = Read<short>(40098) * reactivePowerScaleFactor;
        meterOld.ReactivePowerL1 = Read<short>(40099) * reactivePowerScaleFactor;
        meterOld.ReactivePowerL2 = Read<short>(40100) * reactivePowerScaleFactor;
        meterOld.ReactivePowerL3 = Read<short>(40101) * reactivePowerScaleFactor;

        var powerFactorScaleFactor = Math.Pow(10, Read<short>(40107)) / 100d;//-1
        meterOld.PowerFactorTotal = Read<short>(40103) * powerFactorScaleFactor;
        meterOld.PowerFactorL1 = Read<short>(40104) * powerFactorScaleFactor;
        meterOld.PowerFactorL2 = Read<short>(40105) * powerFactorScaleFactor;
        meterOld.PowerFactorL3 = Read<short>(40106) * powerFactorScaleFactor;

        var energyActiveScaleFactor = Math.Pow(10, Read<short>(40124)); //-3
        meterOld.EnergyActiveProduced = Read<uint>(40108) * energyActiveScaleFactor;
        meterOld.EnergyActiveProducedL1 = Read<uint>(40110) * energyActiveScaleFactor;
        meterOld.EnergyActiveProducedL2 = Read<uint>(40112) * energyActiveScaleFactor;
        meterOld.EnergyActiveProducedL3 = Read<uint>(40114) * energyActiveScaleFactor;
        meterOld.EnergyActiveConsumed = Read<uint>(40116) * energyActiveScaleFactor;
        meterOld.EnergyActiveConsumedL1 = Read<uint>(40118) * energyActiveScaleFactor;
        meterOld.EnergyActiveConsumedL2 = Read<uint>(40120) * energyActiveScaleFactor;
        meterOld.EnergyActiveConsumedL3 = Read<uint>(40122) * energyActiveScaleFactor;
        
        var energyApparentScaleFactor = Math.Pow(10, Read<short>(40141)); //-32768
        meterOld.EnergyApparentProduced = Read<uint>(40125) * energyApparentScaleFactor;
        meterOld.EnergyApparentProducedL1 = Read<uint>(40127) * energyApparentScaleFactor;
        meterOld.EnergyApparentProducedL2 = Read<uint>(40129) * energyApparentScaleFactor;
        meterOld.EnergyApparentProducedL3 = Read<uint>(40131) * energyApparentScaleFactor;
        meterOld.EnergyApparentConsumed = Read<uint>(40133) * energyApparentScaleFactor;
        meterOld.EnergyApparentConsumedL1 = Read<uint>(40135) * energyApparentScaleFactor;
        meterOld.EnergyApparentConsumedL2 = Read<uint>(40137) * energyApparentScaleFactor;
        meterOld.EnergyApparentConsumedL3 = Read<uint>(40139) * energyApparentScaleFactor;

        var energyReactiveScaleFactor = Math.Pow(10, Read<short>(40174)); //-32768
        meterOld.EnergyReactiveConsumedQ1 = Read<uint>(40142) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveConsumedQ1L1 = Read<uint>(40144) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveConsumedQ1L2 = Read<uint>(40146) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveConsumedQ1L3 = Read<uint>(40148) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveConsumedQ2 = Read<uint>(40150) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveConsumedQ2L1 = Read<uint>(40152) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveConsumedQ2L2 = Read<uint>(40154) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveConsumedQ2L3 = Read<uint>(40156) * energyReactiveScaleFactor;

        meterOld.EnergyReactiveProducedQ3 = Read<uint>(40158) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveProducedQ3L1 = Read<uint>(40160) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveProducedQ3L2 = Read<uint>(40162) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveProducedQ3L3 = Read<uint>(40164) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveProducedQ4 = Read<uint>(40166) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveProducedQ4L1 = Read<uint>(40168) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveProducedQ4L2 = Read<uint>(40170) * energyReactiveScaleFactor;
        meterOld.EnergyReactiveProducedQ4L3 = Read<uint>(40172) * energyReactiveScaleFactor;

        meterOld.EventBitField = Read<uint>(40175);
        
        return;
        
        [Pure]
        T Read<T>(ushort registerNumber) where T : unmanaged => ReadAbsolute<T>(data, registerNumber);
    }
    
    [Pure]
    private static T ReadAbsolute<T>(ReadOnlyMemory<byte> data, ushort registerNumber) where T : unmanaged => ReadRelative<T>(data, (ushort)((registerNumber - 40072) << 1));
}