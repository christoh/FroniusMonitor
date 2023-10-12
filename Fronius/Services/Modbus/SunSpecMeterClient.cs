namespace De.Hochstaetter.Fronius.Services.Modbus;

public class SunSpecMeterClient : SunSpecClientBase, ISunSpecMeterClient
{
    public async Task<SunSpecMeter> GetDataAsync(CancellationToken token = default)
    {
        if (!IsConnected)
        {
            throw new SocketException((int)SocketError.NotConnected);
        }

        var (meter, deviceTypeAndDataFormat, numberOfRegisters, data) = await CreateSunSpecDevice<SunSpecMeter>(token).ConfigureAwait(false);
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

    private static void ReadFloatData(SunSpecMeter meter, ReadOnlyMemory<byte> data)
    {
        meter.TotalCurrent = Read<float>(40072);
        meter.CurrentL1 = Read<float>(40074);
        meter.CurrentL2 = Read<float>(40076);
        meter.CurrentL3 = Read<float>(40078);

        meter.PhaseVoltageAverage = Read<float>(40080);
        meter.PhaseVoltageL1 = Read<float>(40082);
        meter.PhaseVoltageL2 = Read<float>(40084);
        meter.PhaseVoltageL3 = Read<float>(40086);
        meter.LineVoltageAverage = Read<float>(40088);
        meter.LineVoltageL12 = Read<float>(40090);
        meter.LineVoltageL23 = Read<float>(40092);
        meter.LineVoltageL31 = Read<float>(40094);

        meter.Frequency = Read<float>(40096);

        meter.ActivePowerSum = Read<float>(40098);
        meter.ActivePowerL1 = Read<float>(40100);
        meter.ActivePowerL2 = Read<float>(40102);
        meter.ActivePowerL3 = Read<float>(40104);

        meter.ApparentPowerSum = Read<float>(40106);
        meter.ApparentPowerL1 = Read<float>(40108);
        meter.ApparentPowerL2 = Read<float>(40110);
        meter.ApparentPowerL3 = Read<float>(40112);

        meter.ReactivePowerSum = Read<float>(40114);
        meter.ReactivePowerL1 = Read<float>(40116);
        meter.ReactivePowerL2 = Read<float>(40118);
        meter.ReactivePowerL3 = Read<float>(40120);

        meter.PowerFactorTotal = Read<float>(40122);
        meter.PowerFactorL1 = Read<float>(40124);
        meter.PowerFactorL2 = Read<float>(40126);
        meter.PowerFactorL3 = Read<float>(40128);

        meter.EnergyActiveProduced = Read<float>(40130);
        meter.EnergyActiveProducedL1 = Read<float>(40132);
        meter.EnergyActiveProducedL2 = Read<float>(40134);
        meter.EnergyActiveProducedL3 = Read<float>(40136);
        meter.EnergyActiveConsumed = Read<float>(40138);
        meter.EnergyActiveConsumedL1 = Read<float>(40140);
        meter.EnergyActiveConsumedL2 = Read<float>(40142);
        meter.EnergyActiveConsumedL3 = Read<float>(40144);

        meter.EnergyApparentProduced = Read<float>(40146);
        meter.EnergyApparentProducedL1 = Read<float>(40148);
        meter.EnergyApparentProducedL2 = Read<float>(40150);
        meter.EnergyApparentProducedL3 = Read<float>(40152);
        meter.EnergyApparentConsumed = Read<float>(40154);
        meter.EnergyApparentConsumedL1 = Read<float>(40156);
        meter.EnergyApparentConsumedL2 = Read<float>(40158);
        meter.EnergyApparentConsumedL3 = Read<float>(40160);

        meter.EnergyReactiveConsumedQ1 = Read<float>(40162);
        meter.EnergyReactiveConsumedQ1L1 = Read<float>(40164);
        meter.EnergyReactiveConsumedQ1L2 = Read<float>(40166);
        meter.EnergyReactiveConsumedQ1L3 = Read<float>(40168);
        meter.EnergyReactiveConsumedQ2 = Read<float>(40170);
        meter.EnergyReactiveConsumedQ2L1 = Read<float>(40172);
        meter.EnergyReactiveConsumedQ2L2 = Read<float>(40174);
        meter.EnergyReactiveConsumedQ2L3 = Read<float>(40176);

        meter.EnergyReactiveProducedQ3 = Read<float>(40178);
        meter.EnergyReactiveProducedQ3L1 = Read<float>(40180);
        meter.EnergyReactiveProducedQ3L2 = Read<float>(40182);
        meter.EnergyReactiveProducedQ3L3 = Read<float>(40184);
        meter.EnergyReactiveProducedQ4 = Read<float>(40186);
        meter.EnergyReactiveProducedQ4L1 = Read<float>(40188);
        meter.EnergyReactiveProducedQ4L2 = Read<float>(40190);
        meter.EnergyReactiveProducedQ4L3 = Read<float>(40192);

        meter.EventBitField = Read<uint>(40194);
        
        return;

        [Pure]
        T Read<T>(ushort registerNumber) where T : unmanaged => ReadAbsolute<T>(data, registerNumber);
    }

    private static void ReadIntSfData(SunSpecMeter meter, ReadOnlyMemory<byte> data)
    {
        var currentScaleFactor = Math.Pow(10, Read<short>(40076));//-4
        meter.TotalCurrent = Read<short>(40072) * currentScaleFactor;
        meter.CurrentL1 = Read<short>(40073) * currentScaleFactor;
        meter.CurrentL2 = Read<short>(40074) * currentScaleFactor;
        meter.CurrentL3 = Read<short>(40075) * currentScaleFactor;

        var voltageScaleFactor = Math.Pow(10, Read<short>(40085));//-1
        meter.PhaseVoltageAverage = Read<short>(40077) * voltageScaleFactor;
        meter.PhaseVoltageL1 = Read<short>(40078) * voltageScaleFactor;
        meter.PhaseVoltageL2 = Read<short>(40079) * voltageScaleFactor;
        meter.PhaseVoltageL3 = Read<short>(40080) * voltageScaleFactor;
        meter.LineVoltageAverage = Read<short>(40081) * voltageScaleFactor;
        meter.LineVoltageL12 = Read<short>(40082) * voltageScaleFactor;
        meter.LineVoltageL23 = Read<short>(40083) * voltageScaleFactor;
        meter.LineVoltageL31 = Read<short>(40084) * voltageScaleFactor;

        var frequencyScaleFactor = Math.Pow(10, Read<short>(40087));//-2
        meter.Frequency = Read<short>(40086) * frequencyScaleFactor;

        var activePowerScaleFactor = Math.Pow(10, Read<short>(40092));//-2
        meter.ActivePowerSum = Read<short>(40088) * activePowerScaleFactor;
        meter.ActivePowerL1 = Read<short>(40089) * activePowerScaleFactor;
        meter.ActivePowerL2 = Read<short>(40090) * activePowerScaleFactor;
        meter.ActivePowerL3 = Read<short>(40091) * activePowerScaleFactor;

        var apparentPowerScaleFactor = Math.Pow(10, Read<short>(40097));//-2
        meter.ApparentPowerSum = Read<short>(40093) * apparentPowerScaleFactor;
        meter.ApparentPowerL1 = Read<short>(40094) * apparentPowerScaleFactor;
        meter.ApparentPowerL2 = Read<short>(40095) * apparentPowerScaleFactor;
        meter.ApparentPowerL3 = Read<short>(40096) * apparentPowerScaleFactor;

        var reactivePowerScaleFactor = Math.Pow(10, Read<short>(40102));//-2
        meter.ReactivePowerSum = Read<short>(40098) * reactivePowerScaleFactor;
        meter.ReactivePowerL1 = Read<short>(40099) * reactivePowerScaleFactor;
        meter.ReactivePowerL2 = Read<short>(40100) * reactivePowerScaleFactor;
        meter.ReactivePowerL3 = Read<short>(40101) * reactivePowerScaleFactor;

        var powerFactorScaleFactor = Math.Pow(10, Read<short>(40107)) / 100d;//-1
        meter.PowerFactorTotal = Read<short>(40103) * powerFactorScaleFactor;
        meter.PowerFactorL1 = Read<short>(40104) * powerFactorScaleFactor;
        meter.PowerFactorL2 = Read<short>(40105) * powerFactorScaleFactor;
        meter.PowerFactorL3 = Read<short>(40106) * powerFactorScaleFactor;

        var energyActiveScaleFactor = Math.Pow(10, Read<short>(40124)); //-3
        meter.EnergyActiveProduced = Read<uint>(40108) * energyActiveScaleFactor;
        meter.EnergyActiveProducedL1 = Read<uint>(40110) * energyActiveScaleFactor;
        meter.EnergyActiveProducedL2 = Read<uint>(40112) * energyActiveScaleFactor;
        meter.EnergyActiveProducedL3 = Read<uint>(40114) * energyActiveScaleFactor;
        meter.EnergyActiveConsumed = Read<uint>(40116) * energyActiveScaleFactor;
        meter.EnergyActiveConsumedL1 = Read<uint>(40118) * energyActiveScaleFactor;
        meter.EnergyActiveConsumedL2 = Read<uint>(40120) * energyActiveScaleFactor;
        meter.EnergyActiveConsumedL3 = Read<uint>(40122) * energyActiveScaleFactor;
        
        var energyApparentScaleFactor = Math.Pow(10, Read<short>(40141)); //-32768
        meter.EnergyApparentProduced = Read<uint>(40125) * energyApparentScaleFactor;
        meter.EnergyApparentProducedL1 = Read<uint>(40127) * energyApparentScaleFactor;
        meter.EnergyApparentProducedL2 = Read<uint>(40129) * energyApparentScaleFactor;
        meter.EnergyApparentProducedL3 = Read<uint>(40131) * energyApparentScaleFactor;
        meter.EnergyApparentConsumed = Read<uint>(40133) * energyApparentScaleFactor;
        meter.EnergyApparentConsumedL1 = Read<uint>(40135) * energyApparentScaleFactor;
        meter.EnergyApparentConsumedL2 = Read<uint>(40137) * energyApparentScaleFactor;
        meter.EnergyApparentConsumedL3 = Read<uint>(40139) * energyApparentScaleFactor;

        var energyReactiveScaleFactor = Math.Pow(10, Read<short>(40174)); //-32768
        meter.EnergyReactiveConsumedQ1 = Read<uint>(40142) * energyReactiveScaleFactor;
        meter.EnergyReactiveConsumedQ1L1 = Read<uint>(40144) * energyReactiveScaleFactor;
        meter.EnergyReactiveConsumedQ1L2 = Read<uint>(40146) * energyReactiveScaleFactor;
        meter.EnergyReactiveConsumedQ1L3 = Read<uint>(40148) * energyReactiveScaleFactor;
        meter.EnergyReactiveConsumedQ2 = Read<uint>(40150) * energyReactiveScaleFactor;
        meter.EnergyReactiveConsumedQ2L1 = Read<uint>(40152) * energyReactiveScaleFactor;
        meter.EnergyReactiveConsumedQ2L2 = Read<uint>(40154) * energyReactiveScaleFactor;
        meter.EnergyReactiveConsumedQ2L3 = Read<uint>(40156) * energyReactiveScaleFactor;

        meter.EnergyReactiveProducedQ3 = Read<uint>(40158) * energyReactiveScaleFactor;
        meter.EnergyReactiveProducedQ3L1 = Read<uint>(40160) * energyReactiveScaleFactor;
        meter.EnergyReactiveProducedQ3L2 = Read<uint>(40162) * energyReactiveScaleFactor;
        meter.EnergyReactiveProducedQ3L3 = Read<uint>(40164) * energyReactiveScaleFactor;
        meter.EnergyReactiveProducedQ4 = Read<uint>(40166) * energyReactiveScaleFactor;
        meter.EnergyReactiveProducedQ4L1 = Read<uint>(40168) * energyReactiveScaleFactor;
        meter.EnergyReactiveProducedQ4L2 = Read<uint>(40170) * energyReactiveScaleFactor;
        meter.EnergyReactiveProducedQ4L3 = Read<uint>(40172) * energyReactiveScaleFactor;

        meter.EventBitField = Read<uint>(40175);
        
        return;
        
        [Pure]
        T Read<T>(ushort registerNumber) where T : unmanaged => ReadAbsolute<T>(data, registerNumber);
    }
    
    [Pure]
    private static T ReadAbsolute<T>(ReadOnlyMemory<byte> data, ushort registerNumber) where T : unmanaged => ReadRelative<T>(data, (ushort)((registerNumber - 40072) << 1));
}