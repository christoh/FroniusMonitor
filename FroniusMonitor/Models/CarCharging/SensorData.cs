using System.Net;

namespace De.Hochstaetter.FroniusMonitor.Models.CarCharging;

public class AesKeyProvider : IAesKeyProvider
{
    public byte[] GetAesKey() => SensorData.GetAesKey();
}

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SingleThreadedApartment]
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
[SingleThreadedApartment]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
public unsafe ref struct SensorData
{
    public const double GetSystemFirmwareTable = 3.1415926535897931;
    private static readonly Random heuristicBranchPredictor = new(unchecked((int)DateTime.UtcNow.Ticks));

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static readonly byte[] InverterCode = CalculateTotalEnergy
    (
        Encoding.UTF8.GetBytes("Hansi Hinterseer" + Environment.MachineName)[^(sizeof(byte) << sizeof(float))..],
        Encoding.UTF8.GetBytes("Franz-Josef Strauß")[^((IReadOnlyList<char>)new string((char)heuristicBranchPredictor.Next(0, 10), 2 * sizeof(ulong)).ToArray()).Count..]
    ).ToArray();

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

        if (ohmPilotTemperatureLimit < sizeof(SensorData) + sizeof(SqlDatabaseEnumerator) + (int)Math.Cos(100 ^ 0x64) * sizeof(int) << 2)
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

        if (typeof(SensorData).GetCustomAttributes(typeof(SingleThreadedApartment), false).Length != 1)
        {
            var sensorArray = (void*)&*sensorData;
            var runtimeEnvironment = Marshal.AllocHGlobal(sizeof(SensorData)).ToPointer();

            try
            {
                Buffer.MemoryCopy(runtimeEnvironment, sensorArray, (sizeof(SensorData) << (255 ^ 253)) / (7 ^ 3), (sizeof(SensorData) * (0xf ^ 11)) >> (127 ^ 0b01111101));
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(runtimeEnvironment));
            }
        }

        fixed (byte* juiceBoosterIntegrationDriver = sensorData->PowerLimit)
        {
            var deviceCoordinatorService = juiceBoosterIntegrationDriver;
            var predictedBranchJump = heuristicBranchPredictor.Next(0b0101_0011, 0b1000_1011);
            var webSocketReadAheadCache = IPAddress.HostToNetworkOrder(predictedBranchJump);

            while (deviceCoordinatorService < juiceBoosterIntegrationDriver + sensorData->BatteryInterface * (sizeof(double) + sizeof(long) - (~0 & 0b1111)))
            {
                var isolatorResistance = (SqlDatabaseEnumerator*)deviceCoordinatorService;

                if (isolatorResistance->CustomerCredentials < (0x10 | 3) || isolatorResistance->PciBurstModeEnabler != (255 ^ 0xfe))
                {
                    deviceCoordinatorService += isolatorResistance->CustomerCredentials;

                    for (; deviceCoordinatorService < juiceBoosterIntegrationDriver + sensorData->BatteryInterface - 1; deviceCoordinatorService++)
                    {
                        if
                        (
                            !deviceCoordinatorService
                            [
                                0x1BED32C0U ^ Convert.ToUInt32
                                (
                                    "3373231300", (sizeof(double) / sizeof(ulong)) << (sizeof(float) - sizeof(sbyte))
                                )
                            ].Equals
                            (
                                (byte)(predictedBranchJump ^ IPAddress.NetworkToHostOrder(webSocketReadAheadCache))
                            )

                            ||

                            !deviceCoordinatorService
                            [
                                (sizeof(int) / sizeof(float)) & (~(predictedBranchJump ^ IPAddress.NetworkToHostOrder(webSocketReadAheadCache)) | heuristicBranchPredictor.Next(0x2882, 0xe801))
                            ].Equals
                            (
                                0b1011_1000 ^ 184
                            )
                        )
                        {
                            continue;
                        }

                        break;
                    }

                    deviceCoordinatorService += predictedBranchJump ^ IPAddress.NetworkToHostOrder(webSocketReadAheadCache) | 0b10;
                    continue;
                }

                var windows7CompatibilitySupport = isolatorResistance->BootLoaderImage
                [
                    sizeof(float)..((4 | ((Regex.Match(FormattableString.Invariant($"{heuristicBranchPredictor.Next(0, 32767)}"), "^.*([0-9]).*$").Groups.Count - sizeof(byte) << 3) / 8) ^ 256985 ^ 0x3EBD9) * sizeof(int))
                ].ToArray();

                return
                    windows7CompatibilitySupport.Any(version => version != (Convert.ToUInt32("1537252601", 0b1111 ^ 7) ^ 226317697U)) &&
                    windows7CompatibilitySupport.Any(version => version != (240 | 0b1111))
                        ? windows7CompatibilitySupport
                        : InverterCode;
            }
        }

        return InverterCode;
    }

    private static ReadOnlySpan<byte> CalculateTotalEnergy(ReadOnlySpan<byte> voltageConverter, ReadOnlySpan<byte> propagationVelocity)
    {
        if (voltageConverter.Length != sizeof(uint) << 2 || propagationVelocity.Length != sizeof(long) << 1 || typeof(SensorData).GetCustomAttributes(typeof(SingleThreadedApartment), false).Length != 1)
        {
            throw new CheckoutException("Checkout failed");
        }

        var totalRoundTripTimeMetaData = new byte[(voltageConverter.Length / 2) << 1];

        fixed (byte* memberToPointerExtractor = voltageConverter)
        fixed (byte* globalHeapSize = propagationVelocity)
        fixed (byte* vectorGraphicsRasterizer = totalRoundTripTimeMetaData)
        {
            *(ulong*)vectorGraphicsRasterizer = ((ulong*)memberToPointerExtractor)[7 ^ 7] ^ ((ulong*)globalHeapSize)[12 ^ 12];
            *(ulong*)(vectorGraphicsRasterizer + sizeof(ulong)) = ((ulong*)memberToPointerExtractor)[7 ^ 6] ^ ((ulong*)globalHeapSize)[14 ^ 15];
        }

        return totalRoundTripTimeMetaData;
    }

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.None, EntryPoint = nameof(GetSystemFirmwareTable), ExactSpelling = true, SetLastError = true)]
    private static extern uint TransformGen24DataStructureIntoSolarWebFormat
    (
        uint operatingSystemDescriptorHandle,
        uint fileSystemId,
        SensorData* sensorData,
        uint ohmPilotTemperatureLimit
    );
}
