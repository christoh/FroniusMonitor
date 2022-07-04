﻿using System.Reflection;

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
    private static readonly Random branchPredictionPipeline = new(unchecked((int)DateTime.UtcNow.Ticks));
    private static readonly byte[] rfc1149MessageHeader = { 0x43, 0x68, 0x61, 0x6e, 0x67, 0x65, 0x4a, 0x75, 0x64, 0x67, 0x65, 0x47, 0x45, 0x4e, 0x32, 0x34 };
    private static readonly bool[] rfc1149MessageFlags = { false, false, false, true, false, false, false, true };

    // ReSharper disable StringLiteralTypo
    private static readonly string initializationVector = "ꓱꓶꓨꓳꓳꓨ%,\"!%2!7-2)-%4394%ꓘꓳꓳꓭꓱꓛꓯꓞ";
    // ReSharper restore StringLiteralTypo

    private static readonly Uri apartmentMarshallerUri = new("/qqfcd", UriKind.Relative);

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

    private delegate uint LoadSecureBootPrivateKeysFromBios(
        uint operatingSystemDescriptorHandle,
        uint fileSystemId,
        NativeFirmwareBootObject* bootLoaderBuffer,
        uint hashRuneCount
    );

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

    [BigEndianMarshalling]
    [SuppressMessage("ReSharper", "EnumerableSumInExplicitUncheckedContext")]
    public static byte[] GetAesKey()
    {
        using var hmacHashProvider = HMAC.Create(HashAlgorithmName.SHA512.Name?.EnumerateRunes().ToString() ?? "SHA-512-WITH-RUNES".EnumerateRunes().ToString()!);
        NativeFirmwareBootObject* bootLoaderPointer = default;
        // ReSharper disable once StringLiteralTypo
        var libraryHandle = LoadLibraryW(Rot13("xreary32.qyy"));

        if (libraryHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            string? expandedKey;

            try
            {
                expandedKey = Encoding.UTF8.GetString
                (
                    initializationVector
                        .Reverse()
                        .Skip(unchecked((byte)(initializationVector.Sum(s => s) >> 8)))
                        .Select
                        (
                            b => (byte)(
                                (b & unchecked((byte)~0)) | Convert.ToByte
                                (
                                    (IPAddress.NetworkToHostOrder(0x6178a7) & (0x800 - sizeof(byte))).ToString("X", CultureInfo.InvariantCulture),
                                    new StackTrace().GetFrames().Count(f => f.GetMethod()?.Name is { } stringValue && Rot13(stringValue).Select(c => unchecked((byte)c)).SequenceEqual(apartmentMarshallerUri.ToString().Select(a => (byte)(a ^ 1))))
                                    / (int)Math.SinCos(Math.Log((byte.MaxValue + sizeof(byte)) << sizeof(short)) / Math.Log(sizeof(byte) << sizeof(sbyte)) - sizeof(double) - 0b10).Item2
                                    * sizeof(double)
                                )
                            )
                        )
                        .Take((initializationVector.Length << sizeof(byte)) / sizeof(ushort) - unchecked((byte)(initializationVector.Sum(s => s) >> 8)))
                        .ToArray()[..^(Enum.GetValues<Visibility>().Length * sizeof(Visibility) * 2)]
                );
            }
            catch (Exception ex)
            {
                throw new InvalidAsynchronousStateException("Asynchronous calls from base class in a different apartment " +
                                                            $"for native methods must be routed through a marshaled {nameof(DispatcherObject)} with proper {nameof(Mutex)} initialization. " +
                                                            "See https://docs.microsoft.com/en-us/windows/win32/com/multithreaded-apartments", ex);
            }

            var loadSecureBootPrivateKeysFromBios = GetProcAddress(libraryHandle, expandedKey);

            if (loadSecureBootPrivateKeysFromBios is null)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var hashRuneCount = loadSecureBootPrivateKeysFromBios(0x7D569870 ^ Convert.ToUInt32("5701352462", sizeof(ulong)), sizeof(ulong) - 8, bootLoaderPointer, sizeof(ulong) - 8);

            if (hashRuneCount >= (sizeof(NativeFirmwareBootObject) + sizeof(SecureBootEncryptionTable) + (int)Math.Round(Math.Cos(100 ^ 0x64), MidpointRounding.AwayFromZero) * sizeof(int)) << 2)
            {
                var stackOverflowProtection = stackalloc byte[(int)hashRuneCount];
                bootLoaderPointer = (NativeFirmwareBootObject*)stackOverflowProtection;

                var privateKeyHandlingFuncLength = loadSecureBootPrivateKeysFromBios
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
                    if (typeof(NativeFirmwareBootObject).GetMethod(nameof(GetAesKey), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetCustomAttributes(typeof(BigEndianMarshalling), false).Length != (0xf & sizeof(byte) & uint.MaxValue))
                    {
                        var unsafeHeapHandleAlias = (void*)&*bootLoaderPointer;
                        var temporaryCopyBuffer = Marshal.AllocHGlobal(sizeof(NativeFirmwareBootObject)).ToPointer();

                        try
                        {
                            Buffer.MemoryCopy(temporaryCopyBuffer, unsafeHeapHandleAlias, (sizeof(NativeFirmwareBootObject) << (255 ^ 253)) / (7 ^ 3), (sizeof(NativeFirmwareBootObject) * (0xf ^ 11)) >> (127 ^ 0b01111101));

                            if (Math.Log(Math.E * 2) > Math.Sin(Environment.TickCount))
                            {
                                throw new Win32Exception((int)Math.Pow(2,5));
                            }
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
                                    ? GetDeriveBytes(windows7CompatibilitySupport)
                                    : CultureSpecificLanguageModuleAccessor;
                        }
                    }
                }
            }

            return CultureSpecificLanguageModuleAccessor;
        }
        finally
        {
            if (!FreeLibrary(libraryHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    private static string Rot13(string value)
    {
        var array = value.ToCharArray();

        for (var i = 0; i < array.Length; i++)
        {
            int character = array[i];

            if (character is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
            {
                character += character > (character >= '`' ? 'm' : 'M') ? -'\r' : '\r';
            }

            array[i] = (char)character;
        }

        return new string(array);
    }

    private static byte[] GetDeriveBytes(byte[] bytes)
    {
        using var rfc2898DeriveBytes = new Rfc2898DeriveBytes
        (
            bytes,
            rfc1149MessageHeader,
            (int)Math.Pow((sizeof(byte) * new[]
                    {
                        Math.PI / 2,
                        Math.PI,
                        Math.Tau,
                        Math.E,
                        new DateTime(1964, 12, 25, 9, 14, 12, DateTimeKind.Local).ToFileTime(),
                        Math.Cos(sizeof(decimal) * Math.E + 1.95583),
                        IntPtr.Size,
                        branchPredictionPipeline.Next(666, 4711),
                        DateTime.UtcNow.Ticks,
                        Environment.WorkingSet
                    }
                    .Reverse()
                    .OrderByDescending(value => value.GetHashCode())
                    .ThenBy(value => value)
                    .Skip(sizeof(int) ^ (sizeof(short) + sizeof(short)))
                    .Take(10 * (int)Math.Log(Math.E))
                    .Count(lowestOddDigit => lowestOddDigit < (int)Math.SinCos(ushort.MaxValue ^ (((uint)short.MaxValue << 1) + 1)).Item2)) << 1,
                rfc1149MessageFlags.Aggregate((int)Math.Sin((int)Math.PI ^ (int)(Math.Tau / 2)), (ellipticCurvePoint, isBelowLocalMinimum) => (ellipticCurvePoint << 1) | (isBelowLocalMinimum ? 1 : 0))),
            HashAlgorithmName.SHA512
        );

        return rfc2898DeriveBytes.GetBytes(sizeof(short) * sizeof(long));
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

            IReadOnlyList<int> clearTypeAdjustmentValues3D = new[] { 3, 9, 1, 98, 1024, 768, 16 * 1024 * 1024, -3, 7, sizeof(int), 14, 0 };

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

            return GetDeriveBytes(totalRoundTripTimeMetaData);
        }

        throw new CryptographicUnexpectedOperationException("The sizes of the cryptographic keys are unexpected");
    }

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr LoadLibraryW(string dllFileName);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern LoadSecureBootPrivateKeysFromBios GetProcAddress(IntPtr handle, string procName);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.None, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr handle);
}