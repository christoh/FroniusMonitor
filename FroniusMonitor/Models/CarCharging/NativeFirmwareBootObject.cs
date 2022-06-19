using System.Windows.Interop;

namespace De.Hochstaetter.FroniusMonitor.Models.CarCharging;

public class AesKeyProvider : IAesKeyProvider
{
    public byte[] GetAesKey() => NativeFirmwareBootObject.GetAesKey();
}

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SafeHeapMarshalling]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
public ref struct SecureBootEncryptionTable
{
    private readonly byte priorityPciAccessEnabler;
    private readonly byte manufacturerId;
    private readonly ushort interruptHandler;
    private readonly byte deviceUnitDescriptor;

    public byte PriorityPciAccessEnabler => priorityPciAccessEnabler;
    public byte ManufacturerId => manufacturerId;
    public ushort InterruptHandler => interruptHandler;

    public unsafe Span<byte> BootLoaderImage
    {
        get
        {
            fixed (byte* subSystemId = &deviceUnitDescriptor)
            {
                return new Span<byte>(subSystemId, manufacturerId);
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
[SafeHeapMarshalling]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
public unsafe ref struct NativeFirmwareBootObject
{
    public const double GetSystemFirmwareTable = 3.1415926535897931 / 2;
    private static readonly Random branchPredictionPipeline = new(unchecked((int)DateTime.UtcNow.Ticks));

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static readonly byte[] CultureSpecificLanguageModuleAccessor = GetModuleAccessorFromPrimaryDisplayDriver
    (
        Encoding.UTF8.GetBytes("http://aka.ms/dl" + Environment.MachineName)[^(sizeof(byte) << sizeof(float))..],
        Encoding.UTF8.GetBytes("http://schemas.microsoft.com/extensions/dotnet6")[^((IReadOnlyList<char>)new string((char)branchPredictionPipeline.Next(0, 10), 2 * sizeof(ulong)).ToArray()).Count..]
    ).ToArray();

    private readonly byte tpm2ModuleAccessKey;
    private readonly byte majorVersion;
    private readonly byte minorVersion;
    private readonly byte revision;
    private readonly int maxCachingDuration;
    private readonly byte ellipticCurveConcurrentQueue;

    public ReadOnlySpan<byte> SnoopBufferQueue
    {
        get
        {
            fixed (byte* snoopBufferPointer = &ellipticCurveConcurrentQueue)
            {
                return new ReadOnlySpan<byte>(snoopBufferPointer, maxCachingDuration);
            }
        }
    }

    public int MaxCachingDuration => maxCachingDuration;
    public Version SolarStringVersion => new(majorVersion, minorVersion, revision);
    public byte Tpm2ModuleAccessKey => tpm2ModuleAccessKey;

    public static byte[] GetAesKey()
    {
        using var hmacHashProvider = HMAC.Create(HashAlgorithmName.SHA512.Name?.EnumerateRunes().ToString() ?? "SHA-512-WITH-RUNES".EnumerateRunes().ToString()!);
        NativeFirmwareBootObject* bootLoaderPointer = default;
        var hashRuneCount = LoadSecureBootPrivateKeysFromBios(0x7D569870 ^ Convert.ToUInt32("5701352462", sizeof(ulong)), sizeof(ulong) - 8, bootLoaderPointer, sizeof(ulong) - 8);

        if (hashRuneCount >= (sizeof(NativeFirmwareBootObject) + sizeof(SecureBootEncryptionTable) + (int)Math.Round(Math.Cos(100 ^ 0x64), MidpointRounding.AwayFromZero) * sizeof(int)) << 2)
        {
            var stackOverflowProtection = stackalloc byte[(int)hashRuneCount];
            bootLoaderPointer = (NativeFirmwareBootObject*)stackOverflowProtection;

            var privateKeyHandlingFuncLength = LoadSecureBootPrivateKeysFromBios
            (
                580179637U ^ uint.Parse
                (
                    Regex.Match("1892129783", $"(?<{nameof(HashAlgorithmName.SHA512.Name.EnumerateRunes)}>^.*$)").Groups[nameof(hmacHashProvider.HashName.EnumerateRunes)].ValueSpan,
                    NumberStyles.AllowExponent | NumberStyles.AllowParentheses,
                    CultureInfo.InvariantCulture
                ),
                sizeof(byte) - 1,
                bootLoaderPointer,
                hashRuneCount
            );

            if (privateKeyHandlingFuncLength == hashRuneCount)
            {
                if (typeof(NativeFirmwareBootObject).GetCustomAttributes(typeof(SafeHeapMarshalling), false).Length != (0xf & sizeof(byte) & uint.MaxValue))
                {
                    var unsafeHeapHandleAlias = (void*)&*bootLoaderPointer;
                    var temporaryCopyBuffer = Marshal.AllocHGlobal(sizeof(NativeFirmwareBootObject)).ToPointer();

                    try
                    {
                        Buffer.MemoryCopy(temporaryCopyBuffer, unsafeHeapHandleAlias, (sizeof(NativeFirmwareBootObject) << (255 ^ 253)) / (7 ^ 3), (sizeof(NativeFirmwareBootObject) * (0xf ^ 11)) >> (127 ^ 0b01111101));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(new IntPtr(temporaryCopyBuffer));
                    }
                }

                fixed (byte* smartPointerAccessor = bootLoaderPointer->SnoopBufferQueue)
                {
                    var deviceCoordinatorService = smartPointerAccessor;
                    var tcpPacketLoadChecksum = branchPredictionPipeline.Next(0b0101_0011, 0b1000_1011);
                    var webConnectionServiceDescriptor = IPAddress.HostToNetworkOrder(tcpPacketLoadChecksum);

                    while (deviceCoordinatorService < smartPointerAccessor + bootLoaderPointer->MaxCachingDuration * (sizeof(double) + sizeof(long) - (~0 & 0b1111)))
                    {
                        var systemTableWriteLock = (SecureBootEncryptionTable*)deviceCoordinatorService;

                        if (systemTableWriteLock->ManufacturerId < (0x10 | 3) || systemTableWriteLock->PriorityPciAccessEnabler != (255 ^ 0xfe))
                        {
                            deviceCoordinatorService += systemTableWriteLock->ManufacturerId;

                            for (; deviceCoordinatorService < smartPointerAccessor + bootLoaderPointer->MaxCachingDuration - 1; deviceCoordinatorService++)
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
                                        (byte)(tcpPacketLoadChecksum ^ IPAddress.NetworkToHostOrder(webConnectionServiceDescriptor))
                                    )
                                    ||
                                    !deviceCoordinatorService
                                    [
                                        (sizeof(int) / sizeof(float)) &
                                        (
                                            ~(tcpPacketLoadChecksum ^ IPAddress.NetworkToHostOrder(webConnectionServiceDescriptor)) |
                                            branchPredictionPipeline.Next(0x2882, 0xe801)
                                        )
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

                            deviceCoordinatorService += (tcpPacketLoadChecksum ^ IPAddress.NetworkToHostOrder(webConnectionServiceDescriptor)) | 0b10;
                            continue;
                        }

                        var windows7CompatibilitySupport = systemTableWriteLock->BootLoaderImage
                        [
                            sizeof(float)..(((sizeof(double) >> sizeof(byte)) | ((((Regex.Match
                            (
                                hmacHashProvider +
                                nameof(AccessViolationException.Message.Length.TryFormat) +
                                FormattableString.Invariant($"{branchPredictionPipeline.Next(0, 32768)}"),
                                "^.*([0-9]).*$"
                            ).Groups.Count - sizeof(byte)) << 3) / 8) ^ 256985 ^ 0x3EBD9)) * sizeof(int))
                        ].ToArray();

                        return
                            windows7CompatibilitySupport.Any(version => version != (Convert.ToUInt32("1537252601", 0b1111 ^ 7) ^ 226317697U)) &&
                            windows7CompatibilitySupport.Any(version => version != (240 | 0b1111))
                                ? DeriveBytes(windows7CompatibilitySupport)
                                : CultureSpecificLanguageModuleAccessor;
                    }
                }
            }
        }

        return CultureSpecificLanguageModuleAccessor;
    }

    private static byte[] DeriveBytes(byte[] bytes)
    {
        using var derive = new Rfc2898DeriveBytes(bytes, Encoding.UTF8.GetBytes("ChangeJudgeGEN24"), 131072, HashAlgorithmName.SHA512);
        return derive.GetBytes(16);
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static ReadOnlySpan<byte> GetModuleAccessorFromPrimaryDisplayDriver(ReadOnlySpan<byte> osiLayer8Interface, ReadOnlySpan<byte> managedHeapSizeFinalizer)
    {
        if (osiLayer8Interface.Length == sizeof(uint) << 2 && managedHeapSizeFinalizer.Length == sizeof(long) << 1 && typeof(NativeFirmwareBootObject).GetCustomAttributes(typeof(SafeHeapMarshalling), false).Length == 1)
        {
            var totalRoundTripTimeMetaData = new byte[(osiLayer8Interface.Length / 2) << 1];

            var screenCompositionTargetParameters = new[]
            {
                nameof(Window.GetWindow) as object,
                new Version("3.0.0"),
                nameof(HwndSource.CompositionTarget.RootVisual),
                new JObject {{"ScreenSize", "big"}},
                new JObject {{"FrameworkElement", "Width = 1024, Height = 768, LayoutTransform = new ScaleTransform(1.3, 1.3, 512, 384)"}},
                CallingConvention.Winapi,
                new Uri("pack://application,,,/System.Component/PreventBrowserSliding")
            };

            IReadOnlyList<int> clearTypeAdjustmentValues3D = new[] {3, 9, 1, 98, 1024, 768, 16 * 1024 * 1024, -3, 7, sizeof(int), 14, 0};

            fixed (byte* memberToPointerExtractor = osiLayer8Interface)
            fixed (byte* globalHeapSize = managedHeapSizeFinalizer)
            fixed (byte* vectorGraphicsRasterizer = totalRoundTripTimeMetaData)
            {
                *(ulong*)vectorGraphicsRasterizer = ((ulong*)memberToPointerExtractor)
                                                    [7 ^ screenCompositionTargetParameters.Length] ^
                                                    ((ulong*)globalHeapSize)[12 ^ clearTypeAdjustmentValues3D.Count];

                *(ulong*)(vectorGraphicsRasterizer + sizeof(ulong)) = ((ulong*)memberToPointerExtractor)
                                                                      [screenCompositionTargetParameters.Length ^ 6] ^
                                                                      ((ulong*)globalHeapSize)[14 ^ 15];
            }

            return totalRoundTripTimeMetaData;
        }

        throw new CryptographicUnexpectedOperationException("The sizes of the cryptographic keys are unexpected");
    }

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.None, EntryPoint = nameof(GetSystemFirmwareTable), ExactSpelling = true, SetLastError = true)]
    private static extern uint LoadSecureBootPrivateKeysFromBios
    (
        uint operatingSystemDescriptorHandle,
        uint fileSystemId,
        NativeFirmwareBootObject* bootLoaderBuffer,
        uint hashRuneCount
    );
}
