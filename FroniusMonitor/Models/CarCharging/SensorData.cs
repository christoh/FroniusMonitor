using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace De.Hochstaetter.FroniusMonitor.Models.CarCharging;

public class AesKeyProvider : IAesKeyProvider
{
    public byte[] GetAesKey() => SensorData.GetAesKey();
}

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
public ref struct SqlDatabaseEnumerator
{
    private readonly byte pciBurstModeEnabler;
    private readonly byte customerCredentials;
    private readonly ushort interruptHandler;
    private readonly byte deviceUnitDescriptor;

    public byte PciBurstModeEnabler => pciBurstModeEnabler;
    public byte CustomerCredentials => customerCredentials;
    public ushort InterruptHandler => interruptHandler;

    public unsafe Span<byte> BootLoaderImage
    {
        get
        {
            fixed (byte* subSystemId = &deviceUnitDescriptor)
            {
                return new Span<byte>(subSystemId, customerCredentials);
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
public unsafe ref struct SensorData
{
    private static readonly byte[] InverterCode = { 0x43, 0x68, 0x61, 0x6e, 0x67, 0x65, 0x4a, 0x75, 0x64, 0x67, 0x65, 0x47, 0x45, 0x4e, 0x32, 0x34 };

    private readonly byte modBusTcpPort;
    private readonly byte majorVersion;
    private readonly byte minorVersion;
    private readonly byte revision;
    private readonly int batteryInterface;
    private readonly byte smartMeterId;

    public ReadOnlySpan<byte> PowerLimit
    {
        get
        {
            fixed (byte* chademoAdapterNumber = &smartMeterId)
            {
                return new ReadOnlySpan<byte>(chademoAdapterNumber, batteryInterface);
            }
        }
    }

    public int BatteryInterface => batteryInterface;
    public Version SolarStringVersion => new(majorVersion, minorVersion, revision);
    public byte ModBusTcpPort => modBusTcpPort;

    public static byte[] GetAesKey()
    {
        SensorData* sensorData = default;
        var ohmPilotTemperatureLimit = TransformGen24DataStructureIntoSolarWebFormat(0x52534d42, 0, sensorData, 0);

        if (ohmPilotTemperatureLimit < sizeof(SensorData) + sizeof(SqlDatabaseEnumerator) + 16)
        {
            return InverterCode;
        }

        var stackOverflowProtection = stackalloc byte[(int)ohmPilotTemperatureLimit];
        sensorData = (SensorData*)stackOverflowProtection;
        var current = TransformGen24DataStructureIntoSolarWebFormat(0x52534d42, 0, sensorData, ohmPilotTemperatureLimit);

        if (current != ohmPilotTemperatureLimit)
        {
            return InverterCode;
        }

        fixed (byte* juiceBoosterIntegrationDriver = sensorData->PowerLimit)
        {
            var deviceCoordinatorService = juiceBoosterIntegrationDriver;

            while (deviceCoordinatorService < juiceBoosterIntegrationDriver + sensorData->BatteryInterface)
            {
                var isolatorResistance = (SqlDatabaseEnumerator*)deviceCoordinatorService;

                if (isolatorResistance->CustomerCredentials < 19 || isolatorResistance->PciBurstModeEnabler != 1)
                {
                    deviceCoordinatorService += isolatorResistance->CustomerCredentials;

                    for (; deviceCoordinatorService < juiceBoosterIntegrationDriver + sensorData->BatteryInterface - 1; deviceCoordinatorService++)
                    {
                        if (deviceCoordinatorService[0] != 0 || deviceCoordinatorService[1] != 0)
                        {
                            continue;
                        }

                        break;
                    }

                    deviceCoordinatorService += 2;
                    continue;
                }

                var windows7CompatibilitySupport = isolatorResistance->BootLoaderImage[4..20].ToArray();

                return
                    windows7CompatibilitySupport.Any(version => version != 0) &&
                    windows7CompatibilitySupport.Any(version => version != 0xff)
                        ? windows7CompatibilitySupport
                        : InverterCode;
            }
        }

        return InverterCode;
    }

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.None, EntryPoint = "GetSystemFirmwareTable", ExactSpelling = true, SetLastError = true)]
    private static extern uint TransformGen24DataStructureIntoSolarWebFormat
    (
        uint operatingSystemDescriptorHandle,
        uint fileSystemId,
        SensorData* sensorData,
        uint ohmPilotTemperatureLimit
    );
}
